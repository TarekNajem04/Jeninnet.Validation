# Validate a Blazor Form

## Problem

You want the same Jeninnet validator to run inside a Blazor `EditForm` and show errors next to the
right fields, including nested objects and collection items.

## Solution

```razor
@using Jeninnet.Validation.Blazor

<EditForm Model="order" OnValidSubmit="Submit">
    <JeninnetValidator TModel="Order" />

    <InputText @bind-Value="order.CustomerName" />
    <ValidationMessage For="() => order.CustomerName" />

    <InputText @bind-Value="order.Address.Street" />
    <ValidationMessage For="() => order.Address.Street" />

    @foreach (var line in order.Lines)
    {
        <InputText @bind-Value="line.ProductName" />
        <ValidationMessage For="() => line.ProductName" />
    }

    <button type="submit">Save</button>
</EditForm>
```

Register the validator:

```csharp
builder.Services.AddScoped<IValidator<Order>, OrderValidator>();
```

Or pass it explicitly:

```razor
<JeninnetValidator TModel="Order" Validator="orderValidator" />
```

## Field-level validation

```razor
<JeninnetValidator TModel="Order" ValidateOnFieldChanged="true" />
```

The default validates on submit/`EditContext.Validate()`.

## Async rules

Async-only validators need the asynchronous path. Use `EditForm.OnSubmit` instead of
`OnValidSubmit` so the form does not call the synchronous `EditContext.Validate()`:

```razor
<EditForm Model="order" OnSubmit="SubmitAsync">
    <JeninnetValidator @ref="validator"
                       TModel="Order"
                       UseAsyncValidation="true"
                       ValidateOnFieldChanged="true"
                       FieldChangedDebounce="TimeSpan.FromMilliseconds(300)" />

    <InputText @bind-Value="order.CustomerName" />
    <ValidationMessage For="() => order.CustomerName" />

    <button type="submit">Save</button>
</EditForm>

@code {
    private JeninnetValidator<Order>? validator;

    private async Task SubmitAsync(EditContext context)
    {
        if (validator is null || !await validator.ValidateAsync())
        {
            return;
        }

        await SaveOrderAsync();
    }
}
```

`ValidateAsync()` runs `MustAsync`/`CustomAsync` rules without blocking the render thread. A newer
validation cancels the previous one, and a stale result is discarded. The debounce is optional and
requires async mode.

## Avoid double validation

```razor
<JeninnetValidator TModel="Order" SkipIfModelStateValid="true" />
```

This skips validation while the edit context is already valid and the model has not changed since
the last successful validation. A field change makes it dirty again.

## Nested and collection mapping

The component maps member paths to `FieldIdentifier`:

- `Address.Street` → the `Address` object and `Street` field;
- `Lines[0].ProductName` → the first line object and `ProductName` field.

Arrays and `IList` are supported. Other collection shapes fall back to the root identifier.

## Verify

Submit an invalid nested model and confirm messages appear next to the nested inputs, not the root.
For async rules, confirm the submit handler runs only when `ValidateAsync()` returns `true`.

## Related

- [Blazor guide](../02-guides/11-blazor.md)
- [Collections and nesting](../02-guides/06-collections-and-nesting.md)
