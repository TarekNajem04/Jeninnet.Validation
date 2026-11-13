//
// Copyright (c) Jeninnet.
// Part of the Jeninnet Platform.
// Repository: https://github.com/TarekNajem04/Jeninnet.Validation
// Licensed under the MIT License.
//
namespace Jeninnet.Validation.Observable;

/// <summary>
/// Configures how <see cref="ObservableValidationAdapter{TModel}"/> validates a model and
/// exposes its errors.
/// </summary>
public sealed class ObservableValidationOptions {
    /// <summary>
    /// Gets or sets a value indicating whether the model is validated when its
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event fires. Defaults to
    /// <see langword="false"/>; the model must implement <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public bool ValidateOnPropertyChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validation runs through the asynchronous path.
    /// When <see langword="true"/>, call
    /// <see cref="ObservableValidationAdapter{TModel}.ValidateAsync(CancellationToken)"/>.
    /// </summary>
    public bool UseAsyncValidation { get; set; }

    /// <summary>
    /// Gets or sets an optional delay applied before a property-change validation starts.
    /// Defaults to <see langword="null"/> (no delay). Requires <see cref="UseAsyncValidation"/>.
    /// </summary>
    public TimeSpan? PropertyChangedDebounce { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an unchanged, already-valid model is skipped.
    /// Property changes mark the model dirty again.
    /// </summary>
    public bool SkipIfValid { get; set; }

    /// <summary>
    /// Gets or sets the minimum severity exposed through
    /// <see cref="INotifyDataErrorInfo.GetErrors"/> and
    /// <see cref="ObservableValidationAdapter{TModel}.GetValidationErrors"/>.
    /// Defaults to <see cref="ValidationErrorSeverity.Info"/> (all severities).
    /// </summary>
    public ValidationErrorSeverity MinimumSeverity { get; set; } = ValidationErrorSeverity.Info;
}
