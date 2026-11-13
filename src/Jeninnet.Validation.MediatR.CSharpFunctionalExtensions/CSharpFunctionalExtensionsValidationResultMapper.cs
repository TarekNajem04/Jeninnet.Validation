//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.CSharpFunctionalExtensions;

/// <summary>
/// Maps a failed validation result to a failed <see cref="Result{TValue}"/>.
/// </summary>
/// <remarks>
/// CSharpFunctionalExtensions stores only a string error on <see cref="Result{TValue}"/>, so
/// member names, severities, and codes are preserved textually in the failure message; custom
/// state and message parameters are not representable.
/// </remarks>
/// <typeparam name="TValue">The successful response value type.</typeparam>
public sealed class CSharpFunctionalExtensionsValidationResultMapper<TValue>
    : IValidationResultMapper<Result<TValue>> {
    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="result"/> is valid.</exception>
    public Result<TValue> Map(ValidationResult result) {
        if(result.IsValid) {
            throw new ArgumentException("The result mapper requires a failed validation result.", nameof(result));
        }

        return Result.Failure<TValue>(ValidationErrorFormatter.JoinMessages(result));
    }
}
