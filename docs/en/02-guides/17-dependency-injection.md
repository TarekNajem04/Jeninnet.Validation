# Dependency Injection and Lifetimes

Validators are ordinary objects. DI is optional for standalone use, but every host integration
resolves `IValidator<T>` from a container. This guide covers registration, lifetimes, composition,
and the AOT-safe path.

## Register explicitly

```csharp
using Jeninnet.Validation.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddScoped<CustomerValidator>();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

Register both the concrete type and the interface when the validator has constructor dependencies
or when another validator injects it directly.

## Register with one call (generated catalog)

The generator emits a module initializer in every assembly that declares `[Validate]` types. It
registers the validator/model pairs in a central catalog using `typeof` only; no reflection and no
assembly scanning:

```csharp
using Jeninnet.Validation.Catalog;

services.AddValidation();
```

`AddValidation()` registers the infrastructure and every catalog entry. The default lifetime is
scoped; pass `ServiceLifetime.Singleton` or `Transient` when the validator graph has no scoped
dependencies. An assembly contributes when it loads, so make sure a type from the validator
assembly is referenced before this call (normal application startup does this).

`AddValidation()` also registers the message pipeline (`IMessageLocalizer`, formatter, provider),
but it never replaces the host's `IStringLocalizerFactory` and does not register
`LocalizationOptions`. Store-backed `IStringLocalizer` support is an explicit opt-in: register an
`ILocalizationStore` and call `JeninnetLocalizationExtensions.AddLocalization(services)` (the
configuring overload also sets `LocalizationOptions`). `AddLocalization` and `AddValidation` can be
called in either order. For resx, use the `AddJeninnetStringLocalizer` bridge instead.

If `Microsoft.Extensions.Validation` is imported as well, both packages expose an `AddValidation`
extension; qualify the call or use the fully qualified type name.

## Register generated validators without reflection (per assembly)

The generator also emits an assembly-local registry. For a model in the current assembly:

```csharp
using MyApp.GeneratedValidation;

services.AddGeneratedValidatorsFromThisAssembly();
```

The generated namespace is `<assembly name>.GeneratedValidation`. The registry registers every
`[Validate]` type as its concrete validator plus `IValidator<T>`, `ISyncValidator<T>`, and
`IAsyncValidator<T>`. Registration is scoped by default and uses `TryAdd`, so it is idempotent.
Pass a different `ServiceLifetime` only when the validator graph has no scoped dependencies.

The registry calls `AddValidationCore()` automatically. Use it directly when registering validators
explicitly in an assembly with no generated validators:

```csharp
services.AddValidationCore();
services.AddScoped<IValidator<Customer>, CustomerValidator>();
```

This path is safe for trimming and Native AOT; it performs no assembly scanning.

## Register generated validators by scanning

Assembly scanning remains available as a fallback:

```csharp
services.AddGeneratedValidators(typeof(Program).Assembly);
```

The no-argument `AddGeneratedValidators()` reads the generated catalog first and only falls back
to scanning when the catalog is empty. The scanner registers every `IGeneratedValidator` as scoped,
plus its `IValidator<T>`, `ISyncValidator<T>`, and `IAsyncValidator<T>` interfaces. It is annotated
`[RequiresUnreferencedCode]`; do not use it in trimmed or Native AOT deployments.

The `Type[]` overload registers only the supplied types, but still inspects interfaces at runtime
and is therefore also not AOT-safe:

```csharp
services.AddGeneratedValidators(typeof(CustomerValidator), typeof(OrderValidator));
```

For AOT, register explicitly.

## Lifetimes

| Registration | Lifetime | When |
|---|---|---|
| `AddScoped<IValidator<T>, TValidator>()` | Scoped | Default for request-scoped hosts; safe for validators with scoped dependencies. |
| `AddSingleton<IValidator<T>, TValidator>()` | Singleton | Validators with no scoped dependencies; best throughput. |
| `AddTransient<IValidator<T>, TValidator>()` | Transient | Rare; only when the validator itself is stateful per resolution. |
| `AddGeneratedValidatorsFromThisAssembly()` | Scoped | Generated validators; reflection-free and AOT-safe. |
| `AddGeneratedValidators(...)` | Scoped | Compatibility scanning; reflection-based and not AOT-safe. |

Validators are stateless after `Configure` runs. Rules are built once, descriptors are frozen, and
validation uses pooled per-thread state. A configured validator is safe to reuse across threads.

Do not capture a scoped service in a singleton validator. If a rule needs a scoped service, pass it
through `ValidationCallOptions.ServiceProvider` or resolve it inside the host adapter.

The provider flows through nested, collection, and included validators via
`IValidationContextValidator<T>`, so a custom rule in a child validator can read
`context.ServiceProvider` without a service-locator call.

## Compose with constructor injection

```csharp
public sealed class OrderValidator : Validator<Order>
{
    private readonly IValidator<Address> _addressValidator;

    public OrderValidator(IValidator<Address> addressValidator)
    {
        _addressValidator = addressValidator;
    }

    protected override void Configure(ValidatorRuleBuilder<Order> builder)
    {
        builder.RuleFor(x => x.ShippingAddress, nameof(Order.ShippingAddress))
               .SetValidator(_addressValidator);
    }
}
```

Register the dependency first:

```csharp
services.AddScoped<IValidator<Address>, AddressValidator>();
services.AddScoped<IValidator<Order>, OrderValidator>();
```

`SetValidator` skips a null child; combine it with `NotNull` when the child is required.

## Compose without DI

`Include`, `SetValidator`, and `ChildRules` are plain object composition:

```csharp
builder.Include(new CustomerDefaultsValidator());

builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .SetValidator(new AddressValidator());

builder.RuleFor(x => x.Address, nameof(Customer.Address))
       .ChildRules(address =>
       {
           address.RuleFor(a => a.Street, nameof(Address.Street)).NotEmpty();
       });
```

This is the AOT-safe way to compose validators.

## Services inside rules

The engine never resolves services itself. Supply a provider per call:

```csharp
var result = validator.Validate(order, new ValidationCallOptions
{
    ServiceProvider = serviceProvider
});
```

```csharp
builder.RuleFor(x => x.Code, nameof(Order.Code))
       .Custom((code, context, errors) =>
       {
           var lookup = context.ServiceProvider?.GetService<ICodeLookup>();
           // ...
       });
```

Host adapters do not set `ServiceProvider` automatically. If a rule needs scoped services, either
inject them into the validator (preferred) or build the call options in your adapter.

## Console and worker hosts

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddScoped<IValidator<Job>, JobValidator>();

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var validator = scope.ServiceProvider.GetRequiredService<IValidator<Job>>();
var result = validator.Validate(job);
```

Use a scope per unit of work; do not resolve scoped validators from the root provider.

## Options validation

`Jeninnet.Validation.Extensions.Validation` can validate `Microsoft.Extensions.Options` at startup.
Register the validator as `IValidator<T>` first, then call `AddValidatedOptions<T>()`:

```csharp
using Jeninnet.Validation.Extensions.Validation;

builder.Services.AddScoped<IValidator<WorkerOptions>, WorkerOptionsValidator>();

builder.Services.AddOptions<WorkerOptions>()
                .BindConfiguration("Worker")
                .AddValidatedOptions();
```

The call registers `IValidateOptions<T>` and enables `ValidateOnStart()`. Invalid configuration
throws `OptionsValidationException` during startup; each failure reads
`name: Member [code] message`, and named options include their name as the prefix. Only errors fail
validation — warnings are ignored. Missing validator registrations produce an explicit failure
message, and scoped validators are resolved from a dedicated scope.

Options validation is synchronous in `1.0.x`. Async-only validators throw
`AsyncValidatorInvokedSynchronouslyException`; the .NET 11 async path (`IAsyncStartupValidator`)
is planned for `1.1.0-preview.1`.

## Testing with DI

Resolve the validator from the same container configuration the application uses. This catches
missing registrations that a `new CustomerValidator()` test would miss:

```csharp
using Microsoft.Extensions.DependencyInjection;

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var validator = scope.ServiceProvider.GetRequiredService<IValidator<Customer>>();
var result = validator.Validate(customer);

result.ShouldNotHaveValidationErrors();
```

## Common mistakes

| Symptom | Cause | Fix |
|---|---|---|
| Validator never runs | Registered only as a concrete type; host resolves `IValidator<T>` | Add `AddScoped<IValidator<T>, TValidator>()`. |
| `InvalidOperationException` from DataAnnotations | Validator cannot be resolved or constructed | Register `IValidator<T>` or give the validator a parameterless constructor. |
| Trim/AOT failure | `AddGeneratedValidators` uses reflection | Use the generated registry or register explicitly. |
| Scoped service disposed during validation | Singleton validator captured a scoped dependency | Match lifetimes or pass the provider per call. |
| Duplicate validation | Both the host filter and manual code run the validator | Validate once at the boundary. |

## Related

- [Installation](../01-getting-started/02-installation.md)
- [AOT and trimming](../04-operations/02-aot-and-trimming.md)
- [Collections and nesting](06-collections-and-nesting.md)
- [Custom rules](07-custom-rules.md)
