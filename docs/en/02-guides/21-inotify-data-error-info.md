# INotifyDataErrorInfo (WPF, MAUI, Avalonia)

The `Jeninnet.Validation.Observable` package adapts Jeninnet validators to
`INotifyDataErrorInfo`, the change-notification contract used by WPF, MAUI, and Avalonia data
binding. It is a decorator: your view model delegates the interface members to the adapter instead
of inheriting from a framework base class.

## Usage

```csharp
using System.ComponentModel;
using Jeninnet.Validation.Observable;

public sealed class OrderViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly ObservableValidationAdapter<Order> _validation;

    public OrderViewModel(IValidator<Order> validator)
    {
        Model = new Order();
        _validation = new ObservableValidationAdapter<Order>(
            Model,
            validator,
            new ObservableValidationOptions
            {
                ValidateOnPropertyChanged = true,
                SkipIfValid = true
            });

        _validation.ErrorsChanged += (_, args) => ErrorsChanged?.Invoke(this, args);
    }

    public Order Model { get; }

    public bool HasErrors => _validation.HasErrors;

    public IEnumerable GetErrors(string? propertyName) => _validation.GetErrors(propertyName);

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

The model must implement `INotifyPropertyChanged` when `ValidateOnPropertyChanged` or
`SkipIfValid` is enabled. The adapter does not reference WPF, MAUI, or Avalonia; only the
`System.ComponentModel` contracts.

## Behavior

- `ValidateOnPropertyChanged="true"` validates the whole model whenever the root model raises
  `PropertyChanged`. The default is manual validation through `Validate()`/`ValidateAsync()`.
- `ErrorsChanged` is raised for every affected path, including ancestors used by bindings
  (`Address` for `Address.Street`, and `Lines` for `Lines[0].ProductName`).
- `HasErrors` reflects the errors exposed after severity filtering.
- `GetErrors` returns message strings; `GetValidationErrors` returns the full `ValidationError`
  values (severity, code, message key, custom state).

## Error mapping

`GetErrors` accepts exact paths and owner paths:

```csharp
adapter.GetErrors("Address.Street");          // exact nested error
adapter.GetErrors("Address");                 // includes Address.Street
adapter.GetErrors("Lines[0].ProductName");    // exact indexed error
adapter.GetErrors("Lines");                   // includes all line errors
adapter.GetErrors(null);                      // model-level errors only
```

Unresolved or error-free paths return an empty sequence.

## Async validation

```csharp
var adapter = new ObservableValidationAdapter<Order>(
    order,
    validator,
    new ObservableValidationOptions
    {
        UseAsyncValidation = true,
        ValidateOnPropertyChanged = true,
        PropertyChangedDebounce = TimeSpan.FromMilliseconds(300)
    });
```

`ValidateAsync()` runs `IAsyncValidator<T>.ValidateAsync`, including `MustAsync` and
`CustomAsync` rules, without blocking the UI thread. A newer validation cancels the previous one,
and a superseded result is discarded. `PendingValidation` exposes the current property-change run.

The debounce is opt-in and requires async validation; no delay is applied by default.

## Severity

`MinimumSeverity` filters what the adapter exposes. The default is
`ValidationErrorSeverity.Info`, which includes all severities. Set `Warning` or `Error` to hide
lower-severity messages from `GetErrors` and `HasErrors`:

```csharp
new ObservableValidationOptions
{
    MinimumSeverity = ValidationErrorSeverity.Error
};
```

## Double validation

`SkipIfValid` skips an unchanged, already-valid model. A root property change marks the model
dirty again, and a failed validation always re-runs. Use it when another layer already validated
the model, or when a submit handler might trigger a second validation pass.

## Limits

- Property-change tracking subscribes to the root model. For nested object changes, call
  `Validate()` or forward `PropertyChanged` from the nested object.
- `Validate()` on an async-only validator still throws
  `AsyncValidatorInvokedSynchronouslyException`; use `ValidateAsync()`.
- The adapter is not an AOT target; it is intended for desktop and MAUI UI stacks.
- Do not run the Blazor `EditContext` component and this adapter for the same model unless one of
  them is configured to skip validation.

## Verify

Bind a view model property to a control, set an invalid value, and confirm the control shows the
error inline. Change the value to a valid one and confirm `ErrorsChanged` clears it.

## Related

- [Collections and nesting](06-collections-and-nesting.md)
- [Integration packages](../03-reference/05-integration-packages.md)
- [Known limitations](../03-reference/07-known-limitations.md)
