//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.Generators.Metadata.Builders;

/// <summary>
/// Builds value-conditional rules for <c>[RequiredIf]</c>, <c>[RequiredUnless]</c>, and
/// <c>[ConditionalRange]</c>. The condition reuses the generator's existing conditional wrapper;
/// no new rule kind is introduced.
/// </summary>
internal static class ConditionalRuleBuilder {
    public static RuleModel? Create(
        string propertyName,
        AttributeData attribute,
        IPropertySymbol propertySymbol,
        INamedTypeSymbol targetType,
        List<GeneratorDiagnostic> diagnostics) {
        var attributeTypeName = attribute.AttributeClass?.GetAttributeTypeName() ?? string.Empty;
        var displayName = GetDisplayName(attributeTypeName);
        var isRange = attributeTypeName.Equals(AttributeNames.CONDITIONAL_RANGE, StringComparison.OrdinalIgnoreCase);
        var isUnless = attributeTypeName.Equals(AttributeNames.REQUIRED_UNLESS, StringComparison.OrdinalIgnoreCase);
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

        var otherName = MetadataHelper.GetConstructorArgument<string>(attribute, 0);
        var otherMember = ValidateTarget(propertyName, otherName, targetType, location, displayName, diagnostics);
        if(otherMember is null) {
            return null;
        }

        var otherType = GetMemberType(otherMember);
        if(otherType is null) {
            ReportValueUnsupported(diagnostics, location, displayName, propertyName, $"member \"{otherName}\" has no comparable type");
            return null;
        }

        var value = GetConstructorConstant(attribute, 1);
        if(!TryFormatComparisonValue(otherType, value, out var literal, out var valueReason)) {
            ReportValueUnsupported(diagnostics, location, displayName, propertyName, valueReason);
            return null;
        }

        var conditionBody = $"x.{otherName} == {literal}";
        ParameterInfo? min = null;
        ParameterInfo? max = null;
        RuleKind kind;

        if(isRange) {
            var range = TryCreateRangeParameters(propertyName, propertySymbol, attribute, location, diagnostics);
            if(range is null) {
                return null;
            }

            (min, max) = range.Value;
            kind = RuleKind.InclusiveBetween;
        } else {
            kind = GetSimpleRuleKind(propertySymbol.Type);
        }

        return new RuleModel(
            kind,
            propertyName,
            propertySymbol.Type.GetFullTypeName(),
            Parameter1: min,
            Parameter2: max,
            Message: MetadataHelper.GetMessageArgument(attribute),
            WhenCondition: isUnless ? null : $"x => {conditionBody}",
            UnlessCondition: isUnless ? $"x => !({conditionBody})" : null);
    }

    private static ISymbol? ValidateTarget(
        string propertyName,
        string? otherName,
        INamedTypeSymbol targetType,
        Location? location,
        string displayName,
        List<GeneratorDiagnostic> diagnostics) {
        if(string.IsNullOrWhiteSpace(otherName)) {
            ReportTargetInvalid(diagnostics, location, displayName, propertyName, "the member name is empty");
            return null;
        }

        if(string.Equals(otherName, propertyName, StringComparison.Ordinal)) {
            ReportTargetInvalid(diagnostics, location, displayName, propertyName, "the attribute references the validated member itself");
            return null;
        }

        var otherMember = FindMember(targetType, otherName!);
        if(otherMember is null) {
            ReportTargetInvalid(
                diagnostics,
                location,
                displayName,
                propertyName,
                $"member \"{otherName}\" was not found on \"{targetType.ToDisplayString()}\"");
            return null;
        }

        if(!IsReadable(otherMember)) {
            ReportTargetInvalid(
                diagnostics,
                location,
                displayName,
                propertyName,
                $"member \"{otherName}\" is not readable from the generated validator");
            return null;
        }

        return otherMember;
    }

    private static ITypeSymbol? GetMemberType(ISymbol member) =>
        member switch {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };

    private static RuleKind GetSimpleRuleKind(ITypeSymbol propertyType) {
        if(propertyType.IsStringOrNullableString()) {
            return RuleKind.StringNotEmpty;
        }

        return IsCollection(propertyType) ? RuleKind.NotEmpty : RuleKind.NotNull;
    }

    private static (ParameterInfo? Min, ParameterInfo? Max)? TryCreateRangeParameters(
        string propertyName,
        IPropertySymbol propertySymbol,
        AttributeData attribute,
        Location? location,
        List<GeneratorDiagnostic> diagnostics) {
        var displayName = GetDisplayName(attribute.AttributeClass?.GetAttributeTypeName() ?? string.Empty);

        if(!CanUseRange(propertySymbol.Type)) {
            ReportValueUnsupported(
                diagnostics,
                location,
                displayName,
                propertyName,
                $"the member type \"{propertySymbol.Type.ToDisplayString()}\" does not support a range");
            return null;
        }

        var minConstant = GetConstructorConstant(attribute, 2);
        if(!TryFormatRangeBound(propertySymbol.Type, minConstant, out var minValue, out var minReason)) {
            ReportValueUnsupported(diagnostics, location, displayName, propertyName, $"minimum {minReason}");
            return null;
        }

        var maxConstant = GetConstructorConstant(attribute, 3);
        if(!TryFormatRangeBound(propertySymbol.Type, maxConstant, out var maxValue, out var maxReason)) {
            ReportValueUnsupported(diagnostics, location, displayName, propertyName, $"maximum {maxReason}");
            return null;
        }

        var typeName = propertySymbol.Type.GetFullTypeName();
        return (new ParameterInfo(minValue, typeName), new ParameterInfo(maxValue, typeName));
    }

    private static TypedConstant GetConstructorConstant(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index] : default;

    private static string GetDisplayName(string attributeTypeName) {
        var lastDot = attributeTypeName.LastIndexOf('.');
        var name = lastDot >= 0 ? attributeTypeName.Substring(lastDot + 1) : attributeTypeName;
        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "Attribute".Length)
            : name;
    }

    private static ISymbol? FindMember(INamedTypeSymbol type, string name) {
        for(var current = type; current is not null; current = current.BaseType) {
            var match = FindMemberIn(current, name);
            if(match is not null) {
                return match;
            }
        }

        foreach(var @interface in type.AllInterfaces) {
            var match = FindMemberIn(@interface, name);
            if(match is not null) {
                return match;
            }
        }

        return null;
    }

    private static ISymbol? FindMemberIn(INamedTypeSymbol type, string name) {
        foreach(var member in type.GetMembers(name)) {
            if(member is IPropertySymbol { GetMethod: not null } property) {
                return property;
            }

            if(member is IFieldSymbol field) {
                return field;
            }
        }

        return null;
    }

    private static bool IsReadable(ISymbol member) =>
        member switch {
            IPropertySymbol { IsStatic: false, GetMethod: { } getter } => IsAccessible(getter),
            IFieldSymbol { IsStatic: false } field => IsAccessible(field),
            _ => false
        };

    private static bool IsAccessible(ISymbol member) =>
        member.DeclaredAccessibility is Accessibility.Public or
            Accessibility.Internal or
            Accessibility.ProtectedOrInternal;

    private static bool IsCollection(ITypeSymbol type) =>
        type is IArrayTypeSymbol ||
        type.AllInterfaces.Any(static @interface =>
            @interface.SpecialType is SpecialType.System_Collections_IEnumerable ||
            @interface.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T);

    private static bool CanUseRange(ITypeSymbol type) {
        var unwrapped = type.UnwrapNullable();
        return unwrapped.IsStringOrNullableString() ||
               unwrapped.IsNumeric() ||
               unwrapped.IsDateOrTime() ||
               unwrapped.TypeKind is TypeKind.Enum ||
               IsSpecialType(unwrapped, "System.Guid") ||
               IsSpecialType(unwrapped, "System.TimeSpan") ||
               unwrapped.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char;
    }

    private static bool IsSpecialType(ITypeSymbol type, string fullName) =>
        string.Equals(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            "global::" + fullName,
            StringComparison.Ordinal);

    private static bool TryFormatComparisonValue(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string literal,
        out string reason) {
        literal = string.Empty;
        reason = string.Empty;

        if(constant.Kind is TypedConstantKind.Error) {
            reason = "the value must be a compile-time constant";
            return false;
        }

        var unwrapped = memberType.UnwrapNullable();

        if(constant.IsNull) {
            return TryFormatNullComparison(memberType, out literal, out reason);
        }

        if(unwrapped.IsStringOrNullableString()) {
            return TryFormatStringComparison(constant, out literal, out reason);
        }

        if(unwrapped.SpecialType is SpecialType.System_Boolean) {
            return TryFormatBooleanComparison(constant, out literal, out reason);
        }

        if(unwrapped.SpecialType is SpecialType.System_Char) {
            return TryFormatCharComparison(constant, out literal, out reason);
        }

        if(unwrapped.TypeKind is TypeKind.Enum) {
            return TryFormatEnumComparison(unwrapped, constant, out literal, out reason);
        }

        if(unwrapped.IsNumeric()) {
            return TryFormatNumericComparison(unwrapped, constant, out literal, out reason);
        }

        if(unwrapped.IsDateOrTime()) {
            return TryFormatDateTimeComparison(unwrapped, constant, out literal, out reason);
        }

        if(IsSpecialType(unwrapped, "System.Guid")) {
            return TryFormatGuidComparison(constant, out literal, out reason);
        }

        if(IsSpecialType(unwrapped, "System.TimeSpan")) {
            return TryFormatTimeSpanComparison(constant, out literal, out reason);
        }

        reason = $"member type \"{unwrapped.ToDisplayString()}\" cannot be compared with a constant";
        return false;
    }

    private static bool TryFormatNullComparison(ITypeSymbol memberType, out string literal, out string reason) {
        if(memberType.IsReferenceType || memberType.IsNullable()) {
            literal = "null";
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = $"member type \"{memberType.ToDisplayString()}\" does not accept null";
        return false;
    }

    private static bool TryFormatStringComparison(TypedConstant constant, out string literal, out string reason) {
        if(constant.Value is string text) {
            literal = RuleExpressionEmitter.FormatParam(text, "string");
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a string member requires a string value";
        return false;
    }

    private static bool TryFormatBooleanComparison(TypedConstant constant, out string literal, out string reason) {
        if(constant.Value is bool flag) {
            literal = flag ? "true" : "false";
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a Boolean member requires a Boolean value";
        return false;
    }

    private static bool TryFormatCharComparison(TypedConstant constant, out string literal, out string reason) {
        if(constant.Value is char character) {
            literal = FormatChar(character);
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a Char member requires a Char value";
        return false;
    }

    private static bool TryFormatEnumComparison(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string literal,
        out string reason) {
        if(IsNumericValue(constant.Value)) {
            literal = $"({memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({MetadataHelper.FormatConstant(constant.Value)})";
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "an enum member requires an enum or numeric value";
        return false;
    }

    private static bool TryFormatNumericComparison(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string literal,
        out string reason) {
        if(!IsNumericValue(constant.Value)) {
            literal = string.Empty;
            reason = $"member type \"{memberType.ToDisplayString()}\" requires a numeric value";
            return false;
        }

        if(IsIntegral(memberType) && HasFraction(constant.Value)) {
            literal = string.Empty;
            reason = $"the value cannot be represented by integral member type \"{memberType.ToDisplayString()}\"";
            return false;
        }

        literal = RuleExpressionEmitter.FormatParam(
            MetadataHelper.FormatConstant(constant.Value),
            memberType.GetFullTypeName());
        reason = string.Empty;
        return true;
    }

    private static bool TryFormatDateTimeComparison(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string literal,
        out string reason) {
        if(constant.Value is string dateText &&
           DateTimeRuleBuilder.TryFormatValue(dateText, memberType.GetFullTypeName(), out var formatted)) {
            literal = RuleExpressionEmitter.FormatParam(formatted, memberType.GetFullTypeName());
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a date/time member requires a parseable string value";
        return false;
    }

    private static bool TryFormatGuidComparison(TypedConstant constant, out string literal, out string reason) {
        if(constant.Value is string guidText && Guid.TryParse(guidText, out _)) {
            literal = $"global::System.Guid.Parse(\"{guidText}\")";
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a Guid member requires a Guid string value";
        return false;
    }

    private static bool TryFormatTimeSpanComparison(TypedConstant constant, out string literal, out string reason) {
        if(constant.Value is string spanText && TimeSpan.TryParse(spanText, CultureInfo.InvariantCulture, out _)) {
            literal = RuleExpressionEmitter.FormatParam(spanText, "timespan");
            reason = string.Empty;
            return true;
        }

        literal = string.Empty;
        reason = "a TimeSpan member requires a time-span string value";
        return false;
    }

    private static bool TryFormatRangeBound(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string value,
        out string reason) {
        value = string.Empty;
        reason = string.Empty;

        if(constant.Kind is TypedConstantKind.Error || constant.IsNull) {
            reason = "must be a compile-time constant and cannot be null";
            return false;
        }

        var unwrapped = memberType.UnwrapNullable();

        if(unwrapped.IsStringOrNullableString()) {
            return TryFormatRangeString(constant, out value, out reason);
        }

        if(unwrapped.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char) {
            return TryFormatRangeBooleanOrChar(constant, out value, out reason);
        }

        if(unwrapped.TypeKind is TypeKind.Enum) {
            return TryFormatRangeEnum(constant, out value, out reason);
        }

        if(unwrapped.IsNumeric()) {
            return TryFormatRangeNumeric(unwrapped, constant, out value, out reason);
        }

        if(unwrapped.IsDateOrTime()) {
            return TryFormatRangeDateTime(unwrapped, constant, out value, out reason);
        }

        if(IsSpecialType(unwrapped, "System.Guid")) {
            return TryFormatRangeGuid(constant, out value, out reason);
        }

        if(IsSpecialType(unwrapped, "System.TimeSpan")) {
            return TryFormatRangeTimeSpan(constant, out value, out reason);
        }

        reason = $"member type \"{unwrapped.ToDisplayString()}\" does not support a range";
        return false;
    }

    private static bool TryFormatRangeString(TypedConstant constant, out string value, out string reason) {
        if(constant.Value is string text) {
            value = text;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires a string bound for a string member";
        return false;
    }

    private static bool TryFormatRangeBooleanOrChar(TypedConstant constant, out string value, out string reason) {
        if(constant.Value is bool or char) {
            value = MetadataHelper.FormatConstant(constant.Value) ?? string.Empty;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires a matching bound type";
        return false;
    }

    private static bool TryFormatRangeEnum(TypedConstant constant, out string value, out string reason) {
        if(IsNumericValue(constant.Value)) {
            value = MetadataHelper.FormatConstant(constant.Value) ?? string.Empty;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires an enum or numeric bound";
        return false;
    }

    private static bool TryFormatRangeNumeric(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string value,
        out string reason) {
        if(!IsNumericValue(constant.Value)) {
            value = string.Empty;
            reason = "requires a numeric bound";
            return false;
        }

        if(IsIntegral(memberType) && HasFraction(constant.Value)) {
            value = string.Empty;
            reason = $"cannot be represented by integral member type \"{memberType.ToDisplayString()}\"";
            return false;
        }

        value = MetadataHelper.FormatConstant(constant.Value) ?? string.Empty;
        reason = string.Empty;
        return true;
    }

    private static bool TryFormatRangeDateTime(
        ITypeSymbol memberType,
        TypedConstant constant,
        out string value,
        out string reason) {
        if(constant.Value is string dateText &&
           DateTimeRuleBuilder.TryFormatValue(dateText, memberType.GetFullTypeName(), out var formatted)) {
            value = formatted;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires a parseable date/time string bound";
        return false;
    }

    private static bool TryFormatRangeGuid(TypedConstant constant, out string value, out string reason) {
        if(constant.Value is string guidText && Guid.TryParse(guidText, out _)) {
            value = guidText;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires a Guid string bound";
        return false;
    }

    private static bool TryFormatRangeTimeSpan(TypedConstant constant, out string value, out string reason) {
        if(constant.Value is string spanText && TimeSpan.TryParse(spanText, CultureInfo.InvariantCulture, out _)) {
            value = spanText;
            reason = string.Empty;
            return true;
        }

        value = string.Empty;
        reason = "requires a time-span string bound";
        return false;
    }

    private static bool IsNumericValue(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsIntegral(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64;

    private static bool HasFraction(object? value) =>
        value switch {
            decimal number => number != decimal.Truncate(number),
            double number => Math.Abs(number - Math.Truncate(number)) > 1e-12,
            float number => Math.Abs(number - (float)Math.Truncate(number)) > 1e-6f,
            _ => false
        };

    private static string FormatChar(char value) =>
        value switch {
            '\\' => "'\\\\'",
            '\'' => "'\\''",
            '\n' => "'\\n'",
            '\r' => "'\\r'",
            '\t' => "'\\t'",
            _ => $"'{value}'"
        };

    private static void ReportTargetInvalid(
        List<GeneratorDiagnostic> diagnostics,
        Location? location,
        string displayName,
        string propertyName,
        string reason) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.ConditionalTargetInvalid,
            location,
            [displayName, propertyName, reason]));

    private static void ReportValueUnsupported(
        List<GeneratorDiagnostic> diagnostics,
        Location? location,
        string displayName,
        string propertyName,
        string reason) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.ConditionalValueUnsupported,
            location,
            [displayName, propertyName, reason]));
}
