# Jeninnet.Validation.Migration

Migration assistant for `Jeninnet.Validation`. It scans C# sources for FluentValidation and
DataAnnotations patterns, reports what converts cleanly and what needs a human, and applies
safe, idempotent codemods. Anything it does not understand is left untouched with an explicit
`// TODO(Jeninnet):` marker and a report entry — never a silent semantic change.

## Requirements

- .NET 11.0 or later.
- C# 15 when consuming the source contracts.

## Installation

```bash
dotnet tool install --global Jeninnet.Validation.Migration --version 1.0.0-preview.1
```

## Analyze

```bash
jeninnet migrate analyze ./src --format markdown --output migration-report.md
jeninnet migrate analyze ./src --format json --output migration-report.json
```

The report lists convertible validators and rules, attributes that convert with review notes,
unsupported patterns, and follow-ups (for example removing FluentValidation package references).
It also prints a **migration readiness score** — the share of findings the codemod can apply
without human review — and how many files are fully convertible. `analyze` always exits `0`; add
`--check` to fail when any item is still pending (CI gate).

## Apply

```bash
jeninnet migrate apply ./src --dry-run   # prints what would change, writes nothing
jeninnet migrate apply ./src             # rewrites files in place
jeninnet migrate apply ./src --check     # exits 1 when files would change, writes nothing
```

Safe conversions include `AbstractValidator<T>` → `Validator<T>` with `Configure`,
single-argument `RuleFor(x => x.Name)` → `RuleFor(x => x.Name, nameof(T.Name))`,
`.WithErrorCode(` → `.WithCode(`, and `[Required]` → `[NotNull]` with a review TODO.
Constructors with parameters, `Custom(`, `Transform(`, and other unknown patterns are left
in place and reported. Running `apply` twice changes nothing the second time.

## Exit codes

- `0` — success, or (without `--check`) nothing left to report as failure.
- `1` — with `--check`, files would change or review items remain; also I/O errors.
- `2` — usage error (unknown command, missing path, bad flag).

## Scope

Attribute-driven and fluent FluentValidation validators plus `[Required]` attributes. The tool
matches FluentValidation API names syntactically and says so in the report; it does not resolve
project references. Runtime shims are out of scope: migrate the code, then delete the old
dependency. See the [migration guides](https://github.com/TarekNajem04/Jeninnet.Validation/tree/main/docs/en/02-guides/15-migration-from-fluentvalidation.md).

## Related packages

- [Jeninnet.Validation](https://www.nuget.org/packages/Jeninnet.Validation) — core engine and fluent API.
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md) — every package and its role.

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
