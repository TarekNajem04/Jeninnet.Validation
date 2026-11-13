# Jeninnet.Validation.Observable

Adapts `Jeninnet.Validation` validators to `INotifyDataErrorInfo` for WPF, MAUI, and Avalonia
view models. The adapter is a decorator: delegate the interface members from your view model
instead of inheriting a base class.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Observable --version 1.0.0-preview.1
```

## Usage

```csharp
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
`SkipIfValid` is enabled. For manual validation, call `adapter.Validate()` or
`await adapter.ValidateAsync()`.

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

Async validation does not block the UI thread. A newer validation cancels the previous one, and
superseded results are discarded. `PendingValidation` exposes the current field-change run.

## Error mapping

`GetErrors("Address")` includes errors from `Address.Street`, and `GetErrors("Lines")` includes
errors from `Lines[0].ProductName`. Exact paths such as `Address.Street` and
`Lines[0].ProductName` also work. `GetValidationErrors` returns the full `ValidationError` values.

## Severity

`MinimumSeverity` filters what `INotifyDataErrorInfo` exposes. The default is
`ValidationErrorSeverity.Info` (all severities); set `Warning` or `Error` to hide lower-severity
messages.

## Limits

- Property-change tracking subscribes to the root model. For nested object changes, call
  `Validate()` or forward `PropertyChanged` from the nested object.
- The adapter is not an AOT target; it is intended for desktop/UI stacks.
- DataAnnotations and Blazor use their own adapters; do not run both the Blazor component and this
  adapter for the same model unless one of them is configured to skip validation.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
