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
    /// Gets or sets a value indicating whether the adapter captures
    /// <see cref="SynchronizationContext.Current"/> at construction and posts asynchronous
    /// validation completion, including <see cref="ObservableValidationAdapter{TModel}.ErrorsChanged"/>,
    /// to that context. Defaults to <see langword="false"/>; when false, asynchronous completion
    /// runs on the thread that completes validation.
    /// <para>
    /// This option is ignored when <see cref="Post"/> is set. Synchronous <c>Validate()</c> and
    /// <c>ClearErrors()</c> always run inline on the calling thread.
    /// </para>
    /// </summary>
    public bool CaptureSynchronizationContext { get; set; }

    /// <summary>
    /// Gets or sets a delegate that posts an action to the UI thread. Use it when the UI framework
    /// has no <see cref="SynchronizationContext"/> or when a dispatcher-specific API is preferred
    /// (for example WPF's <c>Dispatcher.InvokeAsync</c>, Avalonia's <c>Dispatcher.UIThread.Post</c>,
    /// or MAUI's <c>MainThread.BeginInvokeOnMainThread</c>).
    /// <para>
    /// When set, it takes precedence over <see cref="CaptureSynchronizationContext"/>.
    /// <c>ValidateAsync</c> waits for the posted action to finish before it completes.
    /// </para>
    /// </summary>
    public Action<Action>? Post { get; set; }

    /// <summary>
    /// Gets or sets the minimum severity exposed through
    /// <see cref="INotifyDataErrorInfo.GetErrors"/> and
    /// <see cref="ObservableValidationAdapter{TModel}.GetValidationErrors"/>.
    /// Defaults to <see cref="ValidationErrorSeverity.Info"/> (all severities).
    /// </summary>
    public ValidationErrorSeverity MinimumSeverity { get; set; } = ValidationErrorSeverity.Info;
}
