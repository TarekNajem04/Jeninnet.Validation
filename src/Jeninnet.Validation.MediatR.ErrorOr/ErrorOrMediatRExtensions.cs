//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.ErrorOr;

/// <summary>
/// ErrorOr registration helpers for the MediatR validation pipeline.
/// </summary>
public static class ErrorOrMediatRExtensions {
    /// <summary>
    /// Configures the pipeline to return <see cref="ErrorOr{T}"/> validation errors instead of
    /// throwing or calling the handler.
    /// </summary>
    /// <typeparam name="TValue">The successful response value type.</typeparam>
    /// <param name="options">The pipeline options.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static MediatRValidationOptions UseErrorOrResultAdapter<TValue>(
        this MediatRValidationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return options.UseResultAdapter<ErrorOr<TValue>>(
            new ErrorOrValidationResultMapper<TValue>());
    }
}
