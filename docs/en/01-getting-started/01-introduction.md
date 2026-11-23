# Introduction

Jeninnet.Validation is a validation framework for .NET 10 and .NET 11 applications. It gives you two
authoring styles over one execution engine:

- **Fluent authoring** — derive from `Validator<T>` and declare rules with `RuleFor`.
- **Attribute authoring** — annotate a model with `[Validate]`, `[NotEmpty]`, `[Length]`, and let
  the Roslyn source generator emit the validator.

Both styles produce the same `IValidator<T>` contract, the same `ValidationResult`, and the same
error model. The engine is deterministic, allocation-free on the happy path, and designed for
trimming and Native AOT.

## What the library gives you

- A complete rule surface: presence, comparison, string shape, date/time, collections, nesting,
  property comparison, and custom predicates.
- Rule sets, per-rule conditions, dependent rules, cascade modes, and fail-fast.
- Lifecycle hooks (`PreValidate`, `OnFailure`) and a shared validation context.
- Frozen validator descriptors that power JSON Schema, OpenAPI, and client-rule export.
- Optional integrations: ASP.NET Core, MediatR, DataAnnotations, and Blazor.
- Built-in message packs for 40 cultures through the sibling localization platform.

## What it does not do

- It does not rewrite C# expressions into JavaScript. Client rules are data, and the server remains
  authoritative.
- It does not replace the host's model binding or built-in validation pipeline; it plugs into them.
- It does not provide a form builder or UI controls.

## Project status

The current preview in preparation is `1.0.0-preview.3`; the latest published preview is
`1.0.0-preview.2`. The public surface can still change before `1.0.0`.
See [Compatibility and versions](../03-reference/06-compatibility-and-versions.md) and the
[roadmap](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/ROADMAP.md).

## Next

- [Installation](02-installation.md)
- [Quick start](03-quick-start.md)
