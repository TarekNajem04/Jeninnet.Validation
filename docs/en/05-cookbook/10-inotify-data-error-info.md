# Validate a Desktop View Model

## Problem

You want inline, change-driven validation in WPF, MAUI, or Avalonia using the same Jeninnet
validator as the rest of the application, without inheriting from a framework base class.

## Solution

Add the optional package:

```xml
<PackageReference Include="Jeninnet.Validation.Observable" Version="1.0.0-preview.1" />
```

Create an adapter and delegate `INotifyDataErrorInfo` from the view model:

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

The model must implement `INotifyPropertyChanged` when change tracking is enabled.

## Async rules

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

var isValid = await adapter.ValidateAsync();
```

A newer validation cancels the previous one, and stale results are discarded. The debounce is
opt-in and requires async validation.

## Nested and collection errors

```csharp
adapter.GetErrors("Address.Street");        // exact
adapter.GetErrors("Address");               // includes Address.Street
adapter.GetErrors("Lines[0].ProductName");  // exact
adapter.GetErrors("Lines");                 // includes all line errors
```

`ErrorsChanged` is raised for the exact path and its ancestors, so bindings update at every level.

## Severity

```csharp
new ObservableValidationOptions
{
    MinimumSeverity = ValidationErrorSeverity.Error
};
```

Warning and Info messages are hidden from `GetErrors`/`HasErrors` when the minimum is `Error`.

## Verify

Set an invalid value on a bound property and confirm the control shows the error inline. Change it
to a valid value and confirm the error clears. For async rules, confirm the UI remains responsive
and only the latest validation result is applied.

## Related

- [INotifyDataErrorInfo guide](../02-guides/21-inotify-data-error-info.md)
- [Collections and nesting](../02-guides/06-collections-and-nesting.md)
