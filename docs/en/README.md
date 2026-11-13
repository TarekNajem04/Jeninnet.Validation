# The Jeninnet.Validation Book

This book explains Jeninnet.Validation from the first package reference to production operation.
It is written for application developers, library authors, and teams migrating from another
validation stack.

## Reading paths

**New to the library**

1. [Best C# validation libraries in 2026](01-getting-started/00b-best-csharp-validation-libraries.md)
2. [Why Jeninnet?](01-getting-started/00-why-jeninnet.md)
3. [Introduction](01-getting-started/01-introduction.md)
4. [Installation](01-getting-started/02-installation.md)
5. [Quick start](01-getting-started/03-quick-start.md)
6. [Mental model](01-getting-started/04-mental-model.md)
7. [Fluent authoring](02-guides/01-fluent-authoring.md) or
   [attribute authoring](02-guides/02-attribute-authoring.md)

**Integrating with a host**

- [ASP.NET Core](02-guides/08-aspnet-core.md)
- [MediatR](02-guides/09-mediatr.md)
- [DataAnnotations](02-guides/10-dataannotations.md)
- [Blazor](02-guides/11-blazor.md)
- [INotifyDataErrorInfo (WPF, MAUI, Avalonia)](02-guides/21-inotify-data-error-info.md)
- [HotChocolate GraphQL](02-guides/22-graphql.md)
- [gRPC](02-guides/23-grpc.md)
- [JSON Schema, OpenAPI, and client rules](02-guides/12-schema-openapi-client.md)
- [Dependency injection and lifetimes](02-guides/17-dependency-injection.md)
- [Error handling](02-guides/18-error-handling.md)
- [Lifecycle hooks](02-guides/19-lifecycle-hooks.md)

**Migrating**

- [From FluentValidation](02-guides/15-migration-from-fluentvalidation.md)
- [From DataAnnotations](02-guides/16-migration-from-dataannotations.md)

**Operating**

- [Performance](04-operations/01-performance.md)
- [AOT and trimming](04-operations/02-aot-and-trimming.md)
- [Coverage and quality](04-operations/03-coverage-and-quality.md)
- [Troubleshooting](04-operations/04-troubleshooting.md)
- [Production checklist](04-operations/08-production-checklist.md)
- [Security and data exposure](04-operations/09-security.md)
- [Swashbuckle 11 and .NET 11 migration](04-operations/11-swashbuckle-11-migration.md)

## Table of contents

### Part 1 — Getting started

1. [Best C# validation libraries in 2026](01-getting-started/00b-best-csharp-validation-libraries.md)
2. [Why Jeninnet?](01-getting-started/00-why-jeninnet.md)
3. [Introduction](01-getting-started/01-introduction.md)
4. [Installation](01-getting-started/02-installation.md)
5. [Quick start](01-getting-started/03-quick-start.md)
6. [Mental model](01-getting-started/04-mental-model.md)

### Part 2 — Guides

1. [Fluent authoring](02-guides/01-fluent-authoring.md)
2. [Attribute authoring](02-guides/02-attribute-authoring.md)
3. [Rules and messages](02-guides/03-rules-and-messages.md)
4. [Rule sets and cascade](02-guides/04-rule-sets-and-cascade.md)
5. [Conditions and dependent rules](02-guides/05-conditions-and-dependent-rules.md)
6. [Collections and nesting](02-guides/06-collections-and-nesting.md)
7. [Custom rules](02-guides/07-custom-rules.md)
8. [ASP.NET Core](02-guides/08-aspnet-core.md)
9. [MediatR](02-guides/09-mediatr.md)
10. [DataAnnotations](02-guides/10-dataannotations.md)
11. [Blazor](02-guides/11-blazor.md)
12. [JSON Schema, OpenAPI, and client rules](02-guides/12-schema-openapi-client.md)
13. [Localization](02-guides/13-localization.md)
14. [Testing](02-guides/14-testing.md)
15. [Migration from FluentValidation](02-guides/15-migration-from-fluentvalidation.md)
16. [Migration from DataAnnotations](02-guides/16-migration-from-dataannotations.md)
17. [Dependency injection and lifetimes](02-guides/17-dependency-injection.md)
18. [Error handling](02-guides/18-error-handling.md)
19. [Lifecycle hooks](02-guides/19-lifecycle-hooks.md)
20. [Inheritance and polymorphism](02-guides/20-inheritance-and-polymorphism.md)
21. [INotifyDataErrorInfo (WPF, MAUI, Avalonia)](02-guides/21-inotify-data-error-info.md)
22. [HotChocolate GraphQL](02-guides/22-graphql.md)
23. [gRPC](02-guides/23-grpc.md)

### Part 3 — Cookbook

1. [Minimal API request](05-cookbook/01-minimal-api.md)
2. [MVC controller](05-cookbook/02-mvc.md)
3. [MediatR command](05-cookbook/03-mediatr.md)
4. [Blazor form](05-cookbook/04-blazor.md)
5. [Rule sets and partial updates](05-cookbook/05-rule-sets-and-partial-updates.md)
6. [Custom rules with context](05-cookbook/06-custom-rules-with-context.md)
7. [Localized messages](05-cookbook/07-localized-messages.md)
8. [Testing validators](05-cookbook/08-testing-validators.md)
9. [Schema, OpenAPI, and client rules](05-cookbook/09-schema-openapi-client.md)
10. [Desktop view model](05-cookbook/10-inotify-data-error-info.md)

### Part 4 — Reference

1. [Architecture](03-reference/00-architecture.md)
2. [Core API](03-reference/01-core-api.md)
3. [Rule reference](03-reference/02-rule-reference.md)
4. [Diagnostics](03-reference/03-diagnostics.md)
5. [Metadata](03-reference/04-metadata.md)
6. [Integration packages](03-reference/05-integration-packages.md)
7. [Compatibility and versions](03-reference/06-compatibility-and-versions.md)
8. [Known limitations](03-reference/07-known-limitations.md)
9. [API reference](03-reference/08-api-reference.md)
10. [Capability parity](03-reference/09-capability-parity.md)

### Part 5 — Operations

1. [Performance](04-operations/01-performance.md)
2. [AOT and trimming](04-operations/02-aot-and-trimming.md)
3. [Coverage and quality](04-operations/03-coverage-and-quality.md)
4. [Troubleshooting](04-operations/04-troubleshooting.md)
5. [FAQ](04-operations/05-faq.md)
6. [Benchmarks](04-operations/06-benchmarks.md)
7. [Allocation audit](04-operations/07-allocation-audit.md)
8. [Production checklist](04-operations/08-production-checklist.md)
9. [Security and data exposure](04-operations/09-security.md)
10. [Peer benchmarks](04-operations/10-peer-benchmarks.md)
11. [Swashbuckle 11 and .NET 11 migration](04-operations/11-swashbuckle-11-migration.md)

### Appendix

1. [Glossary](appendix/01-glossary.md)
2. [Package map](appendix/02-package-map.md)
3. [Samples](appendix/03-samples.md)
4. [Cheat sheet](appendix/04-cheat-sheet.md)

## Conventions

- Code examples target .NET 11 and C# 15.
- `Validator<T>` is the fluent authoring base class.
- `IValidator<T>` is the runtime contract shared by fluent and generated validators.
- “Generated validator” means a validator emitted by the Roslyn source generator from attributes.
- “Descriptor” means the frozen metadata exposed by `Describe()`.

The book is the source of truth for usage; the repository README is the source of truth for the
package list and build commands.
