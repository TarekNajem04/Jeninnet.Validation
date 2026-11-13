//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Validates MediatR requests with all registered <see cref="IValidator{T}"/>
/// implementations before the handler runs.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="validators">The validators registered for the request type.</param>
/// <param name="options">The pipeline options.</param>
/// <param name="accessor">The scoped result accessor.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    MediatRValidationOptions options,
    ValidationResultAccessor accessor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull {
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(next);

        // The accessor is scoped and reused; a previous request must not leak its result.
        accessor.Result = null;

        var validatorList = validators as IReadOnlyList<IValidator<TRequest>> ?? [.. validators];

        if(validatorList.Count == 0) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var ruleSets = MediatRValidationRuleSetsCache.Get(typeof(TRequest));
        if(ruleSets.Count == 0) {
            ruleSets = [.. options.RuleSets];
        }

        var collector = new ValidationErrorCollector();
        var hasErrors = false;

        foreach(var validator in validatorList) {
            var result = ruleSets.Count > 0
                ? await validator.ValidateAsync(request, ruleSets, cancellationToken).ConfigureAwait(false)
                : await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);

            if(!result.IsValid) {
                collector.AddRange(result.Errors);
                hasErrors = true;
            }
        }

        if(!hasErrors) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var validationResult = ValidationResult.FromErrors(collector);
        accessor.Result = validationResult;

        if(options.FailureStrategy is MediatRValidationFailureStrategy.Throw) {
            throw new ValidationException(validationResult.Errors);
        }

        if(options.FailureStrategy is MediatRValidationFailureStrategy.Result) {
            var mapper = options.GetResultMapper(typeof(TResponse))
                ?? throw new InvalidOperationException(
                    "The Result failure strategy requires a result adapter for " +
                    $"'{typeof(TResponse).FullName}'. Call UseResultAdapter<TResponse>() when " +
                    "configuring AddValidationPipeline.");

            var mapped = mapper(validationResult);

            return mapped is TResponse response
                ? response
                : throw new InvalidOperationException(
                    "The configured validation result mapper returned " +
                    $"'{mapped?.GetType().FullName ?? "null"}', which is not assignable to " +
                    $"'{typeof(TResponse).FullName}'.");
        }

        if(options.FailureStrategy is MediatRValidationFailureStrategy.Continue) {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Unsupported MediatR validation failure strategy '{options.FailureStrategy}'.");
    }
}

/// <summary>
/// Caches the rule-set attribute lookup per request type.
/// </summary>
internal static class MediatRValidationRuleSetsCache {
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> _cache = new();

    internal static IReadOnlyList<string> Get(Type requestType) =>
        _cache.GetOrAdd(
            requestType,
            static type => type.GetCustomAttribute<MediatRValidationRuleSetsAttribute>(inherit: true)?.RuleSets ?? []);
}
