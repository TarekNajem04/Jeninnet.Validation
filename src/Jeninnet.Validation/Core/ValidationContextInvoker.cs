//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Core;

/// <summary>
/// Dispatches a context-aware validator on the synchronous fallback path. Kept separate from
/// async methods so the intentional synchronous fallback is explicit to readers and analyzers.
/// </summary>
internal static class ValidationContextInvoker {
    internal static ValidationResult InvokeSync<T>(
        IValidationContextValidator<T> validator,
        T instance,
        ValidationContext<T> context) =>
        validator.Validate(instance, context);
}
