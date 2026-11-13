# Samples

The repository contains 26 sample projects across 23 directories; 21 are in the solution. Use them
as executable documentation after reading the quick start.

## Run the console sample

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Console
```

The console sample demonstrates DI registration, rule sets, child rules, `Include`, `ForEach`, and
the difference between `Validate` and `ValidateAndThrow`. It is the fastest end-to-end check after a
clone.

## What each sample teaches

| Sample | Concepts |
|---|---|
| **Console** | DI, rule sets, child rules, `Include`, `ForEach`, sync/async paths. |
| **Domain** | Pure domain entities shared by the samples; attribute authoring examples. |
| **Customization** | Custom rules, rule extensions, and reusable validators. |
| **NestedValidation** | Recursive object validation across four levels and nested error paths. |
| **CleanArchitecture** | Domain/Application/Infrastructure/Web layering with validation at the boundary. |
| **MinimalApi** | `AddRequestValidation<T>`, ProblemDetails, `/client-rules` export. |
| **Mvc** | `AddMvcValidation`, `ModelState`, rule sets per action. |
| **MediatR** | `AddValidationPipeline`, aggregated errors, throw/continue strategies. |
| **Analyzers.Sample.Console** | Attribute-driven validation and the source generator. |
| **Localization.\*** | Provider scenarios using the sibling `Jeninnet.Localization` packages. |
| **AotSmoke** | NativeAOT publish/run harness (not a test project). |

## Run a host sample

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.MinimalApi
dotnet run --project samples/Jeninnet.Validation.Sample.Mvc
dotnet run --project samples/Jeninnet.Validation.Sample.MediatR
```

The web samples use the ASP.NET Core launch profile in the project. Send an invalid request and
confirm the 400 ProblemDetails shape described in [ASP.NET Core](../02-guides/08-aspnet-core.md).

## Run the Clean Architecture sample

```bash
dotnet run --project samples/CleanArchitecture/Jeninnet.Validation.Sample.CleanArchitecture.Web
```

The solution contains four projects: Domain, Application, Infrastructure, and Web. Validation lives
at the application boundary; the domain stays free of framework references.

## Run the AOT smoke harness

```bash
dotnet publish samples/Jeninnet.Validation.AotSmoke/Jeninnet.Validation.AotSmoke.csproj \
  -c Release -r linux-x64 --self-contained -o out/aot-smoke

./out/aot-smoke/Jeninnet.Validation.AotSmoke
```

On Windows, use `-r win-x64` and run `out/aot-smoke/Jeninnet.Validation.AotSmoke.exe`. The harness
exercises generated validators, rule sets, async validation, the strict-async guard, localization,
and source-generated JSON. See [AOT and trimming](../04-operations/02-aot-and-trimming.md).

## Localization samples

The `Localization.*` projects reference the sibling `Jeninnet.Localization.*` packages and may need
provider-specific configuration (file paths, SQLite databases, or embedded resources). Read the
README in each sample before running it.

## Standalone samples

These projects are not part of the solution build but can be run individually:

- **Authentication** — validation in an authentication flow.
- **DtoVsDomain** — DTO validation versus domain invariants.
- **ECommerce** — a larger end-to-end scenario.
- **ErrorHandling** — mapping validation errors to responses.
- **FullDemo** — a combined demonstration.

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.ErrorHandling
```

## Run the tests

```bash
dotnet test tests/Jeninnet.Validation.Tests/Jeninnet.Validation.Tests.csproj
dotnet test tests/Jeninnet.Validation.Analyzers.Tests/Jeninnet.Validation.Analyzers.Tests.csproj
dotnet test tests/Jeninnet.Validation.ArchitectureTests/Jeninnet.Validation.ArchitectureTests.csproj
```

The full suite is 997 tests across the core, analyzer, architecture, and integration test projects;
all suites run on `net11.0`.
Use `dotnet test Jeninnet.Validation.slnx` to run everything the solution includes.

## Related

- [Quick start](../01-getting-started/03-quick-start.md)
- [Package map](02-package-map.md)
- [Cheat sheet](04-cheat-sheet.md)
