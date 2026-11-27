#if NET11_0_OR_GREATER
//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.DataAnnotations;

public static partial class JeninnetDataAnnotationsAdapter {
    internal static async ValueTask<JeninnetValidationResult> ValidateAsync(
        object instance,
        Type modelType,
        Type validatorType,
        Func<Type, object?>? serviceProvider = null,
        IReadOnlyList<string>? ruleSets = null,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(validatorType);

        if(modelType.GetCustomAttribute<SkipValidationAttribute>(inherit: true) is not null) {
            return JeninnetValidationResult.Success;
        }

        var validator = ResolveValidator(modelType, validatorType, serviceProvider);
        var asyncInterface = typeof(IAsyncValidator<>).MakeGenericType(modelType);

        if(!asyncInterface.IsInstanceOfType(validator)) {
            throw new InvalidOperationException(
                $"Validator '{validatorType.FullName}' does not implement IAsyncValidator<{modelType.Name}>.");
        }

        var validateMethod = asyncInterface.GetMethod(
            "ValidateAsync",
            [modelType, typeof(IEnumerable<string>), typeof(CancellationToken)])
            ?? throw new InvalidOperationException(
                $"Could not find the asynchronous ValidateAsync method for IAsyncValidator<{modelType.Name}>.");

        return await InvokeAsync(
            validateMethod,
            validator,
            [instance, ruleSets ?? [], cancellationToken],
            validatorType).ConfigureAwait(false);
    }

    internal static async ValueTask<JeninnetValidationResult> ValidatePropertyAsync(
        object instance,
        Type modelType,
        Type validatorType,
        string memberName,
        Func<Type, object?>? serviceProvider = null,
        IReadOnlyList<string>? ruleSets = null,
        CancellationToken cancellationToken = default) {
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
            "ValidateAsync",
            [modelType, typeof(ValidationCallOptions), typeof(CancellationToken)])
            ?? throw new InvalidOperationException(
                $"Could not find the asynchronous property-level ValidateAsync method for IValidator<{modelType.Name}>.");

        var options = new ValidationCallOptions {
            RuleSets = ruleSets is { Count: > 0 } ? ruleSets : null,
            IncludeProperties = [memberName.Trim()]
        };

        return await InvokeAsync(
            validateMethod,
            validator,
            [instance, options, cancellationToken],
            validatorType).ConfigureAwait(false);
    }

    private static async ValueTask<JeninnetValidationResult> InvokeAsync(
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

        if(result is not ValueTask<JeninnetValidationResult> task) {
            throw new InvalidOperationException(
                $"Validator '{validatorType.FullName}' returned no asynchronous validation result.");
        }

        return await task.ConfigureAwait(false);
    }
}
#endif
