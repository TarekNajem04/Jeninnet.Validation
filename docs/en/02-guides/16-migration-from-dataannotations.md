# Migration from DataAnnotations

Jeninnet.Validation has its own attribute set. You can migrate attribute-by-attribute, or keep the
DataAnnotations pipeline and run Jeninnet validators through the bridge.

## Attribute mapping

| DataAnnotations | Jeninnet.Validation |
|---|---|
| `[Required]` | `[NotNull]` or `[NotEmpty]` |
| `[StringLength(max)]` | `[Length(0, max)]` |
| `[MinLength(n)]` | `[MinimumLength(n)]` |
| `[MaxLength(n)]` | `[MaximumLength(n)]` |
| `[Range(min, max)]` | `[InclusiveBetween(min, max)]` |
| `[EmailAddress]` | `[Email]` |
| `[Phone]` | `[Phone]` |
| `[Url]` | `[Url]` |
| `[RegularExpression(pattern)]` | `[Regex(pattern)]` |
| `[Compare(nameof(Other))]` | `[PropertyEqual(nameof(Other))]` |
| `[CreditCard]` | `[CreditCard]` |
| `[CustomValidation]` | Fluent `Must` / `Custom` (no attribute equivalent) |

Add `[Validate]` to the model so the generator emits the validator.

`[NotNull]`, `[Enum]`, `[In]`, `[NotIn]`, and `[ScalePrecision]` live in
`Jeninnet.Validation.Attributes.Common`; `[Phone]`, `[Url]`, and `[IPAddress]` live in
`Jeninnet.Validation.Attributes.String`. Add the matching `using` directive.

## Bridge instead of rewrite

Keep the existing DataAnnotations pipeline and annotate the model:

```csharp
using Jeninnet.Validation.DataAnnotations;

[JeninnetValidatable(typeof(CreateOrderValidator))]
public sealed class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

MVC model binding, Blazor’s `DataAnnotationsValidator`, and
`System.ComponentModel.DataAnnotations.Validator` run the Jeninnet validator. Register it when it has
dependencies:

```csharp
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderValidator>();
```

## `IValidatableObject`

```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
{
    var result = JeninnetDataAnnotationsAdapter.Validate(
        this,
        typeof(Order),
        typeof(OrderValidator),
        validationContext.GetService);

    return JeninnetDataAnnotationsAdapter.ToValidationResults(result);
}
```

## Differences

- The bridge is synchronous; async rules belong on the ASP.NET Core or MediatR async paths.
- One DataAnnotations `ValidationResult` carries one message; multiple errors are joined.
- Severity and error codes are optional in the generated message.
- The bridge is the only package that references `System.ComponentModel.DataAnnotations`.

## Migration assistant (dotnet tool)

The optional `jeninnet` tool scans C# sources and converts DataAnnotations `[Required]` to
Jeninnet `[NotNull]` with an explicit review marker. Because `[Required]` rejects empty strings while
`[NotNull]` does not, every conversion carries a `// TODO(Jeninnet):` note and a report
entry: switch to `[NotEmpty]` wherever empty strings must fail (for example when
`AllowEmptyStrings` is `false`).

```bash
dotnet tool install --global Jeninnet.Validation.Migration --version 1.0.0-preview.1

# Report only; exits 0. Add --format json for machine-readable output.
jeninnet migrate analyze ./src --output migration-report.md

# Preview the codemod without writing files; --check exits 1 when files would change.
jeninnet migrate apply ./src --dry-run

# Rewrite files in place (idempotent: a second run changes nothing).
jeninnet migrate apply ./src
```

Use `jeninnet migrate analyze ./src --check` in CI to fail while review items remain.
Exit codes: `0` success, `1` changes pending or error, `2` usage error.

## Verify

Submit the model through the existing pipeline and confirm the errors appear next to the fields.
Then add a Jeninnet-only rule (for example a rule set or async rule on the ASP.NET Core path) to
confirm the migration path.

## Related

- [DataAnnotations](10-dataannotations.md)
- [Attribute authoring](02-attribute-authoring.md)
