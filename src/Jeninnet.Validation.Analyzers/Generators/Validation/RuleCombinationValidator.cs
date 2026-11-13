//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Validation;

internal static class RuleCombinationValidator {
    public static void ValidatePropertyRules(
        SourceProductionContext context,
        Compilation compilation,
        string propertyName,
        ITypeSymbol propertyType,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        Location? attributeLocation,
        IPropertySymbol propertySymbol
    ) {
        ValidateStructuralRules(context, compilation, rules, propertyName, propertyType, attributeLocation);
        ValidateNullabilityRules(context, compilation, rules, propertyName, propertyType, attributeLocation);

        // 5. Conflicting numeric ranges
        ValidateComparisonConflicts(context, compilation, rules, propertyName, propertyType, attributeLocation);

        // 6. Conflicting string rules
        ValidateStringConflicts(context, compilation, rules, propertyName, propertyType, attributeLocation);

        ValidateDuplicateRules(context, compilation, rules, propertyName, attributeLocation);

        // 8. Property comparison validation
        ValidatePropertyComparison(context, compilation, rules, propertyName, propertyType, attributeLocation, propertySymbol);
    }

    private static void ValidateStructuralRules(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        ITypeSymbol propertyType,
        Location? attributeLocation
    ) {
        // 1. Detect ValidateNested + ValidateEach conflict
        if(rules.Any(r => r.Kind == RuleKind.Nested) && rules.Any(r => r.Kind == RuleKind.ForEach)) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.InvalidRuleCombination,
                attributeLocation,
                RuleKind.Nested.GetAttributeName(),
                RuleKind.ForEach.GetAttributeName(),
                propertyName,
                string.Empty    // Reason
            );
        }

        // 2. ValidateNested only allowed on complex types
        if(rules.Any(r => r.Kind == RuleKind.Nested) && propertyType.IsPrimitive()) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                attributeLocation,
                RuleKind.Nested.GetAttributeName(),
                propertyName,
                propertyType.ToDisplayString(),
                $", {RuleKind.Nested.GetAttributeName()} only allowed on complex types" // The reason
            );
        }

        // 3. ValidateEach only allowed on collections
        if(rules.Any(r => r.Kind == RuleKind.ForEach) && !propertyType.IsCollection(compilation)) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                attributeLocation,
                AttributeNames.SIMPLE_FOR_EACH,
                propertyName,
                propertyType.ToDisplayString(),
                $", {RuleKind.ForEach.GetAttributeName()} only allowed on collections" // The reason
            );
        }

        // 3b. UniqueItems only allowed on collections
        if(rules.Any(r => r.Kind == RuleKind.UniqueItems) &&
           (propertyType.IsStringOrNullableString() || !propertyType.IsCollection(compilation))) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                attributeLocation,
                AttributeNames.SIMPLE_UNIQUE_ITEMS,
                propertyName,
                propertyType.ToDisplayString(),
                ", UniqueItems only allowed on collections");
        }
    }

    private static void ValidateNullabilityRules(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        ITypeSymbol propertyType,
        Location? attributeLocation
    ) {
        // 4. Numeric rules on nullable types without NotNull
        if(propertyType.IsNullableNumeric()) {
            var comparisonRules = rules.Where(r => r.Kind.IsComparisonRule()).ToList();
            var hasNotNull = rules.Any(r => r.Kind == RuleKind.NotNull);

            if(comparisonRules.Count != 0 && !hasNotNull) {
                Report(
                    context,
                    compilation,
                    DiagnosticDescriptors.RuleNotAllowedOnNullableType,
                    attributeLocation,
                    string.Join(", ", comparisonRules.Select(r => r.Kind.GetAttributeName())),
                    propertyType.ToDisplayString(),
                    propertyName,
                    AttributeNames.SIMPLE_NOT_NULL
                );
            }
        }

        // 4b. NotNull on non-nullable types (Redundancy)
        var fullTypeName = propertyType.GetFullTypeName().Replace("?", "");
        var isKnownNonNullablePrimitive = fullTypeName is
            "int" or "long" or "short" or "byte" or "uint" or "ulong" or "ushort" or "sbyte" or "decimal" or "double" or "float" or "bool" or "char" or
            "Int32" or "Int64" or "Int16" or "Byte" or "UInt32" or "UInt64" or "UInt16" or "SByte" or "Decimal" or "Double" or "Single" or "Boolean" or "Char" or "Guid" or "DateTime" or "DateOnly" or "TimeOnly" or
            "System.Int32" or "System.Int64" or "System.Decimal" or "System.Boolean" or "System.Guid" or "System.DateTime" or "System.DateOnly" or "System.TimeOnly";

        var isNonNullableValueType = (propertyType.IsValueType || propertyType.IsPrimitive() || isKnownNonNullablePrimitive) && !propertyType.IsNullable();

        if(isNonNullableValueType) {
            var hasNotNull = rules.Any(r => r.Kind == RuleKind.NotNull);

            if(hasNotNull) {
                Report(
                   context,
                   compilation,
                   DiagnosticDescriptors.RedundantNotNull,
                   attributeLocation,
                   propertyName
               );
            }
        }
    }

    private static void ValidateDuplicateRules(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        Location? attributeLocation
    ) {
        // 7. Duplicate rules
        var duplicates = rules.Where(static rule => rule.Kind is not RuleKind.Custom)
                              .GroupBy(r => r.Kind)
                              .Where(g => g.Count() > 1)
                              .ToList();

        foreach(var dup in duplicates) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.ConflictingRules,
                attributeLocation,
                propertyName,
                string.Join(", ", dup.Select(r => r.Kind.ToString()))
            );
        }
    }

    private static void ValidatePropertyComparison(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        ITypeSymbol propertyType,
        Location? location,
        IPropertySymbol propertySymbol
    ) {
        var propertyComparisonRules = rules.Where(r => r.Kind.IsPropertyComparisonRule()).ToList();
        if(propertyComparisonRules.Count == 0) {
            return;
        }

        var containingType = propertySymbol.ContainingType;
        if(containingType is null) {
            return;
        }

        foreach(var rule in propertyComparisonRules) {
            ValidateSinglePropertyComparison(context, compilation, rule, propertyName, propertyType, location, containingType);
        }
    }

    private static void ValidateSinglePropertyComparison(
        SourceProductionContext context,
        Compilation compilation,
        ValidationRuleDescriptor rule,
        string propertyName,
        ITypeSymbol propertyType,
        Location? location,
        INamedTypeSymbol containingType
    ) {
        if(rule is not ParametricRuleDescriptor parametric || parametric.Parameters.Count == 0) {
            return;
        }

        var otherPropName = parametric.Parameters[0].Value;
        if(otherPropName is null or { Length: 0 }) {
            return;
        }

        var otherProp = containingType.GetMembers(otherPropName).FirstOrDefault(m => m is IPropertySymbol or IFieldSymbol);
        if(otherProp is null) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.TargetPropertyNotFound,
                location,
                rule.Kind.GetAttributeName(),
                otherPropName,
                containingType.Name
            );
            return;
        }

        var otherPropType = otherProp switch {
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            _ => throw new InvalidOperationException()
        };

        if(!IsCompatible(propertyType, otherPropType, compilation)) {
            Report(
               context,
               compilation,
               DiagnosticDescriptors.RuleAppliedToIncompatibleType,
               location,
               rule.Kind.GetAttributeName(),
               propertyName,
               propertyType.ToDisplayString(),
               $", target property '{otherPropName}' has incompatible type '{otherPropType.ToDisplayString()}'"
           );
        }
    }

    private static bool IsCompatible(ITypeSymbol t1, ITypeSymbol t2, Compilation compilation) {
        if(SymbolEqualityComparer.Default.Equals(t1, t2)) {
            return true;
        }

        // Handle Nullables by getting underlying type
        var nt1 = t1;
        if(t1 is INamedTypeSymbol n1 && n1.IsGenericType && n1.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            nt1 = n1.TypeArguments[0];
        }

        var nt2 = t2;
        if(t2 is INamedTypeSymbol n2 && n2.IsGenericType && n2.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
            nt2 = n2.TypeArguments[0];
        }

        if(SymbolEqualityComparer.Default.Equals(nt1, nt2)) {
            return true;
        }

        // If both are numeric
        if(nt1.IsNumeric() && nt2.IsNumeric()) {
            return true;
        }

        // If both are date/time
        if(nt1.IsDateOrTime() && nt2.IsDateOrTime()) {
            return true;
        }

        // DateTimeOffset check
        var dto = compilation.GetTypeByMetadataName("System.DateTimeOffset");

        return dto is not null && SymbolEqualityComparer.Default.Equals(nt1, dto) && SymbolEqualityComparer.Default.Equals(nt2, dto);
    }

    private static void ValidateComparisonConflicts(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        ITypeSymbol propertyType,
        Location? location
    ) {
        // Strings implement IComparable, but we don't support numeric/date-like comparison rules on them.
        // We only allow Equality rules (Equal, NotEqual) on strings if they are not numeric/date/comparable.
        var isString = propertyType.IsStringOrNullableString();
        var isNumeric = propertyType.IsNumeric() || propertyType.IsNullableNumeric();
        var isDate = propertyType.IsDateOrTime() || propertyType.IsNullableDateOrTime();
        var isComparable = propertyType.IsComparable(compilation);

        if(!isNumeric && !isDate && (!isComparable || isString)) {
            ReportIncompatibleType(context, compilation, rules.Where(r => r.Kind.IsComparisonRule() && !r.Kind.IsEqualityRule()).Select(rule => rule.Kind), propertyName, propertyType, location, "numeric, date/time, or comparable types (excluding strings).");

            // For equality rules, they support strings too, so we only report if it's NOT a string either
            if(!isString) {
                ReportIncompatibleType(context, compilation, rules.Where(r => r.Kind.IsEqualityRule()).Select(rule => rule.Kind), propertyName, propertyType, location, "numeric, date/time, or string types.");
            }
        }

        Validate_GreaterThan_LessThan_Conflicts(context, compilation, rules, propertyName, location);

        var ranges = rules.Where(r => r.Kind is RuleKind.InclusiveBetween or RuleKind.ExclusiveBetween or RuleKind.Range).ToList();

        if(ranges.Count > 1) {
            Report(
               context,
               compilation,
               DiagnosticDescriptors.ConflictingRules,
               location,
               propertyName,
               string.Join(" and ", ranges.Select(r => r.Kind.ToString()))
           );
        }

        foreach(var range in ranges) {
            ValidateRangeParameters(context, compilation, range, propertyName, propertyType, location);
        }
    }

    private static void ReportIncompatibleType(SourceProductionContext context, Compilation compilation, IEnumerable<RuleKind> kinds, string propertyName, ITypeSymbol propertyType, Location? location, string expectedTypes) {
        foreach(var ruleKind in kinds) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                location,
                ruleKind.ToString(),
                propertyName,
                propertyType.ToDisplayString(),
                $", the attribute '{ruleKind}' only supports {expectedTypes}"
            );
        }
    }

    private static void ValidateRangeParameters(
        SourceProductionContext context,
        Compilation compilation,
        ValidationRuleDescriptor range,
        string propertyName,
        ITypeSymbol propertyType,
        Location? location
    ) {
        if(range is not ParametricRuleDescriptor p || p.Parameters.Count < 2 ||
           !decimal.TryParse(p.Parameters[0].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var min) ||
           !decimal.TryParse(p.Parameters[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var max)) {
            return;
        }

        if(min > max) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.RuleParameterOutOfRange,
                location,
                $"{min} > {max}",
                range.Kind.ToString(),
                propertyType.ToDisplayString()
            );

            return;
        }

        if(min == max && range.Kind is RuleKind.ExclusiveBetween) {
            Report(
                context,
                compilation,
                DiagnosticDescriptors.DegenerateExclusiveRange,
                location,
                range.Kind.GetAttributeName(),
                propertyName,
                min.ToString(CultureInfo.InvariantCulture)
            );
        }
    }

    private static void Validate_GreaterThan_LessThan_Conflicts(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        Location? location
    ) {
        var greaterThan = rules.FirstOrDefault(r => r.Kind is RuleKind.GreaterThan or RuleKind.GreaterThanOrEqual) as ParametricRuleDescriptor;
        var lessThan = rules.FirstOrDefault(r => r.Kind is RuleKind.LessThan or RuleKind.LessThanOrEqual) as ParametricRuleDescriptor;

        if(greaterThan is not null && lessThan is not null &&
           greaterThan.Parameters.Count > 0 && lessThan.Parameters.Count > 0 &&
           decimal.TryParse(greaterThan.Parameters[0].Value, out var gtVal) &&
           decimal.TryParse(lessThan.Parameters[0].Value, out var ltVal)) {
            if(gtVal > ltVal) {
                Report(
                    context,
                    compilation,
                    DiagnosticDescriptors.ConflictingRules,
                    location,
                    propertyName,
                    $"{greaterThan.Kind}({gtVal}) and {lessThan.Kind}({ltVal}) create an impossible range."
                );
            } else {
                Report(
                    context,
                    compilation,
                    DiagnosticDescriptors.SimplifyRuls,
                    location,
                    propertyName,
                    "'InclusiveBetween' or 'ExclusiveBetween'",
                    $"'{greaterThan.Kind}' and '{lessThan.Kind}'"
                );
            }
        }
    }

    private static void ValidateStringConflicts(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyList<ValidationRuleDescriptor> rules,
        string propertyName,
        ITypeSymbol propertyType,
        Location? location
    ) {
        if(!propertyType.IsStringOrNullableString()) {
            foreach(var ruleKind in rules.Where(static x => x.Kind.IsStringRule()).Select(x => x.Kind)) {
                Report(
                    context,
                    compilation,
                    DiagnosticDescriptors.RuleAppliedToIncompatibleType,
                    location,
                    ruleKind.ToString(),
                    propertyName,
                    propertyType.ToDisplayString(),
                    $", the attribute '{ruleKind}' only supports the 'string' type." // The reason
                );
            }
        }

        var length = rules.FirstOrDefault(r => r.Kind == RuleKind.Length) as ParametricRuleDescriptor;
        var notEmpty = rules.FirstOrDefault(r => r.Kind is RuleKind.NotEmpty or RuleKind.StringNotEmpty);

        if(length is not null && notEmpty is not null &&
           length.Parameters.Count > 0 &&
           int.TryParse(length.Parameters[0].Value, out var min) && min == 0) {
            Report(
                context, compilation,
                DiagnosticDescriptors.InvalidRuleCombination,
                location,
                "Length(min=0)",
                notEmpty.Kind.ToString(),
                propertyName,
                ". The 'min=0' is equal to the 'NotEmpty' attribute. You can either delete the 'min' limit or increase it."
            );
        }
    }

    private static void Report(
            SourceProductionContext context,
            Compilation? compilation,
            DiagnosticDescriptor descriptor,
            Location? location,
            params object[] messageArgs
        ) {
        var finalLocation = location ?? Location.None;

        if(finalLocation.SourceTree is not null && compilation?.ContainsSyntaxTree(finalLocation.SourceTree) == false) {
            finalLocation = Location.None;
        }

        context.ReportDiagnostic(Diagnostic.Create(descriptor, finalLocation, messageArgs));
    }
}
