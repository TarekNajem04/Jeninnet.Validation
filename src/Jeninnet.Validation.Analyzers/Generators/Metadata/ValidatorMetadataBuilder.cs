//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Metadata;

/// <summary>
/// Builds validator metadata models from Roslyn symbols.
/// This is used by the source generator to produce compiled validators.
/// </summary>
internal static class ValidatorMetadataBuilder {
    private const string ATTRIBUTE_SUFFIX = "Attribute";

    /// <summary>
    /// Attempts to build a <see cref="ValidatorDescriptor"/> for the specified type symbol.
    /// Returns <see langword="null"/> if the type is not annotated with <c>[Validate]</c>.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to build metadata for.</param>
    public static ValidatorDescriptor? TryBuildValidatorDescriptor(INamedTypeSymbol typeSymbol) {
        var validateAttribute = GetValidateAttribute(typeSymbol);

        if(validateAttribute is null) {
            return null;
        }

        var targetTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var targetNamespace = MetadataHelper.GetConstructorArgument<string>(validateAttribute, 0);
        var generateModelExtensions =
            !MetadataHelper.TryGetNamedArgument<bool>(validateAttribute, "GenerateModelExtensions", out var configured) ||
            configured;
        var diagnostics = new List<GeneratorDiagnostic>();

        if(HasSkipValidation(typeSymbol)) {
            return new ValidatorDescriptor(
                targetNamespace,
                targetTypeName,
                [],
                Diagnostics: diagnostics) {
                ShouldEmit = false
            };
        }

        if(typeSymbol.IsGenericType) {
            diagnostics.Add(new GeneratorDiagnostic(
                DiagnosticDescriptors.UnsupportedGenericModel,
                typeSymbol.Locations.FirstOrDefault(),
                [typeSymbol.ToDisplayString()]));

            return new ValidatorDescriptor(
                targetNamespace,
                targetTypeName,
                [],
                Diagnostics: diagnostics) {
                ShouldEmit = false
            };
        }

        var classOptions = BuildClassRuleOptions(typeSymbol, diagnostics);
        var propertyDescriptors = BuildPropertyDescriptor(typeSymbol, diagnostics, classOptions).ToArray();
        var modelRules = BuildModelRules(typeSymbol, diagnostics, classOptions).Select(Map).ToArray();
        var (preValidate, preValidateHasErrors, onFailure) = BuildLifecycleHooks(typeSymbol, diagnostics);
        var sanitizers = BuildSanitizers(typeSymbol, diagnostics);

        return new ValidatorDescriptor(
            targetNamespace,
            targetTypeName,
            propertyDescriptors,
            modelRules,
            preValidate,
            preValidateHasErrors,
            onFailure,
            diagnostics,
            Sanitizers: sanitizers,
            GenerateModelExtensions: generateModelExtensions);
    }

    private static AttributeData? GetValidateAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes()
                  .FirstOrDefault(attribute => {
                      var attributeClass = attribute.AttributeClass;
                      if(attributeClass is null) {
                          return false;
                      }

                      var name = attributeClass.GetAttributeTypeName();
                      return name.Equals(AttributeNames.VALIDATE, StringComparison.OrdinalIgnoreCase) ||
                             name.Equals(AttributeNames.SIMPLE_VALIDATE, StringComparison.OrdinalIgnoreCase) ||
                             name.Equals(AttributeNames.SIMPLE_VALIDATE + ATTRIBUTE_SUFFIX, StringComparison.OrdinalIgnoreCase);
                  });

    /// <summary>
    /// Builds the property descriptors for the target type, including properties declared on base
    /// classes and, when explicitly annotated with <c>[Validate]</c>, on implemented interfaces.
    /// </summary>
    /// <param name="typeSymbol">The target type whose properties are described.</param>
    /// <param name="diagnostics">The sink that receives metadata diagnostics.</param>
    /// <param name="classOptions">The class-level validation options.</param>
    private static IEnumerable<ValidatorPropertyDescriptor> BuildPropertyDescriptor(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics,
        ClassRuleOptions classOptions) {
        foreach(var entry in CollectPropertyEntries(typeSymbol)) {
            var descriptor = TryBuildPropertyDescriptor(entry, typeSymbol, classOptions, diagnostics);
            if(descriptor is not null) {
                yield return descriptor;
            }
        }
    }

    private static ValidatorPropertyDescriptor? TryBuildPropertyDescriptor(
        PropertyEntry entry,
        INamedTypeSymbol typeSymbol,
        ClassRuleOptions classOptions,
        List<GeneratorDiagnostic> diagnostics) {
        if(entry.Sources.Any(static source => HasSkipValidation(source.Property))) {
            ReportSkipValidationConflict(entry, diagnostics);
            return null;
        }

        var propertyName = entry.Property.Name;
        var propertyDisplayName = GetPropertyDisplayName(entry);
        var rules = new List<RuleModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var conditionalTargets = new HashSet<string>(StringComparer.Ordinal);

        foreach(var source in entry.Sources) {
            var propertyRuleSets = GetRuleSets(source.Property);
            var (propWhen, propUnless) = GetAsyncConditions(source.Property);
            var (propSyncWhen, propSyncWhenContext, propSyncUnless, propSyncUnlessContext) =
                GetSyncConditions(source.Property, typeSymbol, diagnostics);

            foreach(var attribute in source.Attributes) {
                ReportDuplicateConditionalAttribute(attribute, propertyName, conditionalTargets, diagnostics);

                var rule = TryCreateRuleModelFromAttribute(
                    propertyName,
                    attribute,
                    source.Property,
                    typeSymbol,
                    diagnostics);
                if(rule is null) {
                    continue;
                }

                var (whenCondition, whenHasContext) =
                    ResolveWhenCondition(rule, propSyncWhen, propSyncWhenContext, classOptions);
                var (unlessCondition, unlessHasContext) =
                    ResolveUnlessCondition(rule, propSyncUnless, propSyncUnlessContext, classOptions);

                rule = rule with {
                    PropertyTypeName = entry.Property.Type.GetFullTypeName(),
                    WhenAsyncCondition = propWhen ?? classOptions.WhenAsync,
                    UnlessAsyncCondition = propUnless ?? classOptions.UnlessAsync,
                    WhenCondition = whenCondition,
                    WhenConditionHasContext = whenHasContext,
                    UnlessCondition = unlessCondition,
                    UnlessConditionHasContext = unlessHasContext,
                    RuleSets = ResolveRuleSets(attribute, propertyRuleSets, classOptions),
                    Severity = MetadataHelper.GetNamedSeverity(attribute),
                    ErrorCode = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "ErrorCode")),
                    DisplayName = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "DisplayName"))
                                  ?? propertyDisplayName,
                    OverridePropertyName = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "OverridePropertyName")),
                    CustomState = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "CustomState"))
                };

                if(seen.Add(GetRuleKey(rule))) {
                    rules.Add(rule);
                }
            }
        }

        return new ValidatorPropertyDescriptor(entry.Property, [.. rules.Select(Map)]);
    }

    private static void ReportSkipValidationConflict(PropertyEntry entry, List<GeneratorDiagnostic> diagnostics) {
        if(!entry.Sources.Any(static source => source.Attributes.Any(static attribute =>
            attribute.AttributeClass is { } attributeClass && IsValidationAttribute(attributeClass)))) {
            return;
        }

        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.SkipValidationConflict,
            entry.Property.Locations.FirstOrDefault(),
            [entry.Property.Name]));
    }

    private static void ReportDuplicateConditionalAttribute(
        AttributeData attribute,
        string propertyName,
        HashSet<string> conditionalTargets,
        List<GeneratorDiagnostic> diagnostics) {
        if(TryGetConditionalAttributeName(attribute) is not { } conditionalName) {
            return;
        }

        var target = Normalize(MetadataHelper.GetConstructorArgument<string>(attribute, 0));
        if(target is not null && !conditionalTargets.Add($"{conditionalName}|{target}")) {
            diagnostics.Add(new GeneratorDiagnostic(
                DiagnosticDescriptors.ConditionalAttributeDuplicate,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                [GetConditionalDisplayName(conditionalName), propertyName, target]));
        }
    }

    private static (string? When, bool HasContext) ResolveWhenCondition(
        RuleModel rule,
        string? syncWhen,
        bool syncWhenHasContext,
        ClassRuleOptions classOptions) {
        var baseWhen = syncWhen ?? classOptions.When;
        var baseWhenHasContext = syncWhen is not null ? syncWhenHasContext : classOptions.WhenHasContext;
        var condition = CombineConditions(
            rule.WhenCondition,
            rule.WhenConditionHasContext,
            baseWhen,
            baseWhenHasContext,
            out var hasContext);

        return (condition, hasContext);
    }

    private static (string? Unless, bool HasContext) ResolveUnlessCondition(
        RuleModel rule,
        string? syncUnless,
        bool syncUnlessHasContext,
        ClassRuleOptions classOptions) {
        var baseUnless = syncUnless ?? classOptions.Unless;
        var baseUnlessHasContext = syncUnless is not null ? syncUnlessHasContext : classOptions.UnlessHasContext;
        var condition = CombineConditions(
            rule.UnlessCondition,
            rule.UnlessConditionHasContext,
            baseUnless,
            baseUnlessHasContext,
            out var hasContext);

        return (condition, hasContext);
    }

    private static IReadOnlyList<string>? ResolveRuleSets(
        AttributeData attribute,
        IReadOnlyList<string>? propertyRuleSets,
        ClassRuleOptions classOptions) {
        var ruleSet = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "RuleSet"));
        return ruleSet is not null ? [ruleSet] : propertyRuleSets ?? classOptions.RuleSets;
    }

    private static string? GetPropertyDisplayName(PropertyEntry entry) {
        foreach(var source in entry.Sources) {
            var displayName = GetPropertyDisplayName(source.Property);

            if(displayName is not null) {
                return displayName;
            }
        }

        return null;
    }

    private static string? GetPropertyDisplayName(IPropertySymbol property) {
        foreach(var attribute in property.GetAttributes()) {
            var attributeType = attribute.AttributeClass?.GetAttributeTypeName();

            if(attributeType == "System.ComponentModel.DataAnnotations.DisplayAttribute") {
                var name = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "Name"));

                if(name is not null) {
                    return name;
                }
            }

            if(attributeType == "System.ComponentModel.DisplayNameAttribute") {
                var name = Normalize(MetadataHelper.GetConstructorArgument<string>(attribute, 0));

                if(name is not null) {
                    return name;
                }
            }
        }

        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static List<PropertyEntry> CollectPropertyEntries(INamedTypeSymbol typeSymbol) {
        var entries = new List<PropertyEntry>();
        var byName = new Dictionary<string, int>(StringComparer.Ordinal);

        if(typeSymbol.TypeKind is TypeKind.Interface) {
            foreach(var type in GetInterfaceHierarchy(typeSymbol)) {
                AddInterfaceProperties(type, entries, byName);
            }

            return entries;
        }

        foreach(var type in GetClassHierarchy(typeSymbol)) {
            AddClassProperties(type, entries, byName);
        }

        // Interface rules participate only when the interface is explicitly annotated with
        // [Validate]. This keeps the generator's opt-in policy and avoids inheriting arbitrary
        // framework contracts.
        foreach(var interfaceType in GetAnnotatedInterfaces(typeSymbol)) {
            foreach(var property in interfaceType.GetMembers().OfType<IPropertySymbol>()) {
                if(!byName.TryGetValue(property.Name, out var index)) {
                    continue;
                }

                var entry = entries[index];
                if(TypesMatch(property.Type, entry.Property.Type)) {
                    entry.Sources.Add(new PropertyRuleSource(property, property.GetAttributes()));
                }
            }
        }

        return entries;
    }

    private static void AddClassProperties(
        INamedTypeSymbol typeSymbol,
        List<PropertyEntry> entries,
        Dictionary<string, int> byName) {
        foreach(var property in typeSymbol.GetMembers().OfType<IPropertySymbol>()) {
            if(!byName.TryGetValue(property.Name, out var index)) {
                byName[property.Name] = entries.Count;
                entries.Add(new PropertyEntry(property));
                continue;
            }

            var entry = entries[index];

            if(property.IsOverride && IsOverrideOf(property, entry.Property) &&
               TypesMatch(property.Type, entry.Property.Type)) {
                entry.Property = property;
                entry.Sources.Add(new PropertyRuleSource(property, property.GetAttributes()));
                continue;
            }

            // A hidden ("new") property or a type-changing override replaces the base declaration;
            // the generated accessor can address only the most-derived member.
            entries[index] = new PropertyEntry(property);
        }
    }

    private static void AddInterfaceProperties(
        INamedTypeSymbol typeSymbol,
        List<PropertyEntry> entries,
        Dictionary<string, int> byName) {
        foreach(var property in typeSymbol.GetMembers().OfType<IPropertySymbol>()) {
            if(!byName.TryGetValue(property.Name, out var index)) {
                byName[property.Name] = entries.Count;
                entries.Add(new PropertyEntry(property));
                continue;
            }

            var entry = entries[index];

            if(TypesMatch(property.Type, entry.Property.Type)) {
                entry.Sources.Add(new PropertyRuleSource(property, property.GetAttributes()));
                continue;
            }

            entries[index] = new PropertyEntry(property);
        }
    }

    private static List<INamedTypeSymbol> GetClassHierarchy(INamedTypeSymbol typeSymbol) {
        var hierarchy = new Stack<INamedTypeSymbol>();

        for(var current = typeSymbol;
            current is not null && current.SpecialType is not SpecialType.System_Object;
            current = current.BaseType) {
            hierarchy.Push(current);
        }

        return [.. hierarchy];
    }

    private static List<INamedTypeSymbol> GetInterfaceHierarchy(INamedTypeSymbol typeSymbol) {
        var hierarchy = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        Visit(typeSymbol);
        return hierarchy;

        void Visit(INamedTypeSymbol current) {
            if(!visited.Add(current)) {
                return;
            }

            foreach(var interfaceType in current.Interfaces) {
                Visit(interfaceType);
            }

            hierarchy.Add(current);
        }
    }

    private static List<INamedTypeSymbol> GetAnnotatedInterfaces(INamedTypeSymbol typeSymbol) {
        var interfaces = new List<INamedTypeSymbol>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach(var type in GetClassHierarchy(typeSymbol)) {
            foreach(var interfaceType in type.Interfaces) {
                Visit(interfaceType);
            }
        }

        return interfaces;

        void Visit(INamedTypeSymbol current) {
            if(!visited.Add(current)) {
                return;
            }

            foreach(var baseInterface in current.Interfaces) {
                Visit(baseInterface);
            }

            if(GetValidateAttribute(current) is not null) {
                interfaces.Add(current);
            }
        }
    }

    private static bool IsOverrideOf(IPropertySymbol candidate, IPropertySymbol baseProperty) {
        for(var current = candidate.OverriddenProperty; current is not null; current = current.OverriddenProperty) {
            if(SymbolEqualityComparer.Default.Equals(current, baseProperty)) {
                return true;
            }
        }

        return false;
    }

    private static bool TypesMatch(ITypeSymbol left, ITypeSymbol right) =>
        SymbolEqualityComparer.Default.Equals(left, right);

    private static string GetRuleKey(RuleModel rule) =>
        string.Join(
            "|",
            rule.Kind,
            rule.PropertyName,
            rule.Parameter1?.Name,
            rule.Parameter1?.Type,
            rule.Parameter2?.Name,
            rule.Parameter2?.Type,
            rule.Parameter3?.Name,
            rule.Parameter3?.Type,
            rule.Message,
            rule.TargetTypeName,
            rule.WhenAsyncCondition,
            rule.UnlessAsyncCondition,
            string.Join(",", rule.RuleSets ?? []),
            rule.Severity,
            rule.ErrorCode,
            rule.DisplayName,
            rule.OverridePropertyName,
            rule.CustomState,
            rule.WhenCondition,
            rule.WhenConditionHasContext,
            rule.UnlessCondition,
            rule.UnlessConditionHasContext,
            rule.CustomMethodExpression,
            rule.CustomReturnsResult,
            rule.CustomHasServiceProvider,
            rule.IsModelRule);

    private static (string? when, string? unless) GetAsyncConditions(ISymbol symbol) {
        string? when = null;
        string? unless = null;

        foreach(var attr in symbol.GetAttributes()) {
            var name = attr.AttributeClass?.GetAttributeTypeName();
            if(name == AttributeNames.WHEN_ASYNC_CONDITION) {
                when = MetadataHelper.GetConstructorArgument<string>(attr, 0);
            }

            if(name == AttributeNames.UNLESS_ASYNC_CONDITION) {
                unless = MetadataHelper.GetConstructorArgument<string>(attr, 0);
            }
        }

        return (when, unless);
    }

    private static List<string>? GetRuleSets(ISymbol symbol) {
        var list = new List<string>();

        foreach(var attr in symbol.GetAttributes()) {
            var name = attr.AttributeClass?.GetAttributeTypeName();
            if(!AttributeNames.RULE_SET.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var setName = MetadataHelper.GetConstructorArgument<string>(attr, 0);
            if(!string.IsNullOrEmpty(setName)) {
                list.Add(setName!);
            }
        }

        return list.Count > 0 ? list : null;
    }

    private static bool IsConditionalAttribute(string? attributeTypeName) {
        if(attributeTypeName is null) {
            return false;
        }

        return attributeTypeName.Equals(AttributeNames.REQUIRED_IF, StringComparison.OrdinalIgnoreCase) ||
               attributeTypeName.Equals(AttributeNames.REQUIRED_UNLESS, StringComparison.OrdinalIgnoreCase) ||
               attributeTypeName.Equals(AttributeNames.CONDITIONAL_RANGE, StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetConditionalAttributeName(AttributeData attribute) {
        var name = attribute.AttributeClass?.GetAttributeTypeName();
        return IsConditionalAttribute(name) ? name : null;
    }

    private static string GetConditionalDisplayName(string attributeTypeName) {
        var lastDot = attributeTypeName.LastIndexOf('.');
        var name = lastDot >= 0 ? attributeTypeName.Substring(lastDot + 1) : attributeTypeName;
        return name.EndsWith(ATTRIBUTE_SUFFIX, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - ATTRIBUTE_SUFFIX.Length)
            : name;
    }

    private static string? CombineConditions(
        string? first,
        bool firstHasContext,
        string? second,
        bool secondHasContext,
        out bool hasContext) {
        if(first is null) {
            hasContext = secondHasContext;
            return second;
        }

        if(second is null) {
            hasContext = firstHasContext;
            return first;
        }

        var firstBody = ExtractConditionBody(first, firstHasContext);
        var secondBody = ExtractConditionBody(second, secondHasContext);
        if(firstBody is null || secondBody is null) {
            hasContext = firstHasContext;
            return first;
        }

        hasContext = firstHasContext || secondHasContext;
        return (hasContext ? "(x, context) => " : "x => ") +
               $"({firstBody}) && ({secondBody})";
    }

    private static string? ExtractConditionBody(string expression, bool hasContext) {
        var prefix = hasContext ? "(x, context) => " : "x => ";
        return expression.StartsWith(prefix, StringComparison.Ordinal)
            ? expression.Substring(prefix.Length)
            : null;
    }

    private static (string? when, bool whenHasContext, string? unless, bool unlessHasContext) GetSyncConditions(
        ISymbol symbol,
        INamedTypeSymbol targetType,
        List<GeneratorDiagnostic> diagnostics) {
        var (whenExpression, whenHasContext) =
            GetConditionExpression(symbol, targetType, AttributeNames.WHEN_CONDITION, diagnostics, negate: false);
        var (unlessExpression, unlessHasContext) =
            GetConditionExpression(symbol, targetType, AttributeNames.UNLESS_CONDITION, diagnostics, negate: true);
        return (whenExpression, whenHasContext, unlessExpression, unlessHasContext);
    }

    private static (string? Expression, bool HasContext) GetConditionExpression(
        ISymbol symbol,
        INamedTypeSymbol targetType,
        string attributeName,
        List<GeneratorDiagnostic> diagnostics,
        bool negate) {
        var attribute = symbol.GetAttributes()
                              .LastOrDefault(candidate => string.Equals(
                                  candidate.AttributeClass?.GetAttributeTypeName(),
                                  attributeName,
                                  StringComparison.OrdinalIgnoreCase));

        if(attribute is null) {
            return (null, false);
        }

        var methodName = MetadataHelper.GetConstructorArgument<string>(attribute, 0);
        const string expected = "bool Method(T instance) or bool Method(T instance, ValidationContext<T> context)";

        if(string.IsNullOrWhiteSpace(methodName)) {
            ReportInvalidCondition(diagnostics, attribute, string.Empty, targetType, expected);
            return (null, false);
        }

        var method = FindMethod(targetType, methodName!);
        if(method is null || method.MethodKind is not MethodKind.Ordinary || !IsAccessible(method) ||
           method.ReturnType.SpecialType is not SpecialType.System_Boolean) {
            ReportInvalidCondition(diagnostics, attribute, methodName!, targetType, expected);
            return (null, false);
        }

        var parameters = method.Parameters;
        var receiver = method.IsStatic
            ? targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : "x";

        if(parameters.Length is 1 && IsTargetType(parameters[0].Type, targetType)) {
            return ($"x => {(negate ? "!" : string.Empty)}{receiver}.{methodName}(x)", false);
        }

        if(parameters.Length is 2 &&
           IsTargetType(parameters[0].Type, targetType) &&
           IsValidationContext(parameters[1].Type, targetType)) {
            return ($"(x, context) => {(negate ? "!" : string.Empty)}{receiver}.{methodName}(x, context)", true);
        }

        ReportInvalidCondition(diagnostics, attribute, methodName!, targetType, expected);
        return (null, false);
    }

    private static ClassRuleOptions BuildClassRuleOptions(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics) {
        var (when, unless) = GetAsyncConditions(typeSymbol);
        var (syncWhen, syncWhenHasContext, syncUnless, syncUnlessHasContext) =
            GetSyncConditions(typeSymbol, typeSymbol, diagnostics);

        return new ClassRuleOptions(
            GetRuleSets(typeSymbol),
            when,
            unless,
            syncWhen,
            syncWhenHasContext,
            syncUnless,
            syncUnlessHasContext);
    }

    private sealed record ClassRuleOptions(
        IReadOnlyList<string>? RuleSets,
        string? WhenAsync,
        string? UnlessAsync,
        string? When,
        bool WhenHasContext,
        string? Unless,
        bool UnlessHasContext);

    private static List<SanitizeStepModel> BuildSanitizers(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics) {
        var classSanitize = typeSymbol.GetAttributes().FirstOrDefault(IsSanitizeAttribute);
        var steps = new List<SanitizeStepModel>();

        foreach(var property in CollectPropertyEntries(typeSymbol).Select(static entry => entry.Property)) {
            var attribute = property.GetAttributes().FirstOrDefault(IsSanitizeAttribute) ?? classSanitize;

            if(attribute is null) {
                continue;
            }

            var step = TryCreateSanitizeStep(property, attribute, diagnostics);

            if(step is not null) {
                steps.Add(step);
            }
        }

        return steps;
    }

    private static bool IsSanitizeAttribute(AttributeData attribute) {
        var name = attribute.AttributeClass?.GetAttributeTypeName();

        return string.Equals(name, AttributeNames.SANITIZE, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, AttributeNames.SIMPLE_SANITIZE, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, AttributeNames.SIMPLE_SANITIZE + ATTRIBUTE_SUFFIX, StringComparison.OrdinalIgnoreCase);
    }

    private static SanitizeStepModel? TryCreateSanitizeStep(
        IPropertySymbol property,
        AttributeData attribute,
        List<GeneratorDiagnostic> diagnostics) {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                       ?? property.Locations.FirstOrDefault();
        var trim = MetadataHelper.GetNamedArgument<bool>(attribute, "Trim");
        var normalizeWhitespace = MetadataHelper.GetNamedArgument<bool>(attribute, "NormalizeWhitespace");
        var removeControlChars = MetadataHelper.GetNamedArgument<bool>(attribute, "RemoveControlChars");
        var toUpper = MetadataHelper.GetNamedArgument<bool>(attribute, "ToUpper");
        var toLower = MetadataHelper.GetNamedArgument<bool>(attribute, "ToLower");
        var culture = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "Culture"));

        if(!trim && !normalizeWhitespace && !removeControlChars && !toUpper && !toLower && culture is null) {
            return null;
        }

        var isString = property.Type.IsStringOrNullableString();
        var isStringList = !isString && IsStringList(property.Type);

        if(!isString && !isStringList) {
            ReportSanitizeNotApplicable(
                diagnostics,
                location,
                property.Name,
                $"it is of type '{property.Type.ToDisplayString()}', which sanitization does not support (string and IList<string> only)");
            return null;
        }

        var setter = property.SetMethod;

        if(setter is null) {
            ReportSanitizeNotApplicable(
                diagnostics,
                location,
                property.Name,
                "it has no setter. Sanitization writes the cleaned value back in place");
            return null;
        }

        if(setter.IsInitOnly) {
            ReportSanitizeNotApplicable(
                diagnostics,
                location,
                property.Name,
                "it has an init-only setter. Sanitization requires an assignable property");
            return null;
        }

        if(setter.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal)) {
            ReportSanitizeNotApplicable(
                diagnostics,
                location,
                property.Name,
                "its setter is not accessible. Sanitization requires a public or internal setter");
            return null;
        }

        if(culture is not null) {
            try {
                _ = CultureInfo.GetCultureInfo(culture);
            }
            catch(CultureNotFoundException) {
                ReportSanitizeNotApplicable(
                    diagnostics,
                    location,
                    property.Name,
                    $"culture '{culture}' is not a valid culture name");
                return null;
            }
            catch(ArgumentException) {
                ReportSanitizeNotApplicable(
                    diagnostics,
                    location,
                    property.Name,
                    $"culture '{culture}' is not a valid culture name");
                return null;
            }
        }

        return new SanitizeStepModel(
            property.Name,
            property.Type.GetFullTypeName(),
            isStringList,
            trim,
            normalizeWhitespace,
            removeControlChars,
            toUpper,
            toLower,
            culture);
    }

    private static bool IsStringList(ITypeSymbol type) {
        if(type is IArrayTypeSymbol array) {
            return array.ElementType.IsStringOrNullableString();
        }

        return IsStringListInterface(type) ||
               type.AllInterfaces.Any(static candidate => IsStringListInterface(candidate));
    }

    private static bool IsStringListInterface(ITypeSymbol type) =>
        type is INamedTypeSymbol named &&
        string.Equals(named.Name, "IList", StringComparison.Ordinal) &&
        string.Equals(
            named.ContainingNamespace?.ToDisplayString(),
            "System.Collections.Generic",
            StringComparison.Ordinal) &&
        named.TypeArguments is { Length: 1 } &&
        named.TypeArguments[0].IsStringOrNullableString();

    private static void ReportSanitizeNotApplicable(
        List<GeneratorDiagnostic> diagnostics,
        Location? location,
        string propertyName,
        string reason) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.SanitizeNotApplicable,
            location,
            [propertyName, reason]));

    private static IEnumerable<RuleModel> BuildModelRules(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics,
        ClassRuleOptions classOptions) {
        var targetTypeName = typeSymbol.GetFullTypeName();

        foreach(var attribute in typeSymbol.GetAttributes()) {
            var attributeName = attribute.AttributeClass?.GetAttributeTypeName();
            if(!string.Equals(attributeName, AttributeNames.CUSTOM_VALIDATOR, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var rule = TryCreateCustomRuleModel(
                string.Empty,
                attribute,
                typeSymbol,
                typeSymbol,
                diagnostics,
                isModelRule: true);

            if(rule is null) {
                continue;
            }

            var ruleSet = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "RuleSet"));

            yield return rule with {
                PropertyTypeName = targetTypeName,
                WhenAsyncCondition = classOptions.WhenAsync,
                UnlessAsyncCondition = classOptions.UnlessAsync,
                WhenCondition = classOptions.When,
                WhenConditionHasContext = classOptions.WhenHasContext,
                UnlessCondition = classOptions.Unless,
                UnlessConditionHasContext = classOptions.UnlessHasContext,
                RuleSets = ruleSet is not null ? [ruleSet] : classOptions.RuleSets,
                Severity = MetadataHelper.GetNamedSeverity(attribute),
                ErrorCode = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "ErrorCode")),
                DisplayName = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "DisplayName")),
                OverridePropertyName = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "OverridePropertyName")),
                CustomState = Normalize(MetadataHelper.GetNamedArgument<string>(attribute, "CustomState"))
            };
        }
    }

    private static RuleModel? TryCreateCustomRuleModel(
        string propertyName,
        AttributeData attribute,
        ITypeSymbol valueType,
        INamedTypeSymbol targetType,
        List<GeneratorDiagnostic> diagnostics,
        bool isModelRule) {
        var rulesType = GetCustomRulesType(attribute);
        var methodName = MetadataHelper.GetConstructorArgument<string>(attribute, 1);
        var isAsync = MetadataHelper.GetNamedArgument<bool>(attribute, "IsAsync");
        var message = MetadataHelper.GetNamedArgument<string>(attribute, "Message")
                      ?? MetadataHelper.GetConstructorArgument<string>(attribute, 2);
        var expected = GetCustomExpectedSignature(isAsync);

        if(rulesType is null || string.IsNullOrWhiteSpace(methodName)) {
            ReportInvalidCustomValidator(
                diagnostics,
                attribute,
                methodName ?? string.Empty,
                rulesType?.Name ?? targetType.Name,
                expected);
            return null;
        }

        var method = FindCustomMethod(
            rulesType,
            methodName!,
            valueType,
            isAsync,
            out var returnsResult,
            out var hasServiceProvider);

        if(method is null) {
            ReportInvalidCustomValidator(
                diagnostics,
                attribute,
                methodName!,
                rulesType.Name,
                expected);
            return null;
        }

        return new RuleModel(
            RuleKind.Custom,
            propertyName,
            valueType.GetFullTypeName(),
            Message: message,
            IsAsyncRule: isAsync,
            CustomMethodExpression: GetHookExpression(method),
            CustomReturnsResult: returnsResult,
            CustomHasServiceProvider: hasServiceProvider,
            IsModelRule: isModelRule);
    }

    private static INamedTypeSymbol? GetCustomRulesType(AttributeData attribute) =>
        attribute.ConstructorArguments.Length is 0
            ? null
            : attribute.ConstructorArguments[0].Value as INamedTypeSymbol;

    private static IMethodSymbol? FindCustomMethod(
        INamedTypeSymbol rulesType,
        string methodName,
        ITypeSymbol valueType,
        bool isAsync,
        out bool returnsResult,
        out bool hasServiceProvider) {
        returnsResult = false;
        hasServiceProvider = false;

        foreach(var method in rulesType.GetMembers(methodName).OfType<IMethodSymbol>()) {
            if(!IsCandidateCustomMethod(method)) {
                continue;
            }

            if(!TryGetCustomReturnKind(method.ReturnType, isAsync, out var result)) {
                continue;
            }

            if(!TryMatchCustomParameters(method, valueType, isAsync, out var provider)) {
                continue;
            }

            returnsResult = result;
            hasServiceProvider = provider;
            return method;
        }

        return null;
    }

    private static bool IsCandidateCustomMethod(IMethodSymbol method) =>
        method.IsStatic && !method.IsGenericMethod && IsAccessible(method);

    private static bool TryMatchCustomParameters(
        IMethodSymbol method,
        ITypeSymbol valueType,
        bool isAsync,
        out bool hasServiceProvider) {
        hasServiceProvider = false;
        var parameters = method.Parameters;

        if(parameters.Length is 0 ||
           !SymbolEqualityComparer.Default.Equals(parameters[0].Type, valueType)) {
            return false;
        }

        var index = 1;

        if(index < parameters.Length && IsServiceProvider(parameters[index].Type)) {
            hasServiceProvider = true;
            index++;
        }

        if(isAsync) {
            if(index >= parameters.Length || !IsCancellationToken(parameters[index].Type)) {
                return false;
            }

            index++;
        }

        return index == parameters.Length;
    }

    private static bool TryGetCustomReturnKind(
        ITypeSymbol returnType,
        bool isAsync,
        out bool returnsResult) {
        returnsResult = false;
        var resultType = returnType;

        if(isAsync) {
            if(returnType is not INamedTypeSymbol { Name: "ValueTask", TypeArguments.Length: 1 } taskType ||
               taskType.ContainingNamespace.ToDisplayString() is not "System.Threading.Tasks") {
                return false;
            }

            resultType = taskType.TypeArguments[0];
        }

        if(resultType.SpecialType is SpecialType.System_Boolean) {
            return true;
        }

        if(IsNamedType(resultType, "Jeninnet.Validation.ValidationResult")) {
            returnsResult = true;
            return true;
        }

        return false;
    }

    private static bool IsServiceProvider(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "IServiceProvider" } named &&
        named.ContainingNamespace.ToDisplayString() is "System";

    private static bool IsCancellationToken(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "CancellationToken" } named &&
        named.ContainingNamespace.ToDisplayString() is "System.Threading";

    private static string GetCustomExpectedSignature(bool isAsync) =>
        isAsync
            ? "a static method returning ValueTask<bool> or ValueTask<ValidationResult> with parameters (TValue, CancellationToken) or (TValue, IServiceProvider, CancellationToken)"
            : "a static method returning bool or ValidationResult with parameters (TValue) or (TValue, IServiceProvider)";

    private static void ReportInvalidCustomValidator(
        List<GeneratorDiagnostic> diagnostics,
        AttributeData attribute,
        string methodName,
        string typeName,
        string expected) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.InvalidCustomValidator,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            [methodName, typeName, expected]));

    private static (string? preValidate, bool preValidateHasErrors, string? onFailure) BuildLifecycleHooks(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics) {
        var (preValidate, preValidateHasErrors) = BuildPreValidateHook(typeSymbol, diagnostics);
        return (preValidate, preValidateHasErrors, BuildOnFailureHook(typeSymbol, diagnostics));
    }

    private static (string? Expression, bool HasErrors) BuildPreValidateHook(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics) {
        var preAttribute = GetLastAttribute(typeSymbol, AttributeNames.PRE_VALIDATE);
        if(preAttribute is null) {
            return (null, false);
        }

        var methodName = MetadataHelper.GetConstructorArgument<string>(preAttribute, 0);
        var method = string.IsNullOrWhiteSpace(methodName) ? null : FindMethod(typeSymbol, methodName!);
        const string expected = "a static bool Method(T instance, ValidationContext<T> context[, ValidationErrorCollector errors])";

        if(method?.IsStatic != true || !IsAccessible(method) ||
           method.ReturnType.SpecialType is not SpecialType.System_Boolean) {
            ReportInvalidHook(diagnostics, preAttribute, methodName ?? string.Empty, typeSymbol, expected);
            return (null, false);
        }

        if(method.Parameters.Length is 2 &&
           IsTargetType(method.Parameters[0].Type, typeSymbol) &&
           IsValidationContext(method.Parameters[1].Type, typeSymbol)) {
            return (GetHookExpression(method), false);
        }

        if(method.Parameters.Length is 3 &&
           IsTargetType(method.Parameters[0].Type, typeSymbol) &&
           IsValidationContext(method.Parameters[1].Type, typeSymbol) &&
           IsNamedType(method.Parameters[2].Type, "Jeninnet.Validation.ValidationErrorCollector")) {
            return (GetHookExpression(method), true);
        }

        ReportInvalidHook(diagnostics, preAttribute, methodName!, typeSymbol, expected);
        return (null, false);
    }

    private static string? BuildOnFailureHook(
        INamedTypeSymbol typeSymbol,
        List<GeneratorDiagnostic> diagnostics) {
        var onFailureAttribute = GetLastAttribute(typeSymbol, AttributeNames.ON_FAILURE);
        if(onFailureAttribute is null) {
            return null;
        }

        var methodName = MetadataHelper.GetConstructorArgument<string>(onFailureAttribute, 0);
        var method = string.IsNullOrWhiteSpace(methodName) ? null : FindMethod(typeSymbol, methodName!);
        const string expected = "a static void Method(ValidationResult result)";

        if(method?.IsStatic != true || !IsAccessible(method) ||
           !method.ReturnsVoid ||
           method.Parameters.Length is not 1 ||
           !IsNamedType(method.Parameters[0].Type, "Jeninnet.Validation.ValidationResult")) {
            ReportInvalidHook(diagnostics, onFailureAttribute, methodName ?? string.Empty, typeSymbol, expected);
            return null;
        }

        return GetHookExpression(method);
    }

    private static string GetHookExpression(IMethodSymbol method) =>
        $"{method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{method.Name}";

    private static AttributeData? GetLastAttribute(INamedTypeSymbol typeSymbol, string attributeName) =>
        typeSymbol.GetAttributes()
                  .LastOrDefault(attribute => string.Equals(
                      attribute.AttributeClass?.GetAttributeTypeName(),
                      attributeName,
                      StringComparison.OrdinalIgnoreCase));

    private static IMethodSymbol? FindMethod(INamedTypeSymbol typeSymbol, string methodName) {
        for(var current = typeSymbol; current is not null; current = current.BaseType) {
            var method = current.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
            if(method is not null) {
                return method;
            }
        }

        return null;
    }

    private static bool IsAccessible(IMethodSymbol method) =>
        method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

    private static bool IsTargetType(ITypeSymbol type, INamedTypeSymbol targetType) =>
        SymbolEqualityComparer.Default.Equals(type, targetType);

    private static bool IsValidationContext(ITypeSymbol type, INamedTypeSymbol targetType) =>
        type is INamedTypeSymbol named &&
        named.Name is "ValidationContext" &&
        named.ContainingNamespace.ToDisplayString() is "Jeninnet.Validation.Core" &&
        named.TypeArguments.Length is 1 &&
        IsTargetType(named.TypeArguments[0], targetType);

    private static bool IsNamedType(ITypeSymbol type, string fullName) =>
        string.Equals(type.ToDisplayString(), fullName, StringComparison.Ordinal);

    private static void ReportInvalidCondition(
        List<GeneratorDiagnostic> diagnostics,
        AttributeData attribute,
        string methodName,
        INamedTypeSymbol targetType,
        string expected) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.InvalidConditionMethod,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            [methodName, targetType.Name, expected]));

    private static void ReportInvalidHook(
        List<GeneratorDiagnostic> diagnostics,
        AttributeData attribute,
        string methodName,
        INamedTypeSymbol targetType,
        string expected) =>
        diagnostics.Add(new GeneratorDiagnostic(
            DiagnosticDescriptors.InvalidLifecycleHook,
            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
            [methodName, targetType.Name, expected]));

    private sealed class PropertyEntry(IPropertySymbol property) {
        public IPropertySymbol Property { get; set; } = property;

        public List<PropertyRuleSource> Sources { get; } = [new(property, property.GetAttributes())];
    }

    private sealed record PropertyRuleSource(IPropertySymbol Property, ImmutableArray<AttributeData> Attributes);

    private static ValidationRuleDescriptor Map(RuleModel model) {
        if(!Enum.IsDefined(typeof(RuleKind), model.Kind)) {
            throw new NotSupportedException($"Unsupported rule kind: {model.Kind}");
        }

        var options = new RuleOptions(
            model.Message,
            model.WhenAsyncCondition,
            model.UnlessAsyncCondition,
            model.RuleSets,
            model.IsAsyncRule,
            model.Severity,
            model.ErrorCode,
            model.DisplayName,
            model.OverridePropertyName,
            model.CustomState,
            model.WhenCondition,
            model.WhenConditionHasContext,
            model.UnlessCondition,
            model.UnlessConditionHasContext
        );

        if(model.Kind is RuleKind.Nested or RuleKind.ForEach or RuleKind.UniqueItems) {
            return new NestedRuleDescriptor(
                model.Kind,
                model.PropertyName,
                model.PropertyTypeName,
                options,
                model.TargetTypeName ?? model.PropertyTypeName
            );
        }

        if(model.Kind is RuleKind.Custom) {
            return new CustomRuleDescriptor(
                model.Kind,
                model.PropertyName,
                model.PropertyTypeName,
                options,
                model.CustomMethodExpression ?? string.Empty,
                model.CustomReturnsResult,
                model.CustomHasServiceProvider,
                model.IsModelRule
            );
        }

        var parameters = new List<RuleParameter>();
        if(model.Parameter1 is not null) {
            parameters.Add(new RuleParameter(model.Parameter1.Name, model.Parameter1.Type));
        }

        if(model.Parameter2 is not null) {
            parameters.Add(new RuleParameter(model.Parameter2.Name, model.Parameter2.Type));
        }

        if(model.Parameter3 is not null) {
            parameters.Add(new RuleParameter(model.Parameter3.Name, model.Parameter3.Type));
        }

        if(parameters.Count > 0) {
            return new ParametricRuleDescriptor(
                model.Kind,
                model.PropertyName,
                model.PropertyTypeName,
                options,
                parameters
            );
        }

        return new BasicRuleDescriptor(
            model.Kind,
            model.PropertyName,
            model.PropertyTypeName,
            options
        );
    }

    private static RuleModel? TryCreateRuleModelFromAttribute(
        string propertyName,
        AttributeData attribute,
        IPropertySymbol propertySymbol,
        INamedTypeSymbol targetType,
        List<GeneratorDiagnostic> diagnostics) {
        var attributeClass = attribute.AttributeClass;
        if(attributeClass is null || !IsValidationAttribute(attributeClass)) {
            return null;
        }

        var propertyTypeName = propertySymbol.Type.GetFullTypeName();
        var attributeTypeName = attributeClass.GetAttributeTypeName();

        if(IsNestedOrForEach(attributeTypeName) && IsSkippedType(propertySymbol.Type)) {
            return null;
        }

        if(IsConditionalAttribute(attributeTypeName)) {
            return ConditionalRuleBuilder.Create(
                propertyName,
                attribute,
                propertySymbol,
                targetType,
                diagnostics);
        }

        if(string.Equals(attributeTypeName, AttributeNames.CUSTOM_VALIDATOR, StringComparison.OrdinalIgnoreCase)) {
            return TryCreateCustomRuleModel(
                propertyName,
                attribute,
                propertySymbol.Type,
                targetType,
                diagnostics,
                isModelRule: false);
        }

        return TryHandleCollectionRules(propertyName, attribute, propertySymbol)
            ?? TryHandleCommonRules(propertyName, attribute, propertySymbol, propertyTypeName, attributeTypeName)
            ?? TryHandleComparisonRules(propertyName, attribute, propertyTypeName, attributeTypeName, diagnostics)
            ?? TryHandleDateTimeRules(propertyName, attribute, propertyTypeName, attributeTypeName, diagnostics)
            ?? TryHandleRelativeDateRules(propertyName, attribute, propertyTypeName, attributeTypeName, diagnostics)
            ?? TryHandlePropertyComparisonRules(propertyName, attribute, propertyTypeName, attributeTypeName)
            ?? TryHandleNestedRules(propertyName, attribute, propertyTypeName, attributeTypeName)
            ?? TryHandleDomainRules(propertyName, attribute, propertyTypeName, attributeTypeName)
            ?? TryHandleStringRules(propertyName, attribute, propertyTypeName, attributeTypeName);
    }

    private static bool IsNestedOrForEach(string attributeTypeName) =>
        attributeTypeName.Equals(AttributeNames.NESTED, StringComparison.OrdinalIgnoreCase) ||
        attributeTypeName.Equals("Nested", StringComparison.OrdinalIgnoreCase) ||
        attributeTypeName.Equals("NestedAttribute", StringComparison.OrdinalIgnoreCase) ||
        attributeTypeName.Equals(AttributeNames.FOR_EACH, StringComparison.OrdinalIgnoreCase) ||
        attributeTypeName.Equals("ForEach", StringComparison.OrdinalIgnoreCase) ||
        attributeTypeName.Equals("ForEachAttribute", StringComparison.OrdinalIgnoreCase);

    private static bool HasSkipValidation(ISymbol symbol) =>
        symbol.GetAttributes().Any(static attribute =>
            string.Equals(
                attribute.AttributeClass?.GetAttributeTypeName(),
                AttributeNames.SKIP_VALIDATION,
                StringComparison.OrdinalIgnoreCase));

    private static bool IsSkippedType(ITypeSymbol type) {
        if(HasSkipValidation(type)) {
            return true;
        }

        if(type is IArrayTypeSymbol array) {
            return IsSkippedType(array.ElementType);
        }

        return type is INamedTypeSymbol { IsGenericType: true } named &&
           named.TypeArguments.Any(static argument => IsSkippedType(argument));
    }

    private static bool IsValidationAttribute(INamedTypeSymbol attributeType) {        // Name-based check for backward compatibility and tests
        if(IsValidationAttributeName(attributeType.Name)) {
            return true;
        }

        // Hierarchy-based check
        var baseType = attributeType.BaseType;
        while(baseType is not null) {
            if(baseType.Name == "ValidationAttribute" && baseType.ContainingNamespace?.ToDisplayString() == "Jeninnet.Validation.Attributes") {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsValidationAttributeName(string attributeName) =>
        Enum.GetValues(typeof(RuleKind))
            .OfType<RuleKind>()
            .Any(x => x.GetAttributeName(removeSuffix: false)
            .Contains(attributeName, StringComparison.OrdinalIgnoreCase)) ||
        attributeName.Equals(nameof(RuleKind.MaximumLength), StringComparison.OrdinalIgnoreCase) ||
        attributeName.Equals(nameof(RuleKind.MinimumLength), StringComparison.OrdinalIgnoreCase);

    private static RuleModel? TryHandleCollectionRules(string propertyName, AttributeData attribute, IPropertySymbol propertySymbol) {
        var name = attribute.AttributeClass?.GetAttributeTypeName();
        if(name is null) {
            return null;
        }

        return name switch {
            _ when name.Equals(AttributeNames.FOR_EACH, StringComparison.OrdinalIgnoreCase) || name.Equals("ForEach", StringComparison.OrdinalIgnoreCase) || name.Equals("ForEachAttribute", StringComparison.OrdinalIgnoreCase)
                => CollectionRuleBuilder.CreateForEachRule(propertyName, propertySymbol),
            _ when name.Equals(AttributeNames.COUNT, StringComparison.OrdinalIgnoreCase) || name.Equals("Count", StringComparison.OrdinalIgnoreCase) || name.Equals("CountAttribute", StringComparison.OrdinalIgnoreCase)
                => CollectionRuleBuilder.CreateCountRule(propertyName, attribute, propertySymbol.Type.GetFullTypeName()),
            _ when name.Equals(AttributeNames.MIN_COUNT, StringComparison.OrdinalIgnoreCase) || name.Equals("MinCount", StringComparison.OrdinalIgnoreCase) || name.Equals("MinCountAttribute", StringComparison.OrdinalIgnoreCase)
                => CollectionRuleBuilder.CreateMinCountRule(propertyName, attribute, propertySymbol.Type.GetFullTypeName()),
            _ when name.Equals(AttributeNames.MAX_COUNT, StringComparison.OrdinalIgnoreCase) || name.Equals("MaxCount", StringComparison.OrdinalIgnoreCase) || name.Equals("MaxCountAttribute", StringComparison.OrdinalIgnoreCase)
                => CollectionRuleBuilder.CreateMaxCountRule(propertyName, attribute, propertySymbol.Type.GetFullTypeName()),
            _ when name.Equals(AttributeNames.UNIQUE_ITEMS, StringComparison.OrdinalIgnoreCase) || name.Equals("UniqueItems", StringComparison.OrdinalIgnoreCase) || name.Equals("UniqueItemsAttribute", StringComparison.OrdinalIgnoreCase)
                => CollectionRuleBuilder.CreateUniqueItemsRule(propertyName, attribute, propertySymbol),
            _ => null
        };
    }

    private static RuleModel? TryHandleCommonRules(string propertyName, AttributeData attribute, IPropertySymbol propertySymbol, string propertyTypeName, string attributeTypeName) {
        var name = attributeTypeName;
        return name switch {
            _ when name.Equals(AttributeNames.NOT_EMPTY, StringComparison.OrdinalIgnoreCase) || name.Equals("NotEmpty", StringComparison.OrdinalIgnoreCase) || name.Equals("NotEmptyAttribute", StringComparison.OrdinalIgnoreCase)
                => CommonRuleBuilder.CreateCommonNotXRule(propertyName, propertySymbol.Type.IsStringOrNullableString() ? RuleKind.StringNotEmpty : RuleKind.NotEmpty, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.NOT_NULL, StringComparison.OrdinalIgnoreCase) || name.Equals("NotNull", StringComparison.OrdinalIgnoreCase) || name.Equals("NotNullAttribute", StringComparison.OrdinalIgnoreCase)
                => CommonRuleBuilder.CreateCommonNotXRule(propertyName, RuleKind.NotNull, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.ENUM, StringComparison.OrdinalIgnoreCase) || name.Equals("Enum", StringComparison.OrdinalIgnoreCase) || name.Equals("EnumAttribute", StringComparison.OrdinalIgnoreCase)
                => CommonRuleBuilder.CreateCommonNotXRule(propertyName, RuleKind.Enum, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.IN, StringComparison.OrdinalIgnoreCase) || name.Equals("In", StringComparison.OrdinalIgnoreCase) || name.Equals("InAttribute", StringComparison.OrdinalIgnoreCase)
                => CommonRuleBuilder.CreateInRule(propertyName, RuleKind.In, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.NOT_IN, StringComparison.OrdinalIgnoreCase) || name.Equals("NotIn", StringComparison.OrdinalIgnoreCase) || name.Equals("NotInAttribute", StringComparison.OrdinalIgnoreCase)
                => CommonRuleBuilder.CreateInRule(propertyName, RuleKind.NotIn, attribute, propertyTypeName),
            _ => null
        };
    }

    private static RuleModel? TryHandleComparisonRules(string propertyName, AttributeData attribute, string propertyTypeName, string name, List<GeneratorDiagnostic> diagnostics) =>
        name switch {
            _ when name.Equals(AttributeNames.EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("Equal", StringComparison.OrdinalIgnoreCase) || name.Equals("EqualAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.Equal, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.EXCLUSIVE_BETWEEN, StringComparison.OrdinalIgnoreCase) || name.Equals("ExclusiveBetween", StringComparison.OrdinalIgnoreCase) || name.Equals("ExclusiveBetweenAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonBetweenRule(propertyName, RuleKind.ExclusiveBetween, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.GREATER_THAN, StringComparison.OrdinalIgnoreCase) || name.Equals("GreaterThan", StringComparison.OrdinalIgnoreCase) || name.Equals("GreaterThanAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.GreaterThan, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.GREATER_THAN_OR_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("GreaterThanOrEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("GreaterThanOrEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.GreaterThanOrEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.INCLUSIVE_BETWEEN, StringComparison.OrdinalIgnoreCase) || name.Equals("InclusiveBetween", StringComparison.OrdinalIgnoreCase) || name.Equals("InclusiveBetweenAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonBetweenRule(propertyName, RuleKind.InclusiveBetween, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.LESS_THAN, StringComparison.OrdinalIgnoreCase) || name.Equals("LessThan", StringComparison.OrdinalIgnoreCase) || name.Equals("LessThanAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.LessThan, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.LESS_THAN_OR_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("LessThanOrEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("LessThanOrEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.LessThanOrEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.NOT_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("NotEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("NotEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateComparisonRule(propertyName, RuleKind.NotEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.SCALE_PRECISION, StringComparison.OrdinalIgnoreCase) || name.Equals("ScalePrecision", StringComparison.OrdinalIgnoreCase) || name.Equals("ScalePrecisionAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateScalePrecisionRule(propertyName, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.MULTIPLE_OF, StringComparison.OrdinalIgnoreCase) || name.Equals("MultipleOf", StringComparison.OrdinalIgnoreCase) || name.Equals("MultipleOfAttribute", StringComparison.OrdinalIgnoreCase)
                => ComparisonRuleBuilder.CreateMultipleOfRule(propertyName, attribute, propertyTypeName, diagnostics),
            _ => null
        };

    private static RuleModel? TryHandleRelativeDateRules(
        string propertyName,
        AttributeData attribute,
        string propertyTypeName,
        string name,
        List<GeneratorDiagnostic> diagnostics) {
        RuleKind kind;
        string displayName;

        if(name.Equals(AttributeNames.FUTURE_DATE, StringComparison.OrdinalIgnoreCase) ||
           name.Equals("FutureDate", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("FutureDateAttribute", StringComparison.OrdinalIgnoreCase)) {
            kind = RuleKind.FutureDate;
            displayName = "FutureDate";
        } else if(name.Equals(AttributeNames.PAST_DATE, StringComparison.OrdinalIgnoreCase) ||
                  name.Equals("PastDate", StringComparison.OrdinalIgnoreCase) ||
                  name.Equals("PastDateAttribute", StringComparison.OrdinalIgnoreCase)) {
            kind = RuleKind.PastDate;
            displayName = "PastDate";
        } else {
            return null;
        }

        var simpleTypeName = GetSimpleTypeName(propertyTypeName);
        if(simpleTypeName is not ("DateTime" or "DateTimeOffset" or "DateOnly")) {
            diagnostics.Add(new GeneratorDiagnostic(
                DiagnosticDescriptors.RelativeDateUnsupportedType,
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                [displayName, propertyName, propertyTypeName]));
            return null;
        }

        var includeToday = MetadataHelper.GetNamedArgument<bool>(attribute, "IncludeToday");

        return new RuleModel(
            kind,
            propertyName,
            propertyTypeName,
            Parameter1: new ParameterInfo(includeToday ? "true" : "false", typeof(bool).FullName),
            Message: MetadataHelper.GetMessageArgument(attribute));
    }

    private static string GetSimpleTypeName(string typeName) {
        var normalized = typeName.Replace("global::", string.Empty).TrimEnd('?');
        var lastDot = normalized.LastIndexOf('.');
        return lastDot >= 0 ? normalized.Substring(lastDot + 1) : normalized;
    }

    private static RuleModel? TryHandleDateTimeRules(
        string propertyName,
        AttributeData attribute,
        string propertyTypeName,
        string name,
        List<GeneratorDiagnostic> diagnostics) =>
        name switch {
            _ when name.Equals(AttributeNames.AFTER, StringComparison.OrdinalIgnoreCase) || name.Equals("After", StringComparison.OrdinalIgnoreCase) || name.Equals("AfterAttribute", StringComparison.OrdinalIgnoreCase)
                => DateTimeRuleBuilder.CreateDateTimeRule(propertyName, RuleKind.After, attribute, propertyTypeName, diagnostics),
            _ when name.Equals(AttributeNames.BEFORE, StringComparison.OrdinalIgnoreCase) || name.Equals("Before", StringComparison.OrdinalIgnoreCase) || name.Equals("BeforeAttribute", StringComparison.OrdinalIgnoreCase)
                => DateTimeRuleBuilder.CreateDateTimeRule(propertyName, RuleKind.Before, attribute, propertyTypeName, diagnostics),
            _ => null
        };

    private static RuleModel? TryHandlePropertyComparisonRules(string propertyName, AttributeData attribute, string propertyTypeName, string name) =>
        name switch {
            _ when name.Equals(AttributeNames.PROPERTY_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.PROPERTY_GREATER_THAN, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyGreaterThan", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyGreaterThanAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyGreaterThan, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.PROPERTY_GREATER_THAN_OR_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyGreaterThanOrEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyGreaterThanOrEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyGreaterThanOrEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.PROPERTY_LESS_THAN, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyLessThan", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyLessThanAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyLessThan, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.PROPERTY_LESS_THAN_OR_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyLessThanOrEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyLessThanOrEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyLessThanOrEqual, attribute, propertyTypeName),
            _ when name.Equals(AttributeNames.PROPERTY_NOT_EQUAL, StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyNotEqual", StringComparison.OrdinalIgnoreCase) || name.Equals("PropertyNotEqualAttribute", StringComparison.OrdinalIgnoreCase)
                => PropertyRuleBuilder.CreatePropertyRule(propertyName, RuleKind.PropertyNotEqual, attribute, propertyTypeName),
            _ => null
        };

    private static RuleModel? TryHandleNestedRules(string propertyName, AttributeData attribute, string propertyTypeName, string name) =>
        name switch {
            _ when name.Equals(AttributeNames.NESTED, StringComparison.OrdinalIgnoreCase) || name.Equals("Nested", StringComparison.OrdinalIgnoreCase) || name.Equals("NestedAttribute", StringComparison.OrdinalIgnoreCase)
                => NestedRuleBuilder.CreateNestedRule(propertyName, attribute, propertyTypeName),
            _ => null
        };

    private static RuleModel? TryHandleDomainRules(
        string propertyName,
        AttributeData attribute,
        string propertyTypeName,
        string name) =>
        name switch {
            _ when name.Equals(AttributeNames.IBAN, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Iban", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("IbanAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateCountryRule(propertyName, RuleKind.Iban, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.VAT_NUMBER, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("VatNumber", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("VatNumberAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateCountryRule(propertyName, RuleKind.VatNumber, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.POSTAL_CODE, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PostalCode", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PostalCodeAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateCountryRule(propertyName, RuleKind.PostalCode, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.NATIONAL_ID, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("NationalId", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("NationalIdAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateCountryRule(propertyName, RuleKind.NationalId, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.PASSWORD_STRENGTH, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PasswordStrength", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("PasswordStrengthAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreatePasswordStrengthRule(propertyName, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.UUID, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Uuid", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("UuidAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateSimpleRule(propertyName, RuleKind.Uuid, attribute, propertyTypeName),

            _ when name.Equals(AttributeNames.ULID, StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Ulid", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("UlidAttribute", StringComparison.OrdinalIgnoreCase)
                => DomainRuleBuilder.CreateSimpleRule(propertyName, RuleKind.Ulid, attribute, propertyTypeName),

            _ => null
        };

    private static RuleModel? TryHandleStringRules(string propertyName, AttributeData attribute, string propertyTypeName, string name) =>
        TryHandleStringContentRule(propertyName, attribute, propertyTypeName, name) ??
        TryHandleStringLengthRule(propertyName, attribute, propertyTypeName, name) ??
        TryHandleStringFormatRule(propertyName, attribute, propertyTypeName, name);

    private static RuleModel? TryHandleStringContentRule(string propertyName, AttributeData attribute, string propertyTypeName, string name) => name switch {
        _ when name.Equals(AttributeNames.CREDIT_CARD, StringComparison.OrdinalIgnoreCase) || name.Equals("CreditCard", StringComparison.OrdinalIgnoreCase) || name.Equals("CreditCardAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateCreditCardRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.EMAIL, StringComparison.OrdinalIgnoreCase) || name.Equals("Email", StringComparison.OrdinalIgnoreCase) || name.Equals("EmailAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateEmailRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.STARTS_WITH, StringComparison.OrdinalIgnoreCase) || name.Equals("StartsWith", StringComparison.OrdinalIgnoreCase) || name.Equals("StartsWithAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringParamRule(propertyName, RuleKind.StartsWith, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.ENDS_WITH, StringComparison.OrdinalIgnoreCase) || name.Equals("EndsWith", StringComparison.OrdinalIgnoreCase) || name.Equals("EndsWithAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringParamRule(propertyName, RuleKind.EndsWith, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.CONTAINS, StringComparison.OrdinalIgnoreCase) || name.Equals("Contains", StringComparison.OrdinalIgnoreCase) || name.Equals("ContainsAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringParamRule(propertyName, RuleKind.Contains, attribute, propertyTypeName),
        _ => null
    };

    private static RuleModel? TryHandleStringLengthRule(string propertyName, AttributeData attribute, string propertyTypeName, string name) => name switch {
        _ when name.Equals(AttributeNames.LENGTH, StringComparison.OrdinalIgnoreCase) || name.Equals("Length", StringComparison.OrdinalIgnoreCase) || name.Equals("LengthAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateLengthRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.MAXIMUM_LENGTH, StringComparison.OrdinalIgnoreCase) || name.Equals("MaximumLength", StringComparison.OrdinalIgnoreCase) || name.Equals("MaximumLengthAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateMaxLengthRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.MINIMUM_LENGTH, StringComparison.OrdinalIgnoreCase) || name.Equals("MinimumLength", StringComparison.OrdinalIgnoreCase) || name.Equals("MinimumLengthAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateMinLengthRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.EXACT_LENGTH, StringComparison.OrdinalIgnoreCase) || name.Equals("ExactLength", StringComparison.OrdinalIgnoreCase) || name.Equals("ExactLengthAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateExactLengthRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.BASE64_STRING, StringComparison.OrdinalIgnoreCase) || name.Equals("Base64String", StringComparison.OrdinalIgnoreCase) || name.Equals("Base64StringAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateBase64StringRule(propertyName, attribute, propertyTypeName),
        _ => null
    };

    private static RuleModel? TryHandleStringFormatRule(string propertyName, AttributeData attribute, string propertyTypeName, string name) => name switch {
        _ when name.Equals(AttributeNames.FILE_EXTENSIONS, StringComparison.OrdinalIgnoreCase) || name.Equals("FileExtensions", StringComparison.OrdinalIgnoreCase) || name.Equals("FileExtensionsAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateFileExtensionsRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.REGEX, StringComparison.OrdinalIgnoreCase) || name.Equals("Regex", StringComparison.OrdinalIgnoreCase) || name.Equals("RegexAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateRegexRule(propertyName, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.IP_ADDRESS, StringComparison.OrdinalIgnoreCase) || name.Equals("IPAddress", StringComparison.OrdinalIgnoreCase) || name.Equals("IPAddressAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringOnlyRule(propertyName, RuleKind.IPAddress, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.PHONE, StringComparison.OrdinalIgnoreCase) || name.Equals("Phone", StringComparison.OrdinalIgnoreCase) || name.Equals("PhoneAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringOnlyRule(propertyName, RuleKind.Phone, attribute, propertyTypeName),
        _ when name.Equals(AttributeNames.URL, StringComparison.OrdinalIgnoreCase) || name.Equals("Url", StringComparison.OrdinalIgnoreCase) || name.Equals("UrlAttribute", StringComparison.OrdinalIgnoreCase)
            => StringRuleBuilder.CreateStringOnlyRule(propertyName, RuleKind.Url, attribute, propertyTypeName),
        _ => null
    };
}
