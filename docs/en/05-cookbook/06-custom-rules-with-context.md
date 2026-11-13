# Custom Rules with Shared Context

## Problem

A rule needs a database lookup, and several rules should reuse the same lookup instead of querying
per rule. Another rule emits multiple password-policy errors.

## Solution: `RootData` for one lookup

```csharp
builder.RuleFor(x => x.CustomerId, nameof(Order.CustomerId))
       .Custom((customerId, context, errors) =>
       {
           var customer = context.RootData!.GetOrAdd(
               "customer",
               () => LoadCustomer(customerId));

           if (customer.IsBlocked)
           {
               errors.Add(
                   context.GetFullPath(nameof(Order.CustomerId)),
                   new MessageId("Blocked"),
                   "Blocked",
                   null,
                   "This customer is blocked.",
                   "CUSTOMER_BLOCKED",
                   ValidationErrorSeverity.Error);
           }
       });
```

`RootData` is a per-walk bag, pooled per thread. It is shared by every rule in one `Validate*` call
and is not thread-safe (validation is sequential).

## Solution: `ServiceProvider`

The engine never resolves services itself; pass the provider explicitly:

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

## Solution: multi-error password policy

```csharp
builder.RuleFor(x => x.Password, nameof(User.Password))
       .Custom((password, context, errors) =>
       {
           if (password?.Any(char.IsDigit) is not true)
           {
               errors.Add(
                   context.GetFullPath(nameof(User.Password)),
                   new MessageId("Password"),
                   "Password",
                   null,
                   "Password needs a digit.",
                   "PWD_DIGIT",
                   ValidationErrorSeverity.Error);
           }

           if (password?.Any(char.IsUpper) is not true)
           {
               errors.Add(
                   context.GetFullPath(nameof(User.Password)),
                   new MessageId("Password"),
                   "Password",
                   null,
                   "Password needs an uppercase letter.",
                   "PWD_UPPER",
                   ValidationErrorSeverity.Error);
           }
       });
```

## Async variant

```csharp
builder.RuleFor(x => x.Email, nameof(User.Email))
       .CustomAsync(async (email, context, errors, cancellationToken) =>
       {
           if (!await IsUniqueAsync(email, cancellationToken))
           {
               errors.Add(
                   context.GetFullPath(nameof(User.Email)),
                   new MessageId("Unique"),
                   "Unique",
                   null,
                   "Email is already registered.",
                   "EMAIL_TAKEN",
                   ValidationErrorSeverity.Error);
           }
       });
```

`CustomAsync` runs only on the async path and honors cancellation. An exception thrown by the
delegate becomes a `MessageId.Invalid` failure with code `CustomValidationException`.

## Verify

Validate a model with a blocked customer and a weak password; confirm both the blocked error and
both password errors appear in one result, and that `RootData` performed one lookup.

## Related

- [Custom rules guide](../02-guides/07-custom-rules.md)
- [Conditions and dependent rules](../02-guides/05-conditions-and-dependent-rules.md)
