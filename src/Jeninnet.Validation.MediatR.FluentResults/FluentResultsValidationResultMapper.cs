//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR.FluentResults;

/// <summary>
/// Maps a failed validation result to a failed <see cref="Result{TValue}"/> with one
/// <see cref="Error"/> per validation error. Member name, severity, code, and custom state are
/// preserved in <see cref="Error.Metadata"/>.
/// </summary>
/// <typeparam name="TValue">The successful response value type.</typeparam>
public sealed class FluentResultsValidationResultMapper<TValue> : IValidationResultMapper<Result<TValue>> {
    /// <inheritdoc />
    /// <exception cref="ArgumentException"><paramref name="result"/> is valid.</exception>
    public Result<TValue> Map(ValidationResult result) {
        if(result.IsValid) {
            throw new ArgumentException("The result mapper requires a failed validation result.", nameof(result));
        }

        var errors = result.Errors
            .Select(static error => new Error(ValidationErrorFormatter.GetMessage(error))
                .WithMetadata(ValidationErrorFormatter.GetMetadata(error)))
            .ToList();

        return Result.Fail<TValue>(errors);
    }
}
