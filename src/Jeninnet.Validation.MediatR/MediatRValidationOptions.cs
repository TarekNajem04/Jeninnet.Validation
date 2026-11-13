//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Options for the MediatR validation pipeline.
/// </summary>
public sealed class MediatRValidationOptions {
    /// <summary>
    /// Gets or sets the failure strategy. Defaults to <see cref="MediatRValidationFailureStrategy.Throw"/>.
    /// </summary>
    public MediatRValidationFailureStrategy FailureStrategy { get; set; } = MediatRValidationFailureStrategy.Throw;

    /// <summary>
    /// Gets the default rule sets applied when a request type has no
    /// <see cref="MediatRValidationRuleSetsAttribute"/>. Empty means all rules.
    /// </summary>
    public IList<string> RuleSets { get; } = [];

    private readonly Dictionary<Type, Func<ValidationResult, object?>> _resultMappers = [];

    /// <summary>
    /// Gets the result mapper registered for the specified response type.
    /// </summary>
    /// <param name="responseType">The response type.</param>
    /// <returns>The mapper, or <see langword="null"/> when none is registered.</returns>
    internal Func<ValidationResult, object?>? GetResultMapper(Type responseType) =>
        _resultMappers.TryGetValue(responseType, out var mapper) ? mapper : null;

    /// <summary>
    /// Configures a result adapter and switches the failure strategy to
    /// <see cref="MediatRValidationFailureStrategy.Result"/>. The adapter maps a failed
    /// validation result to the MediatR response type and skips the handler.
    /// </summary>
    /// <typeparam name="TResponse">The MediatR response type.</typeparam>
    /// <param name="mapper">The result mapper.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is null.</exception>
    public MediatRValidationOptions UseResultAdapter<TResponse>(
        IValidationResultMapper<TResponse> mapper) {
        ArgumentNullException.ThrowIfNull(mapper);

        _resultMappers[typeof(TResponse)] = result => mapper.Map(result);
        FailureStrategy = MediatRValidationFailureStrategy.Result;
        return this;
    }
}
