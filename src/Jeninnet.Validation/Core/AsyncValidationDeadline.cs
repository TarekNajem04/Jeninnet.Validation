//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Applies the per-call asynchronous validation budget. The deadline composes with the caller
/// token; caller cancellation is preserved and a deadline expiration becomes
/// <see cref="ValidationTimeoutException"/>.
/// </summary>
internal static class AsyncValidationDeadline {
    /// <summary>
    /// Rejects non-positive timeouts synchronously, before a validation run starts.
    /// </summary>
    /// <param name="timeout">The configured timeout.</param>
    /// <exception cref="ArgumentOutOfRangeException">The timeout is zero or negative.</exception>
    public static void Validate(TimeSpan? timeout) {
        if(timeout is { } value && value <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                value,
                "Timeout must be greater than zero.");
        }
    }

    /// <summary>
    /// Rejects a configured timeout on the synchronous path, which cannot honor a cooperative
    /// deadline.
    /// </summary>
    /// <param name="options">The per-call options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A timeout is configured.</exception>
    public static void ThrowIfSynchronous(ValidationCallOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        if(options.Timeout is not null) {
            throw new InvalidOperationException(
                "Timeout is only supported by ValidateAsync. A synchronous validation cannot honor " +
                "a cooperative deadline.");
        }
    }

    /// <summary>
    /// Executes an asynchronous validation with the configured deadline.
    /// </summary>
    /// <param name="options">The per-call options.</param>
    /// <param name="operation">The validation operation, bound to the linked token.</param>
    /// <param name="cancellationToken">The caller token.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="ValidationTimeoutException">The deadline expired.</exception>
    /// <exception cref="OperationCanceledException">The caller token was canceled.</exception>
    public static async ValueTask<ValidationResult> ExecuteAsync(
        ValidationCallOptions options,
        Func<CancellationToken, ValueTask<ValidationResult>> operation,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(operation);

        Validate(options.Timeout);

        if(options.Timeout is not { } timeout) {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try {
            return await operation(linked.Token).ConfigureAwait(false);
        }
        catch(OperationCanceledException exception) when(
            cancellationToken.IsCancellationRequested || timeoutSource.IsCancellationRequested) {
            if(cancellationToken.IsCancellationRequested) {
                throw;
            }

            throw new ValidationTimeoutException(timeout, exception);
        }
    }
}
