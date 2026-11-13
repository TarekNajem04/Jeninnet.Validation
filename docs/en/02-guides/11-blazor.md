# Blazor

The `Jeninnet.Validation.Blazor` package runs Jeninnet validators inside Blazor forms and maps
errors to `FieldIdentifier` values.

## Usage

```razor
@using Jeninnet.Validation.Blazor

<EditForm Model="order" OnValidSubmit="Submit">
    <JeninnetValidator TModel="Order" />

    <InputText @bind-Value="order.CustomerName" />
    <ValidationMessage For="() => order.CustomerName" />

    <InputText @bind-Value="order.Address.Street" />
    <ValidationMessage For="() => order.Address.Street" />
</EditForm>
```

The component resolves `IValidator<TModel>` from DI:

```csharp
builder.Services.AddScoped<IValidator<Order>, OrderValidator>();
```

Or pass it explicitly:

```razor
<JeninnetValidator TModel="Order" Validator="orderValidator" />
```

## Behavior

- `EditContext.Validate()` and form submission run the validator and replace the messages.
- Nested paths (`Address.Street`) and collection paths (`Lines[0].ProductName`) resolve to the
  owning object and leaf field.
- `ValidateOnFieldChanged="true"` validates on every field change; the default is submit/`Validate()`.
- A newer validation supersedes an in-flight one; a late result from the old validation is
  discarded.
- Missing validators are a no-op.

## Async validation

`EditContext.OnValidationRequested` is synchronous, so async rules use an explicit
`ValidateAsync()` call from `EditForm.OnSubmit`:

```razor
@using Jeninnet.Validation.Blazor

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

`ValidateAsync()` runs `IAsyncValidator<TModel>.ValidateAsync`, including `MustAsync` and
`CustomAsync` rules, and returns `true` when the model is valid. It does not block the render
thread. When async mode is enabled, calling `EditContext.Validate()` throws a guidance exception
instead of silently running the synchronous path.

### Field changes and debounce

```razor
<JeninnetValidator TModel="Order"
                   UseAsyncValidation="true"
                   ValidateOnFieldChanged="true"
                   FieldChangedDebounce="TimeSpan.FromMilliseconds(300)" />
```

The debounce is opt-in and requires async mode. Rapid field changes cancel the previous delay and
only the latest validation is applied. Without a debounce, every field change starts an async
validation and cancels the previous one.

When field-change validation is enabled, `JeninnetValidator.PendingValidation` exposes the pending
run so a submit handler can await it before starting another validation.

### Double-validation toggle

`SkipIfModelStateValid="true"` skips validation while the edit context is already valid and the
model has not changed since the last successful validation. A field change marks the model dirty,
and a failed validation always re-runs. Use it when a successful explicit `ValidateAsync()` call
would otherwise be followed by a second validation pass.

## Static SSR forms

Interactive `EditContext` validation does not run in static SSR; the form posts directly to the
server. Use `JeninnetStaticSsrValidation` to render standard `data-val-*` attributes and a
`data-jeninnet-rules` payload, then load the dependency-free module:

```razor
@inject IValidator<Registration> Validator

@{
    var descriptor = ((IValidationDescriptorProvider)Validator).Describe();
}

<form method="post" @formname="register" @attributes="JeninnetStaticSsrValidation.FormAttributes()">
    <AntiforgeryToken />
    <input name="Model.Name" value="@Model?.Name"
           @attributes="JeninnetStaticSsrValidation.For(descriptor, nameof(Registration.Name))" />
    <div @attributes="JeninnetStaticSsrValidation.MessageFor(nameof(Registration.Name))"></div>
    <button type="submit">Register</button>
</form>

<script type="module" src="/js/jeninnet-unobtrusive.js"></script>
```

The helper emits `data-val`, `data-val-required`, `data-val-length`, `data-val-range`,
`data-val-regex`, `data-val-email`, and `data-val-url` where the rules map cleanly. All member
rules, including additional ones such as `Count` and `FileExtensions`, travel in the JSON payload
and are evaluated by the existing client runtime. The module validates on submit, prevents invalid
submissions, renders messages in the `data-jeninnet-message-for` element, and clears them on input.
It has no jQuery or other runtime dependency.

Rules marked `serverOnly` (conditions, services, async rules, relative dates, and any rule the
browser runtime does not implement) are not evaluated in the browser and never block submission;
the server remains authoritative. Without JavaScript the form still posts and the server validates
the bound model normally.

The sample at `samples/Jeninnet.Validation.Sample.Mvc` exposes `/static-ssr` and copies the module
from `client/src`; keep those copies in sync when updating the browser runtime.

## Limits

- The component is synchronous by default because `EditContext.OnValidationRequested` is
  synchronous. Async-only validators throw `AsyncValidatorInvokedSynchronouslyException` on that
  path; enable async mode and call `ValidateAsync()` instead.
- The resolver uses cached reflection to walk nested objects and `IList` collections. Arrays and
  `IList` implementations are supported; other collection shapes fall back to the root identifier.
- The component renders nothing; it only coordinates validation state.
- `EditContext.ValidateAsync` is available on .NET 11; on .NET 10 this package's explicit
  `ValidateAsync` path is the supported async form route.

## Verify

Submit an invalid nested model and confirm the messages appear next to the nested inputs, not the
root. For async rules, use `OnSubmit` and confirm the handler runs only when `ValidateAsync()`
returns `true`.

## Related

- [DataAnnotations](10-dataannotations.md)
- [Integration packages](../03-reference/05-integration-packages.md)
