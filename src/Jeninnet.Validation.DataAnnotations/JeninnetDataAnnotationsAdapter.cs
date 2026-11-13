//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations;

/// <summary>
/// Converts Jeninnet.Validation results into DataAnnotations results and runs
/// validators from DataAnnotations-based pipelines.
/// </summary>
public static class JeninnetDataAnnotationsAdapter {
    /// <summary>
    /// Runs a validator for the supplied model instance.
    /// </summary>
    /// <param name="instance">The model instance.</param>
    /// <param name="modelType">The validated model type.</param>
    /// <param name="validatorType">The validator type.</param>
    /// <param name="serviceProvider">Optional service lookup (for example <c>ValidationContext.GetService</c>).</param>
    /// <param name="ruleSets">Optional rule sets; empty means all rules.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="InvalidOperationException">The validator cannot be resolved or does not implement <c>IValidator&lt;T&gt;</c> for the model.</exception>
    public static JeninnetValidationResult Validate(
        object instance,
        Type modelType,
        Type validatorType,
        Func<Type, object?>? serviceProvider = null,
        IReadOnlyList<string>? ruleSets = null) {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(validatorType);

        if(modelType.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
            return JeninnetValidationResult.Success;
        }

        var validator = ResolveValidator(modelType, validatorType, serviceProvider);
        var validateMethod = typeof(ISyncValidator<>)
            .MakeGenericType(modelType)
            .GetMethod("Validate", [modelType, typeof(IEnumerable<string>)])
            ?? throw new InvalidOperationException(
                $"Could not find the synchronous Validate method for IValidator<{modelType.Name}>.");

        return InvokeValidate(validateMethod, validator, [instance, ruleSets ?? []], validatorType);
    }

    /// <summary>
    /// Runs only the rules for one member of the supplied model instance.
    /// </summary>
    /// <param name="instance">The model instance.</param>
    /// <param name="modelType">The validated model type.</param>
    /// <param name="validatorType">The validator type.</param>
    /// <param name="memberName">The member path to validate.</param>
    /// <param name="serviceProvider">Optional service lookup (for example <c>ValidationContext.GetService</c>).</param>
    /// <param name="ruleSets">Optional rule sets; empty means all rules.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="memberName"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// The validator cannot be resolved, does not implement <c>IValidator&lt;T&gt;</c> for the model,
    /// or does not support <see cref="ValidationCallOptions.IncludeProperties"/>.
    /// </exception>
    public static JeninnetValidationResult ValidateProperty(
        object instance,
        Type modelType,
        Type validatorType,
        string memberName,
        Func<Type, object?>? serviceProvider = null,
        IReadOnlyList<string>? ruleSets = null) {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(validatorType);
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);

        if(modelType.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
            return JeninnetValidationResult.Success;
        }

        var validator = ResolveValidator(modelType, validatorType, serviceProvider);
        var optionsValidatorInterface = typeof(IValidationOptionsValidator<>).MakeGenericType(modelType);

        if(!optionsValidatorInterface.IsInstanceOfType(validator)) {
            throw new InvalidOperationException(
                $"Validator '{validatorType.FullName}' does not implement " +
                $"IValidationOptionsValidator<{modelType.Name}>; property-level validation " +
                "requires IncludeProperties support.");
        }

        var validateMethod = optionsValidatorInterface.GetMethod(
            "Validate",
            [modelType, typeof(ValidationCallOptions)])
            ?? throw new InvalidOperationException(
                $"Could not find the property-level Validate method for IValidator<{modelType.Name}>.");

        var options = new ValidationCallOptions {
            RuleSets = ruleSets is { Count: > 0 } ? ruleSets : null,
            IncludeProperties = [memberName.Trim()]
        };

        return InvokeValidate(validateMethod, validator, [instance, options], validatorType);
    }

    /// <summary>
    /// Converts a Jeninnet.Validation result into one DataAnnotations result.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="includeSeverity">Whether to prefix non-error severities.</param>
    /// <param name="includeErrorCodes">Whether to append error codes.</param>
    /// <param name="separator">The separator between multiple messages.</param>
    /// <returns>A single DataAnnotations result, or an empty list when validation passed.</returns>
    public static IReadOnlyList<DataAnnotationsValidationResult> ToValidationResults(
        JeninnetValidationResult result,
        bool includeSeverity = true,
        bool includeErrorCodes = false,
        string separator = "; ") {
        if(result.IsValid) {
            return [];
        }

        var members = GetMemberNames(result);

        return [
            new DataAnnotationsValidationResult(
                BuildMessage(result, includeSeverity, includeErrorCodes, separator),
                members.Count > 0 ? members : null)
        ];
    }

    /// <summary>
    /// Builds the joined DataAnnotations message for a failed result.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <param name="includeSeverity">Whether to prefix non-error severities.</param>
    /// <param name="includeErrorCodes">Whether to append error codes.</param>
    /// <param name="separator">The separator between multiple messages.</param>
    /// <returns>The joined message.</returns>
    public static string BuildMessage(
        JeninnetValidationResult result,
        bool includeSeverity = true,
        bool includeErrorCodes = false,
        string separator = "; ") {
        ArgumentNullException.ThrowIfNull(separator);

        return string.Join(
            separator,
            result.Errors.Select(error => FormatError(error, includeSeverity, includeErrorCodes)));
    }

    /// <summary>
    /// Gets the distinct non-empty member names from a failed result.
    /// </summary>
    /// <param name="result">The validation result.</param>
    /// <returns>The member names.</returns>
    public static IReadOnlyList<string> GetMemberNames(JeninnetValidationResult result) =>
        [
            .. result.Errors
                .Select(static error => error.MemberName)
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
        ];

    private static JeninnetValidationResult InvokeValidate(
        MethodInfo validateMethod,
        object validator,
        object?[] arguments,
        Type validatorType) {
        object? result;

        try {
            result = validateMethod.Invoke(validator, arguments);
        }
        catch(TargetInvocationException exception) when(exception.InnerException is not null) {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        return result is JeninnetValidationResult typedResult
            ? typedResult
            : throw new InvalidOperationException(
                $"Validator '{validatorType.FullName}' returned no validation result.");
    }

    private static object ResolveValidator(
        Type modelType,
        Type validatorType,
        Func<Type, object?>? serviceProvider) {
        var validatorInterface = typeof(IValidator<>).MakeGenericType(modelType);
        var validator = serviceProvider?.Invoke(validatorInterface) ?? serviceProvider?.Invoke(validatorType);
        validator ??= CreateValidator(validatorType, modelType);

        if(!validatorInterface.IsInstanceOfType(validator)) {
            throw new InvalidOperationException(
                $"Validator '{validatorType.FullName}' does not implement IValidator<{modelType.Name}>.");
        }

        return validator;
    }

    private static object CreateValidator(Type validatorType, Type modelType) {
        try {
            return Activator.CreateInstance(validatorType)
                   ?? throw new InvalidOperationException(
                       $"Could not create validator '{validatorType.FullName}'.");
        }
        catch(Exception exception) when(exception is MissingMethodException or TargetInvocationException) {
            throw new InvalidOperationException(
                $"Could not create validator '{validatorType.FullName}'. Register " +
                $"IValidator<{modelType.Name}> or the concrete validator in the validation service provider, " +
                "or give the validator a parameterless constructor.",
                exception);
        }
    }

    private static string FormatError(
        ValidationError error,
        bool includeSeverity,
        bool includeErrorCodes) {
        var message = error.Message ?? error.MessageKey ?? "Invalid";

        if(includeSeverity && error.Severity is not ValidationErrorSeverity.Error) {
            message = error.Severity is ValidationErrorSeverity.Warning
                ? $"[Warning] {message}"
                : $"[Info] {message}";
        }

        if(includeErrorCodes && !string.IsNullOrWhiteSpace(error.ErrorCode)) {
            message = $"{message} ({error.ErrorCode})";
        }

        return message;
    }
}
