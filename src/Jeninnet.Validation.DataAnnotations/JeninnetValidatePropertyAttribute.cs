//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations;

/// <summary>
/// Runs the rules for one member of a model as part of a DataAnnotations property pipeline.
/// </summary>
/// <remarks>
/// Apply this attribute to a property or field when only that member should be validated, for
/// example by MVC property validation or a Blazor <c>DataAnnotationsValidator</c>. The validator
/// is resolved from the validation context's service provider first, then created with its
/// parameterless constructor. When the model also has <see cref="JeninnetValidatableAttribute"/>,
/// the class-level attribute wins and this attribute does nothing.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class JeninnetValidatePropertyAttribute : ValidationAttribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="JeninnetValidatePropertyAttribute"/> class.
    /// </summary>
    /// <param name="validatorType">The validator type; it must implement <c>IValidator&lt;T&gt;</c> for the model.</param>
    /// <param name="memberName">
    /// The validator member path to run, such as <c>Name</c>, <c>Address.Street</c>, or
    /// <c>Lines[0].ProductName</c>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="validatorType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is empty or whitespace.</exception>
    public JeninnetValidatePropertyAttribute(Type validatorType, string memberName) {
        ArgumentNullException.ThrowIfNull(validatorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        ValidatorType = validatorType;
        MemberName = memberName.Trim();
    }

    /// <summary>
    /// Gets the validator type.
    /// </summary>
    public Type ValidatorType { get; }

    /// <summary>
    /// Gets the validator member path this attribute runs.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets or sets the rule sets passed to the validator. Empty means all rules.
    /// </summary>
    public string[] RuleSets { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether non-error severities are prefixed
    /// with <c>[Warning]</c> or <c>[Info]</c>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeSeverity { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether error codes are appended to messages.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeErrorCodes { get; set; }

    /// <summary>
    /// Gets or sets the separator used when multiple errors are joined into one
    /// DataAnnotations message. Defaults to <c>"; "</c>.
    /// </summary>
    public string MessageSeparator { get; set; } = "; ";

    /// <inheritdoc />
    protected override DataAnnotationsValidationResult? IsValid(
        object? value,
        ValidationContext validationContext) {
        ArgumentNullException.ThrowIfNull(validationContext);

        _ = value;

        if(validationContext.ObjectType.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
            return DataAnnotationsValidationResult.Success;
        }

        if(validationContext.MemberName is { Length: > 0 } memberName) {
            var member = (MemberInfo?)validationContext.ObjectType.GetProperty(memberName)
                         ?? validationContext.ObjectType.GetField(memberName);

            if(member?.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
                return DataAnnotationsValidationResult.Success;
            }
        }

        if(validationContext.ObjectType.GetCustomAttribute<JeninnetValidatableAttribute>(inherit: true) is not null) {
            return DataAnnotationsValidationResult.Success;
        }

        if(validationContext.ObjectInstance is null) {
            throw new InvalidOperationException(
                "Property-level Jeninnet validation requires a model instance.");
        }

        var result = JeninnetDataAnnotationsAdapter.ValidateProperty(
            validationContext.ObjectInstance,
            validationContext.ObjectType,
            ValidatorType,
            MemberName,
            validationContext.GetService,
            RuleSets);

        if(result.IsValid) {
            return DataAnnotationsValidationResult.Success;
        }

        var message = ErrorMessage is not null
            ? FormatErrorMessage(validationContext.DisplayName)
            : JeninnetDataAnnotationsAdapter.BuildMessage(
                result,
                IncludeSeverity,
                IncludeErrorCodes,
                MessageSeparator);

        return new DataAnnotationsValidationResult(
            message,
            JeninnetDataAnnotationsAdapter.GetMemberNames(result));
    }
}

/// <summary>
/// Runs the rules for one member of a model as part of a DataAnnotations property pipeline,
/// using a generic validator type.
/// </summary>
/// <typeparam name="TValidator">The validator type.</typeparam>
/// <remarks>
/// This is the generic syntax for <see cref="JeninnetValidatePropertyAttribute"/>:
/// <c>[JeninnetValidateProperty&lt;MyValidator&gt;(nameof(MyModel.Name))]</c>. The validator type
/// is resolved from the validation context's service provider first, then created with its
/// parameterless constructor.
/// </remarks>
/// <param name="memberName">The validator member path to run.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class JeninnetValidatePropertyAttribute<TValidator>(string memberName) : JeninnetValidatePropertyAttribute(typeof(TValidator), memberName)
    where TValidator : class {
}
