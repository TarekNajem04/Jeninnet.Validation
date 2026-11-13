//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.AspNetCore;

/// <summary>
/// Converts an unhandled <see cref="ValidationException"/> into a 400 ProblemDetails response.
/// </summary>
/// <param name="options">The problem-detail options.</param>
public sealed class ValidationExceptionHandler(ValidationProblemOptions options) : IExceptionHandler {
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if(exception is not ValidationException validationException) {
            return false;
        }

        var problem = new ValidationProblemDetails(validationException.Errors.ToProblemErrors(options)) {
            Status = StatusCodes.Status400BadRequest,
            Title = options.Title ?? "One or more validation errors occurred."
        };

        ValidationProblemExtensions.EnrichProblem(problem, httpContext, options);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
