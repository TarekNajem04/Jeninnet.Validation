//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.CSharpFunctionalExtensions;

/// <summary>
/// CSharpFunctionalExtensions registration helpers for the MediatR validation pipeline.
/// </summary>
public static class CSharpFunctionalExtensionsMediatRExtensions {
    /// <summary>
    /// Configures the pipeline to return a failed <see cref="Result{TValue}"/> instead of
    /// throwing or calling the handler.
    /// </summary>
    /// <typeparam name="TValue">The successful response value type.</typeparam>
    /// <param name="options">The pipeline options.</param>
    /// <returns>The same options for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static MediatRValidationOptions UseCSharpFunctionalExtensionsAdapter<TValue>(
        this MediatRValidationOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        return options.UseResultAdapter<Result<TValue>>(
            new CSharpFunctionalExtensionsValidationResultMapper<TValue>());
    }
}
