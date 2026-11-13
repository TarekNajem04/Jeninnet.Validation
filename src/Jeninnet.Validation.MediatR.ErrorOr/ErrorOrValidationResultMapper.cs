//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.ErrorOr;

/// <summary>
/// Maps a failed validation result to <see cref="ErrorOr{T}"/> validation errors.
/// </summary>
/// <typeparam name="TValue">The successful response value type.</typeparam>
public sealed class ErrorOrValidationResultMapper<TValue> : IValidationResultMapper<ErrorOr<TValue>> {
    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="result"/> is valid.</exception>
    public ErrorOr<TValue> Map(ValidationResult result) {
        if(result.IsValid) {
            throw new ArgumentException("The result mapper requires a failed validation result.", nameof(result));
        }

        var errors = result.Errors
            .Select(static error => Error.Validation(
                ValidationErrorFormatter.GetCode(error),
                ValidationErrorFormatter.GetMessage(error),
                ValidationErrorFormatter.GetMetadata(error)))
            .ToList();

        return ErrorOrFactory.From<TValue>(errors);
    }
}
