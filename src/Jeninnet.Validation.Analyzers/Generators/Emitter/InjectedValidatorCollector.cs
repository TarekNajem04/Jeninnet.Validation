//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Generators.Emitter;

/// <summary>
/// Represents a validator that must be injected through the constructor.
/// </summary>
/// <param name="FieldName">The name of the field.</param>
/// <param name="InterfaceType">The interface type of the validator.</param>
/// <param name="SimpleTargetType">The simple type of the validator.</param>
internal sealed record InjectedValidator(
    string FieldName,
    string InterfaceType,       // e.g. IValidator<Address>
    string SimpleTargetType);   // e.g. Address

/// <summary>
/// Scans validator descriptors to identify nested validators that require DI.
/// </summary>
internal static class InjectedValidatorCollector {
    private const string GLOBAL_PREFIX = "global::";

    /// <summary>
    /// Scans the validator descriptor to identify nested validators that require DI.
    /// </summary>
    /// <param name="descriptor">The validator descriptor to scan.</param>
    public static List<InjectedValidator> Collect(ValidatorDescriptor descriptor) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<InjectedValidator>();

        foreach(var prop in descriptor.Properties) {
            foreach(var rule in prop.Rules) {
                if(rule is not NestedRuleDescriptor nested ||
                   nested.Kind is not (RuleKind.Nested or RuleKind.ForEach)) {
                    continue;
                }

                // TargetTypeName is the simple (namespace-stripped) type for the
                // nested or element type.
                var rawTarget = nested.TargetTypeName;
                var target = rawTarget
                    .Replace(GLOBAL_PREFIX, "")
                    .Replace("?", "");

                // Strip the "Validator" suffix that MetadataBuilder might have appended.
                if(target.EndsWith(ValidatorEmitter.CLASS_SUFFIX, StringComparison.Ordinal)) {
                    target = target.Substring(0, target.Length - ValidatorEmitter.CLASS_SUFFIX.Length);
                }

                var simpleTarget = NamingUtilities.GetSimpleName(target);

                if(!seen.Add(simpleTarget)) {
                    continue; // already registered
                }

                var fieldName = $"_{char.ToLowerInvariant(simpleTarget[0])}{simpleTarget.Substring(1)}Validator";
                var interfaceType = $"IValidator<{simpleTarget}>";

                result.Add(new InjectedValidator(fieldName, interfaceType, simpleTarget));
            }
        }

        return result;
    }
}
