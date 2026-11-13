//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.JsonSchema;

/// <summary>
/// Maps validator descriptors to schema constraints.
/// </summary>
public static class ValidationSchemaMapper {
    private const string STRING_TYPE = "string";
    private const string NUMBER_TYPE = "number";
    private const string ARRAY_TYPE = "array";
    private const string OBJECT_TYPE = "object";
    private const string COUNTRY_PARAMETER = "Country";
    private const string THRESHOLD_PARAMETER = "Threshold";

    private static readonly Dictionary<string, Action<MemberMappingState, IValidationRuleDescriptor>> _handlers =
        new(StringComparer.Ordinal) {
            ["NotNull"] = static (state, _) => state.Constraints.IsRequired = true,
            ["StringNotEmpty"] = static (state, _) => {
                SetType(state.Constraints, STRING_TYPE);
                state.Constraints.IsRequired = true;
                state.NotEmpty = true;
            },
            ["NotEmpty"] = static (state, _) => {
                state.Constraints.IsRequired = true;
                state.NotEmpty = true;
            },
            ["Length"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, STRING_TYPE);
                constraints.MinLength = TryInt(GetValue(rule, "Min", "0")) ?? constraints.MinLength;
                constraints.MaxLength = TryInt(GetValue(rule, "Max", "1")) ?? constraints.MaxLength;
            },
            ["ExactLength"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, STRING_TYPE);
                var length = TryInt(GetValue(rule, "Length", "0"));

                if(length is not null) {
                    constraints.MinLength = length;
                    constraints.MaxLength = length;
                }
            },
            ["Regex"] = static (state, rule) =>
                SetPattern(state.Constraints, GetValue(rule, "Pattern", "0")),
            ["Contains"] = static (state, rule) =>
                SetPattern(state.Constraints, Regex.Escape(GetValue(rule, "Value", "0") ?? string.Empty)),
            ["StartsWith"] = static (state, rule) =>
                SetPattern(state.Constraints, "^" + Regex.Escape(GetValue(rule, "Value", "0") ?? string.Empty)),
            ["EndsWith"] = static (state, rule) =>
                SetPattern(state.Constraints, Regex.Escape(GetValue(rule, "Value", "0") ?? string.Empty) + "$"),
            ["Email"] = static (state, _) => SetFormat(state.Constraints, "email"),
            ["Url"] = static (state, _) => SetFormat(state.Constraints, "uri"),
            ["IPAddress"] = static (state, _) => {
                SetType(state.Constraints, STRING_TYPE);
                state.Constraints.AnyOfFormats = ["ipv4", "ipv6"];
            },
            ["Base64String"] = static (state, _) => {
                SetType(state.Constraints, STRING_TYPE);
                state.Constraints.ContentEncoding = "base64";
            },
            ["FileExtensions"] = static (state, rule) => {
                SetType(state.Constraints, STRING_TYPE);
                state.Constraints.FileExtensions = SplitValues(GetValue(rule, "Extensions", "0"));
            },
            ["Iban"] = static (state, rule) =>
                SetDomainPattern(state.Constraints, rule, DomainSchemaPatterns.Iban),
            ["VatNumber"] = static (state, rule) =>
                SetDomainPattern(state.Constraints, rule, DomainSchemaPatterns.VatNumber),
            ["PostalCode"] = static (state, rule) =>
                SetDomainPattern(state.Constraints, rule, DomainSchemaPatterns.PostalCode),
            ["NationalId"] = static (state, rule) =>
                SetDomainPattern(state.Constraints, rule, DomainSchemaPatterns.NationalId),
            ["PasswordStrength"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, STRING_TYPE);
                constraints.MinLength = TryInt(GetValue(rule, "MinLength")) ?? constraints.MinLength;
            },
            ["Uuid"] = static (state, _) => SetFormat(state.Constraints, "uuid"),
            ["Ulid"] = static (state, _) =>
                SetPattern(state.Constraints, "^[0-7][0-9A-HJKMNP-TV-Z]{25}$"),
            ["Nested"] = static (state, rule) => {
                SetType(state.Constraints, OBJECT_TYPE);
                state.Constraints.NestedDescriptor = GetNestedDescriptor(rule);

                if(state.Constraints.NestedDescriptor is null) {
                    state.AdvisoryRules.Add(rule);
                }
            },
            ["ForEach"] = static (state, rule) => {
                SetType(state.Constraints, ARRAY_TYPE);
                state.Constraints.ItemDescriptor = GetNestedDescriptor(rule);

                if(state.Constraints.ItemDescriptor is null) {
                    state.AdvisoryRules.Add(rule);
                }
            },
            ["Enum"] = static (state, rule) => {
                state.Constraints.EnumType = GetValue(rule, "EnumType", "0");

                if(state.Constraints.EnumType is null) {
                    state.AdvisoryRules.Add(rule);
                }
            },
            ["Count"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, ARRAY_TYPE);
                constraints.MinItems = TryInt(GetValue(rule, "Min", "0")) ?? constraints.MinItems;
                constraints.MaxItems = TryInt(GetValue(rule, "Max", "1")) ?? constraints.MaxItems;
            },
            ["MinCount"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, ARRAY_TYPE);
                constraints.MinItems = TryInt(GetValue(rule, "Min", "0")) ?? constraints.MinItems;
            },
            ["MaxCount"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, ARRAY_TYPE);
                constraints.MaxItems = TryInt(GetValue(rule, "Max", "0")) ?? constraints.MaxItems;
            },
            ["UniqueItems"] = static (state, _) => {
                SetType(state.Constraints, ARRAY_TYPE);
                state.Constraints.UniqueItems = true;
            },
            ["MultipleOf"] = static (state, rule) => {
                SetType(state.Constraints, NUMBER_TYPE);
                state.Constraints.MultipleOf = TryDecimal(GetValue(rule, "Divisor", "0"));
            },
            ["GreaterThan"] = static (state, rule) => {
                SetType(state.Constraints, NUMBER_TYPE);
                state.Constraints.ExclusiveMinimum = TryDecimal(GetValue(rule, THRESHOLD_PARAMETER, "0"));
            },
            ["GreaterThanOrEqual"] = static (state, rule) => {
                SetType(state.Constraints, NUMBER_TYPE);
                state.Constraints.Minimum = TryDecimal(GetValue(rule, THRESHOLD_PARAMETER, "0"));
            },
            ["LessThan"] = static (state, rule) => {
                SetType(state.Constraints, NUMBER_TYPE);
                state.Constraints.ExclusiveMaximum = TryDecimal(GetValue(rule, THRESHOLD_PARAMETER, "0"));
            },
            ["LessThanOrEqual"] = static (state, rule) => {
                SetType(state.Constraints, NUMBER_TYPE);
                state.Constraints.Maximum = TryDecimal(GetValue(rule, THRESHOLD_PARAMETER, "0"));
            },
            ["InclusiveBetween"] = HandleInclusiveBetween,
            ["Range"] = HandleInclusiveBetween,
            ["Between"] = HandleInclusiveBetween,
            ["ExclusiveBetween"] = static (state, rule) => {
                var constraints = state.Constraints;
                SetType(constraints, NUMBER_TYPE);
                constraints.ExclusiveMinimum = TryDecimal(GetValue(rule, "Min", "0"));
                constraints.ExclusiveMaximum = TryDecimal(GetValue(rule, "Max", "1"));
            },
            ["In"] = static (state, rule) => {
                var values = SplitValues(GetValue(rule, "Values", "0"));
                state.Constraints.Enum = values;
                SetType(state.Constraints, values.Count > 0 && values.All(IsDecimal) ? NUMBER_TYPE : STRING_TYPE);
            },
            ["ScalePrecision"] = HandleScalePrecision,
            ["PrecisionScale"] = HandleScalePrecision,
            ["After"] = static (state, _) => SetFormat(state.Constraints, "date-time"),
            ["Before"] = static (state, _) => SetFormat(state.Constraints, "date-time")
        };

    /// <summary>
    /// Maps a validator descriptor to an object schema.
    /// </summary>
    /// <param name="descriptor">The validator descriptor.</param>
    /// <returns>The mapped object schema.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is null.</exception>
    public static ValidationObjectSchema Map(IValidationDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);

        var properties = new Dictionary<string, ValidationSchemaConstraints>(StringComparer.Ordinal);

        foreach(var member in descriptor.Members.OrderBy(static member => member.Name, StringComparer.Ordinal)) {
            properties[member.Name] = MapMember(member);
        }

        return new ValidationObjectSchema(descriptor.ValidatorName, properties);
    }

    /// <summary>
    /// Maps one member's rules to schema constraints.
    /// </summary>
    /// <param name="member">The member descriptor.</param>
    /// <returns>The mapped constraints.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member"/> is null.</exception>
    public static ValidationSchemaConstraints MapMember(IValidationMemberDescriptor member) {
        ArgumentNullException.ThrowIfNull(member);

        var state = new MemberMappingState();

        if(member is ValidationMemberDescriptor typedMember) {
            state.Constraints.MemberType = typedMember.MemberType;
        }

        foreach(var rule in member.Rules) {
            // Conditions are delegates, not descriptors: applying their inner rule as an
            // unconditional constraint would be wrong. Keep the rule as an explicit advisory.
            if(rule.HasCondition) {
                state.AdvisoryRules.Add(rule);
                continue;
            }

            if(_handlers.TryGetValue(rule.Kind, out var handler)) {
                handler(state, rule);
            } else {
                state.AdvisoryRules.Add(rule);
            }
        }

        ApplyNotEmpty(state);
        NormalizeArrayConstraints(state.Constraints);

        state.Constraints.AdvisoryRules = state.AdvisoryRules;
        return state.Constraints;
    }

    private static void HandleInclusiveBetween(MemberMappingState state, IValidationRuleDescriptor rule) {
        var constraints = state.Constraints;
        SetType(constraints, NUMBER_TYPE);
        constraints.Minimum = TryDecimal(GetValue(rule, "Min", "0"));
        constraints.Maximum = TryDecimal(GetValue(rule, "Max", "1"));
    }

    private static void HandleScalePrecision(MemberMappingState state, IValidationRuleDescriptor rule) {
        var constraints = state.Constraints;
        SetType(constraints, NUMBER_TYPE);
        var scale = TryInt(GetValue(rule, "Scale", "1"));

        if(scale is not null) {
            constraints.MultipleOf = 1m / Pow10(scale.Value);
        }
    }

    private static decimal Pow10(int exponent) {
        var result = 1m;

        for(var i = 0; i < exponent; i++) {
            result *= 10m;
        }

        return result;
    }

    private static void ApplyNotEmpty(MemberMappingState state) {
        if(!state.NotEmpty) {
            return;
        }

        var constraints = state.Constraints;

        if(constraints.Type is null) {
            if(constraints.MemberType == typeof(string)) {
                SetType(constraints, STRING_TYPE);
            } else if(constraints.MemberType is { } memberType &&
                      IsCollectionType(memberType)) {
                SetType(constraints, ARRAY_TYPE);
            }
        }

        if(string.Equals(constraints.Type, STRING_TYPE, StringComparison.Ordinal) && constraints.MinLength is null) {
            constraints.MinLength = 1;
        } else if(string.Equals(constraints.Type, ARRAY_TYPE, StringComparison.Ordinal) && constraints.MinItems is null) {
            constraints.MinItems = 1;
        }
    }

    private static void NormalizeArrayConstraints(ValidationSchemaConstraints constraints) {
        if(!string.Equals(constraints.Type, ARRAY_TYPE, StringComparison.Ordinal)) {
            return;
        }

        constraints.MinItems ??= constraints.MinLength;
        constraints.MaxItems ??= constraints.MaxLength;
        constraints.MinLength = null;
        constraints.MaxLength = null;
    }

    private static void SetType(ValidationSchemaConstraints constraints, string type) =>
        constraints.Type ??= type;

    private static void SetPattern(ValidationSchemaConstraints constraints, string? pattern) {
        SetType(constraints, STRING_TYPE);
        constraints.Pattern = pattern;
    }

    private static void SetFormat(ValidationSchemaConstraints constraints, string format) {
        SetType(constraints, STRING_TYPE);
        constraints.Format = format;
    }

    private static void SetDomainPattern(
        ValidationSchemaConstraints constraints,
        IValidationRuleDescriptor rule,
        Func<string?, string> factory) {
        SetType(constraints, STRING_TYPE);
        constraints.Pattern = factory(GetValue(rule, COUNTRY_PARAMETER));
    }

    private static IValidationDescriptor? GetNestedDescriptor(IValidationRuleDescriptor rule) =>
        rule is ValidationRuleDescriptor descriptor ? descriptor.NestedDescriptor : null;

    private static bool IsCollectionType(Type type) =>
        type != typeof(string) &&
        typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

    private static string? GetValue(IValidationRuleDescriptor rule, params string[] keys) {
        foreach(var key in keys) {
            if(rule.Parameters.TryGetValue(key, out var value) && value is not null) {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static int? TryInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static decimal? TryDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool IsDecimal(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    private static IReadOnlyList<string> SplitValues(string? value) =>
        value is null
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private sealed class MemberMappingState {
        public ValidationSchemaConstraints Constraints { get; } = new();

        public List<IValidationRuleDescriptor> AdvisoryRules { get; } = [];

        public bool NotEmpty { get; set; }
    }
}
