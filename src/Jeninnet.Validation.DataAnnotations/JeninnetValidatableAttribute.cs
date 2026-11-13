//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations;

/// <summary>
/// Runs a Jeninnet.Validation validator as part of a DataAnnotations validation pipeline.
/// </summary>
/// <remarks>
/// Apply this attribute to a model class. MVC model binding and Blazor's
/// <c>DataAnnotationsValidator</c> evaluate class-level attributes, so the
/// annotated model is validated by the configured validator. The validator is
/// resolved from the validation context's service provider first, then created
/// with its parameterless constructor.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class JeninnetValidatableAttribute : ValidationAttribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="JeninnetValidatableAttribute"/> class.
    /// </summary>
    /// <param name="validatorType">The validator type; it must implement <c>IValidator&lt;T&gt;</c> for the model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="validatorType"/> is null.</exception>
    public JeninnetValidatableAttribute(Type validatorType) {
        ArgumentNullException.ThrowIfNull(validatorType);
        ValidatorType = validatorType;
    }

    /// <summary>
    /// Gets the validator type.
    /// </summary>
    public Type ValidatorType { get; }

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
    protected override DataAnnotationsValidationResult? IsValid(object? value, ValidationContext validationContext) {
        ArgumentNullException.ThrowIfNull(validationContext);

        if(validationContext.ObjectType.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
            return DataAnnotationsValidationResult.Success;
        }

        if(value is null) {
            return DataAnnotationsValidationResult.Success;
        }

        var result = JeninnetDataAnnotationsAdapter.Validate(
            value,
            validationContext.ObjectType,
            ValidatorType,
            validationContext.GetService,
            RuleSets);

        if(result.IsValid) {
            return DataAnnotationsValidationResult.Success;
        }

        var message = ErrorMessage is not null
            ? FormatErrorMessage(validationContext.DisplayName)
            : JeninnetDataAnnotationsAdapter.BuildMessage(result, IncludeSeverity, IncludeErrorCodes, MessageSeparator);

        return new DataAnnotationsValidationResult(
            message,
            JeninnetDataAnnotationsAdapter.GetMemberNames(result));
    }
}
