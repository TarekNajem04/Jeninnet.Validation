//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation;

/// <summary>
/// Thrown when a validator that declares only asynchronous rules is executed
/// through its synchronous <see cref="ISyncValidator{T}"/> path.
/// </summary>
/// <remarks>
/// Synchronous execution cannot honour asynchronous rules without either
/// silently dropping them (producing a false <see cref="ValidationResult.Success"/>)
/// or blocking on asynchronous code; both failure modes are forbidden by the
/// runtime contract. Call
/// <see cref="IAsyncValidator{T}.ValidateAsync(T, CancellationToken)"/> instead, or
/// add synchronous rules if synchronous validation is intended.
/// </remarks>
public sealed class AsyncValidatorInvokedSynchronouslyException : Exception {
    /// <summary>
    /// Gets the name of the validator type that was invoked synchronously.
    /// </summary>
    public string ValidatorTypeName { get; } = string.Empty;

    /// <summary>
    /// Gets the number of asynchronous rules carried by the validator.
    /// </summary>
    public int AsyncRuleCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidatorInvokedSynchronouslyException"/> class.
    /// </summary>
    /// <param name="validatorTypeName">The name of the validator type that was invoked synchronously.</param>
    /// <param name="asyncRuleCount">The number of asynchronous rules the validator declares.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="validatorTypeName"/> is empty or whitespace.</exception>
    public AsyncValidatorInvokedSynchronouslyException(string validatorTypeName, int asyncRuleCount)
        : base(ComposeMessage(validatorTypeName, asyncRuleCount)) {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatorTypeName);

        ValidatorTypeName = validatorTypeName;
        AsyncRuleCount = asyncRuleCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidatorInvokedSynchronouslyException"/> class with a
    /// reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="validatorTypeName">The name of the validator type that was invoked synchronously.</param>
    /// <param name="asyncRuleCount">The number of asynchronous rules the validator declares.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="validatorTypeName"/> is empty or whitespace.</exception>
    public AsyncValidatorInvokedSynchronouslyException(
        string validatorTypeName,
        int asyncRuleCount,
        Exception innerException
    ) : base(ComposeMessage(validatorTypeName, asyncRuleCount), innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatorTypeName);

        ValidatorTypeName = validatorTypeName;
        AsyncRuleCount = asyncRuleCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidatorInvokedSynchronouslyException"/> class.
    /// </summary>
    public AsyncValidatorInvokedSynchronouslyException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidatorInvokedSynchronouslyException"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public AsyncValidatorInvokedSynchronouslyException(string? message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncValidatorInvokedSynchronouslyException"/> class
    /// with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AsyncValidatorInvokedSynchronouslyException(string? message, Exception? innerException) : base(message, innerException) {
    }

    private static string ComposeMessage(string validatorTypeName, int asyncRuleCount) =>
        $"The validator '{validatorTypeName}' declares {asyncRuleCount} asynchronous rule(s) and no synchronous rules. " +
        "Synchronous Validate() would silently skip them; call ValidateAsync() instead, or add synchronous rules.";
}
