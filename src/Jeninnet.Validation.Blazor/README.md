# Jeninnet.Validation.Blazor

Runs `Jeninnet.Validation` validators inside Blazor forms. The component
subscribes to `EditContext`, validates the model, and maps errors to
`FieldIdentifier` values so `ValidationMessage` components render next to the
right fields.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet add package Jeninnet.Validation.Blazor --version 1.0.0-preview.1
```

## Usage

```razor
<EditForm Model="order" OnValidSubmit="Submit">
    <JeninnetValidator TModel="Order" />

    <InputText @bind-Value="order.CustomerName" />
    <ValidationMessage For="() => order.CustomerName" />

    <InputText @bind-Value="order.Address.Street" />
    <ValidationMessage For="() => order.Address.Street" />
</EditForm>
```

The validator is resolved from DI:

```csharp
builder.Services.AddScoped<IValidator<Order>, OrderValidator>();
```

Or pass it explicitly:

```razor
<JeninnetValidator TModel="Order" Validator="orderValidator" />
```

## Behavior

- `EditContext.Validate()` (including `<EditForm>` submission) runs the
  validator and replaces the messages.
- Nested member paths (`Address.Street`) and collection paths
  (`Lines[0].ProductName`) resolve to the owning object and leaf field, so
  messages appear beside the correct input.
- `ValidateOnFieldChanged="true"` validates on every field change; the default
  is submit/`Validate()` only.
- A newer validation supersedes an in-flight one; a late result from the old
  validation is discarded.

## Async validation

Use `UseAsyncValidation="true"` with `EditForm.OnSubmit` when the validator
contains `MustAsync`/`CustomAsync` rules:

```razor
<EditForm Model="order" OnSubmit="SubmitAsync">
    <JeninnetValidator @ref="validator"
                       TModel="Order"
                       UseAsyncValidation="true" />

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

`EditContext.Validate()` is synchronous, so async mode deliberately
does not use `OnValidSubmit`; `Validate()` throws a guidance exception when
async mode is enabled. The component also exposes `PendingValidation` when
field-change validation is enabled, so a submit handler can await the pending
run before starting another validation.

### Field changes and debounce

```razor
<JeninnetValidator TModel="Order"
                   UseAsyncValidation="true"
                   ValidateOnFieldChanged="true"
                   FieldChangedDebounce="TimeSpan.FromMilliseconds(300)" />
```

`FieldChangedDebounce` is opt-in and requires async mode; no delay is applied by
default. Two rapid field changes restart the delay, and only the latest run is
applied.

### Double-validation toggle

Set `SkipIfModelStateValid="true"` to skip validation while the edit context is
already valid and the model has not changed since the last successful
validation. A field change marks the model dirty again, and a failed validation
always re-runs.

## Static SSR forms

Static SSR forms have no `EditContext`. Render the validation attributes from the descriptor and
load the browser module:

```razor
@{
    var descriptor = ((IValidationDescriptorProvider)Validator).Describe();
}

<form method="post" @formname="register" @attributes="JeninnetStaticSsrValidation.FormAttributes()">
    <AntiforgeryToken />
    <input name="Model.Name"
           @attributes="JeninnetStaticSsrValidation.For(descriptor, nameof(Registration.Name))" />
    <div @attributes="JeninnetStaticSsrValidation.MessageFor(nameof(Registration.Name))"></div>
</form>

<script type="module" src="/js/jeninnet-unobtrusive.js"></script>
```

`JeninnetStaticSsrValidation.For` returns a dictionary of `data-val-*` and `data-jeninnet-*`
attributes suitable for `@attributes`. The module reads `data-jeninnet-rules`, uses the shared
client-rules evaluator, prevents invalid submissions, and renders messages next to the fields.
Server-only rules never block the browser, and the server validates the posted model again. Without
JavaScript the form posts unchanged and the server remains authoritative.

The sample copies `client/src/jeninnet-unobtrusive.js`, `client-validator.js`, and `messages.js`
into `wwwroot/js`; update those copies when the client runtime changes.

## Limits

- `EditContext.OnValidationRequested` is synchronous. Async-only validators
  still throw `AsyncValidatorInvokedSynchronouslyException` on the synchronous
  path; use `ValidateAsync`.
- The component renders nothing; it only coordinates validation state.
- The resolver uses cached reflection to walk nested objects and `IList`
  collections. Blazor Server and WASM both work; this package is not an AOT
  target.
- Arrays and `IList` implementations are supported for indexed paths; other
  collection shapes fall back to the root model and the full path.

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
