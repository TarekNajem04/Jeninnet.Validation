# Customization Sample

Shows how to extend Jeninnet.Validation with application-specific rules while keeping the fluent
authoring model.

## What it demonstrates

- A custom synchronous rule (`StartsWithRule<T>`) that derives from `SyncValidateRule<T, TValue>`.
- A custom asynchronous rule (`UsernameAvailableRule<T>`) that simulates an external availability
  check.
- Fluent extensions (`StartsWith`, `UsernameAvailable`) that compose with built-in rules.
- Custom messages, codes, and severities on top of generated error paths.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Customization
```

The output validates an intentionally invalid `UserProfile`, then a valid profile, so both the
failure and success paths are visible.

## Key files

| File | Purpose |
|---|---|
| `Program.cs` | Map/endpoint setup and the validation walkthrough. |
| `CustomRules.cs` | Custom sync/async rule implementations and fluent extensions. |
| `UserProfileValidator` | Combines built-in and custom rules in one validator. |

## Related

- [Custom rules with context](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/06-custom-rules-with-context.md)
- [Fluent authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/01-fluent-authoring.md)
- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
