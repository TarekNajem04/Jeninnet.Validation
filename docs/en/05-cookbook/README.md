# Cookbook

Task-oriented recipes for common application scenarios. Each recipe is self-contained and links to
the deeper guide for the underlying concept.

## Recipes

1. [Validate a minimal API request](01-minimal-api.md)
2. [Validate an MVC controller](02-mvc.md)
3. [Validate a MediatR command](03-mediatr.md)
4. [Validate a Blazor form](04-blazor.md)
5. [Rule sets and partial updates](05-rule-sets-and-partial-updates.md)
6. [Custom rules with shared context](06-custom-rules-with-context.md)
7. [Localized validation messages](07-localized-messages.md)
8. [Test validators](08-testing-validators.md)
9. [Emit JSON Schema, OpenAPI, and client rules](09-schema-openapi-client.md)
10. [Validate a desktop view model](10-inotify-data-error-info.md)

## Conventions

- Examples target .NET 10/11 and the preview package `1.0.0-preview.1`.
- Register validators as `IValidator<T>` (scoped or singleton).
- Use the async path when the validator has async rules.
