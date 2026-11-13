//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//

namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Invokes a validator with the request service provider so the effective
/// <see cref="ValidationOptions"/> come from the application's container.
/// </summary>
internal static class ValidationInvoker {
    internal static readonly ConcurrentDictionary<Type, Func<object, object, IEnumerable<string>?, IServiceProvider, CancellationToken, ValueTask<ValidationResult>>> Invokers = new();

    internal static Func<object, object, IEnumerable<string>?, IServiceProvider, CancellationToken, ValueTask<ValidationResult>> Create(Type modelType) {
        var method = typeof(ValidationInvoker)
            .GetMethod(nameof(InvokeAsync), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(modelType);

        var validatorParameter = Expression.Parameter(typeof(object), "validator");
        var modelParameter = Expression.Parameter(typeof(object), "model");
        var ruleSetsParameter = Expression.Parameter(typeof(IEnumerable<string>), "ruleSets");
        var servicesParameter = Expression.Parameter(typeof(IServiceProvider), "services");
        var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            method,
            Expression.Convert(validatorParameter, typeof(IValidator<>).MakeGenericType(modelType)),
            Expression.Convert(modelParameter, modelType),
            ruleSetsParameter,
            servicesParameter,
            cancellationTokenParameter);

        return Expression
            .Lambda<Func<object, object, IEnumerable<string>?, IServiceProvider, CancellationToken, ValueTask<ValidationResult>>>(
                call,
                validatorParameter,
                modelParameter,
                ruleSetsParameter,
                servicesParameter,
                cancellationTokenParameter)
            .Compile();
    }

    public static ValueTask<ValidationResult> InvokeAsync<T>(
        IValidator<T> validator,
        T model,
        IEnumerable<string>? ruleSets,
        IServiceProvider services,
        CancellationToken cancellationToken) {
        if(validator is IValidationOptionsValidator<T> optionsValidator) {
            return optionsValidator.ValidateAsync(
                model,
                new ValidationCallOptions {
                    RuleSets = ruleSets,
                    ServiceProvider = services
                },
                cancellationToken);
        }

        return validator.ValidateAsync(model, ruleSets ?? [], cancellationToken);
    }
}
