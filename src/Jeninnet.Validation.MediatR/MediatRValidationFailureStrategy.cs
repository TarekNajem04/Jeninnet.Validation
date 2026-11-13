//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.MediatR;

/// <summary>
/// Controls what the validation behavior does when a request is invalid.
/// </summary>
public enum MediatRValidationFailureStrategy {
    /// <summary>
    /// Throws <see cref="ValidationException"/> and stops the pipeline.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Stores the result in <see cref="ValidationResultAccessor"/> and continues to the
    /// next behavior so the handler can shape the response.
    /// </summary>
    Continue = 1,

    /// <summary>
    /// Maps the failed result to the response through a configured
    /// <see cref="MediatRValidationOptions.UseResultAdapter{TResponse}"/> adapter and skips the
    /// handler.
    /// </summary>
    Result = 2
}
