//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Blazor;

/// <summary>
/// Configures how <see cref="EditContextValidationCoordinator{TModel}"/> validates an edit context.
/// </summary>
public sealed class EditContextValidationOptions {
    /// <summary>
    /// Gets or sets a value indicating whether the model is validated when a field changes.
    /// Defaults to <see langword="false"/> (submit/validation-requested only).
    /// </summary>
    public bool ValidateOnFieldChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validation runs through
    /// <see cref="IAsyncValidator{T}.ValidateAsync(T, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, call
    /// <see cref="EditContextValidationCoordinator{TModel}.ValidateAsync(CancellationToken)"/>
    /// from <c>EditForm.OnSubmit</c>; <see cref="EditContext.Validate"/> cannot await validation.
    /// Field changes, when enabled, are validated asynchronously.
    /// </remarks>
    public bool UseAsyncValidation { get; set; }

    /// <summary>
    /// Gets or sets an optional delay applied before a field-change validation starts.
    /// Defaults to <see langword="null"/> (no delay). Requires <see cref="UseAsyncValidation"/>.
    /// </summary>
    public TimeSpan? FieldChangedDebounce { get; set; }

    /// <summary>
    /// Gets or sets the double-validation toggle. When <see langword="true"/>, validation is
    /// skipped while the edit context is already valid and the model has not changed since the
    /// last successful validation.
    /// </summary>
    public bool SkipIfModelStateValid { get; set; }
}
