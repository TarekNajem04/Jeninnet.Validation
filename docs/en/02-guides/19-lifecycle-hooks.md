# Lifecycle Hooks

Fluent validators can run code before and after rule evaluation. Hooks are registered inside
`Configure` and are useful for loading context, short-circuiting, auditing, or shaping a failure.

## Hook overview

| Hook | Path | When it runs | Return value |
|---|---|---|---|
| `PreValidate(Func<ValidationContext<T>, bool>)` | Sync and async | Before any rule. | `false` short-circuits with `ValidationResult.Success`. |
| `PreValidateAsync(Func<ValidationContext<T>, CancellationToken, ValueTask<bool>>)` | Async only | After `PreValidate`, before any rule. | `false` short-circuits with success. |
| `OnFailure(Action<ValidationResult>)` | Sync and async | Once after rules, only when invalid. | — |
| `OnFailureAsync(Func<ValidationResult, CancellationToken, ValueTask>)` | Async only | After `OnFailure`, only when invalid. | — |

A second registration of the same hook replaces the first.

## Declarative hooks on generated validators

Attribute-driven validators can bind static methods on the model with `[PreValidate]` and
`[OnFailure]`:

```csharp
using Jeninnet.Validation;
using Jeninnet.Validation.Attributes;
using Jeninnet.Validation.Core;

[Validate]
[PreValidate(nameof(BeforeValidate))]
[OnFailure(nameof(AfterFailure))]
public sealed class Order
{
    [NotEmpty] public string? CustomerName { get; set; }

    public static bool BeforeValidate(
        Order instance,
        ValidationContext<Order> context,
        ValidationErrorCollector errors) {
        if (instance.CustomerName == "blocked") {
            errors.Add(nameof(CustomerName), "blocked");
            return false;
        }

        return true;
    }

    public static void AfterFailure(ValidationResult result) {
        // audit, metrics, or a domain event
    }
}
```

- `PreValidate` is static and returns `bool`; it accepts `(T, ValidationContext<T>)` or
  `(T, ValidationContext<T>, ValidationErrorCollector)`.
- `OnFailure` is static `void(ValidationResult)` and runs once after the merged result is known to be
  invalid.
- Errors added through the collector are merged with rule errors. Returning `false` stops rule
  evaluation; the collected errors still make the result invalid.
- Generated validators call these hooks directly; no reflection is used. Declarative hooks have no
  async variants—use fluent `PreValidateAsync`/`OnFailureAsync` for async work.

## Pre-validation

```csharp
public sealed class OrderValidator : Validator<Order>
{
    protected override void Configure(ValidatorRuleBuilder<Order> builder)
    {
        PreValidate(context =>
        {
            if (context.Instance.IsDeleted)
            {
                return false; // deleted orders are not validated
            }

            return true;
        });

        builder.RuleFor(x => x.CustomerName, nameof(Order.CustomerName)).NotEmpty();
    }
}
```

When `PreValidate` returns `false`, the result is `ValidationResult.Success` and failure hooks do
not run. This is a deliberate “nothing to validate” signal, not an error.

The synchronous `PreValidate` runs on both paths. The async path runs it first, then
`PreValidateAsync`:

```csharp
PreValidateAsync(async (context, cancellationToken) =>
{
    var tenant = await LoadTenantAsync(context.Instance.TenantId, cancellationToken);
    return tenant.IsActive;
});
```

If `PreValidateAsync` is configured, calling synchronous `Validate` throws
`InvalidOperationException` with a message telling you to call `ValidateAsync`. This is a different
exception from `AsyncValidatorInvokedSynchronouslyException`, which is about async rules.

## Failure hooks

```csharp
OnFailure(result =>
{
    _audit.RecordFailure(result.Errors);
});

OnFailureAsync(async (result, cancellationToken) =>
{
    await _metrics.RecordAsync(result.Errors.Count, cancellationToken);
});
```

- `OnFailure` runs on both paths, after all errors are collected.
- `OnFailureAsync` runs only on the async path, after `OnFailure`.
- Neither runs for a valid result or a short-circuited pre-validation.
- `ValidateAndThrow` invokes the hooks before throwing.

## Context available to hooks

`PreValidate` receives a `ValidationContext<T>` with:

- `Instance` — the root instance;
- `MemberPath` — empty at the root;
- `RuleSets` and `FailFast` — the current call settings;
- `Root`, `RootData`, and `ServiceProvider` — **not populated** for hooks.

If a pre-validation hook needs a service, inject it into the validator or perform the check inside a
custom rule where `context.ServiceProvider` is available.

## Exceptions

A hook exception is wrapped in `ValidationException` with the original exception as `InnerException`:

```csharp
try
{
    validator.Validate(order);
}
catch (ValidationException exception) when (exception.InnerException is not null)
{
    // A PreValidate or OnFailure hook failed.
}
```

`OperationCanceledException` is not wrapped; cancellation propagates to the caller.

## Ordering summary

```text
sync path:   PreValidate -> rules -> OnFailure (if invalid)
async path:  PreValidate -> PreValidateAsync -> sync rules -> async rules
             -> OnFailure (if invalid) -> OnFailureAsync (if invalid)
```

Under `FailFast` or class-level `Stop`, a sync-phase failure skips the async phase; the failure hooks
still run for the errors that were collected.

## When to use hooks

- **PreValidate** — skip validation for deleted, draft, or tenant-disabled entities; load cheap
  context that affects every rule.
- **PreValidateAsync** — load tenant/feature state from a service before validation.
- **OnFailure** — audit, metrics, or domain events; do not mutate the model.
- **OnFailureAsync** — async telemetry or notification after a failed validation.

For fluent validators, do not use hooks to add validation errors; use rules or `Custom` for that.
The declarative `PreValidate` overloads that accept a `ValidationErrorCollector` are the exception:
they exist to reject a request before rule evaluation with a precise member error.

## Related

- [Fluent authoring](01-fluent-authoring.md)
- [Custom rules](07-custom-rules.md)
- [Dependency injection and lifetimes](17-dependency-injection.md)
- [Error handling](18-error-handling.md)
