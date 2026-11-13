# Integration Packages

| Package | Main API | Behavior | Lifetime |
|---|---|---|---|
| `Jeninnet.Validation.AspNetCore` | `AddValidationEndpoints`, `AddRequestValidation<T>`, `AddMvcValidation`, `AddValidationInterceptor<T>`, `ToProblemErrors` | Minimal API filter, MVC filter, validation interceptors (before/after), ProblemDetails, exception handler | Validators scoped/singleton; filter transient; interceptors scoped by default |
| `Jeninnet.Validation.Extensions.Validation` | `AddJeninnetValidation()` / `AddJeninnetValidation<TModel>()`, `AddValidatedOptions<T>()` | `Microsoft.Extensions.Validation` adapter (net11.0) with `IValidatableTypeInfo`/`IValidatableParameterInfo` on .NET 11, plus synchronous `IValidateOptions<T>`/`ValidateOnStart` integration | Resolver per model; options validator singleton; validators resolved from DI |
| `Jeninnet.Validation.MediatR` | `AddValidationPipeline`, `UseResultAdapter<TResponse>` | Aggregated request validation before handlers; throw/continue/result | Behavior transient; accessor scoped |
| `Jeninnet.Validation.MediatR.ErrorOr` | `UseErrorOrResultAdapter<T>` | Maps failed results to `ErrorOr<T>` with validation metadata | Stateless mapper |
| `Jeninnet.Validation.MediatR.FluentResults` | `UseFluentResultsAdapter<T>` | Maps failed results to `FluentResults.Result<T>` with metadata | Stateless mapper |
| `Jeninnet.Validation.MediatR.CSharpFunctionalExtensions` | `UseCSharpFunctionalExtensionsAdapter<T>` | Maps failed results to `CSharpFunctionalExtensions.Result<T>` | Stateless mapper |
| `Jeninnet.Validation.DataAnnotations` | `[JeninnetValidatable]`, `[JeninnetValidateProperty]`, `JeninnetDataAnnotationsAdapter` | Runs validators in DataAnnotations pipelines, with class-level and member-level attributes; ships net11.0 assets | Validator resolved per validation |
| `Jeninnet.Validation.Blazor` | `<JeninnetValidator TModel>`, `JeninnetStaticSsrValidation` | `EditContext` subscription, sync/async validation, `FieldIdentifier` mapping, and static SSR `data-val-*` rendering | Component per interactive form; helper per static SSR member |
| `Jeninnet.Validation.Observable` | `ObservableValidationAdapter<TModel>` | `INotifyDataErrorInfo` decorator for WPF/MAUI/Avalonia view models | Adapter per view model |
| `Jeninnet.Validation.GraphQL` | `AddJeninnetGraphQLValidation()` | HotChocolate field middleware that validates input objects and maps errors to GraphQL errors | Middleware per schema |
| `Jeninnet.Validation.Grpc` | `AddJeninnetGrpcValidation()`, `ValidationInterceptor` | gRPC unary/server-streaming interceptor with `InvalidArgument` and `BadRequest` details | Interceptor singleton |
| `Jeninnet.Validation.JsonSchema` | `ValidationSchemaWriter` | Descriptor → draft 2020-12 schema | Stateless |
| `Jeninnet.Validation.OpenApi` | `AddSchemaConstraintsFromValidators` | Descriptor → OpenAPI schema transformer (`Microsoft.AspNetCore.OpenApi`) | Registry singleton |
| `Jeninnet.Validation.OpenApi.Swashbuckle` | `AddSchemaConstraintsFromValidators` | Descriptor → Swashbuckle `ISchemaFilter` | Registry per filter |
| `Jeninnet.Validation.OpenApi.NSwag` | `AddSchemaConstraintsFromValidators` | Descriptor → NJsonSchema `ISchemaProcessor` (NSwag) | Registry per processor |
| `Jeninnet.Validation.ClientRules` | `ClientRulesWriter` | Descriptor → JSON/TS/JS payload | Stateless |
| `Jeninnet.Validation.Diagnostics` | `AddValidationDiagnostics` | `ActivitySource`/`Meter` signals for validation duration and errors; ProblemDetails enrichment options live in AspNetCore | Singleton sink |
| `Jeninnet.Validation.Rules.Domain` | `Iban`, `VatNumber`, `PostalCode`, `NationalId`, `PasswordStrength`, `Uuid`, `Ulid` | Optional structural domain rules with server-side checksums | Stateless |
| `Jeninnet.Validation.Localization` | `AddDefaultMessagePacks`, `IValidationMessageCatalog` | Message packs for 40 cultures plus runtime overrides/additions | Singleton provider; mutable catalog |
| `Jeninnet.Validation.Localization.AspNetCore` | `AddJeninnetStringLocalizer`, `IValidationMessageResolver` | `IStringLocalizer` → packs → `ValidationError.Message`, with force/default/disable culture control | Resolver singleton; scoped UI-culture lookup |
| `Jeninnet.Validation.Assertions` | `ShouldHaveValidationErrors`, `TestValidate` | Test assertions | Test-time |

## Registration rules

- Register validators as `IValidator<T>` (scoped or singleton); they are thread-safe.
- Integration packages resolve validators from DI; missing validators bypass or throw depending on
  the adapter (ASP.NET Core/Blazor bypass, DataAnnotations throws when it cannot construct one).
- MediatR, ASP.NET Core, the Blazor async submit path, and the Observable adapter with
  `UseAsyncValidation` use the async path; DataAnnotations and the default Blazor/Observable paths
  are synchronous.
- MediatR result adapters are separate optional packages; the core MediatR package does not
  reference any result library. `MediatRValidationFailureStrategy.Result` requires a configured
  mapper.
- The ASP.NET Core localization bridge is a reader, not an `IInnerMessageProvider`; keep the
  exactly-one-provider rule when combining it with `AddDefaultMessagePacks`.
- Property-level `[JeninnetValidateProperty]` requires a validator that implements
  `IValidationOptionsValidator<T>` (all built-in and generated validators do).
- GraphQL rule sets require `IValidationOptionsValidator<T>`; gRPC rule sets use the same
  validator contract. Missing validators bypass validation in both adapters.
- `AddJeninnetValidation<TModel>()` is the AOT-safe registration; the non-generic overload scans
  existing `IValidator<T>` registrations, so call it after registering validators.
- `AddValidatedOptions<T>()` requires a registered `IValidator<T>` and validates options
  synchronously at startup; .NET 11 async options validation is planned for `1.1.0-preview.1`.
- Models using `[JeninnetValidatable]` are skipped by the `Microsoft.Extensions.Validation` bridge
  to avoid double validation through the DataAnnotations bridge.
- Schema/OpenAPI/client packages never execute validation.

## OpenAPI adapters

All three adapters read the same `ValidationSchemaRegistry` built from `IValidationDescriptorProvider`
validators, so the constraints match the JSON Schema and client-rule emitters:

```csharp
// Microsoft.AspNetCore.OpenApi (ASP.NET Core 10/11+)
builder.Services.AddSchemaConstraintsFromValidators(new CreateOrderValidator());

// Swashbuckle
builder.Services.AddSwaggerGen(options =>
    options.AddSchemaConstraintsFromValidators(new CreateOrderValidator()));

// NSwag
builder.Services.AddOpenApiDocument(settings =>
    settings.AddSchemaConstraintsFromValidators(new CreateOrderValidator()));
```

The Swashbuckle and NSwag adapters preserve an existing schema type (for example `integer`) and
only add constraints the validator declares. Nested models are covered when the nested type has its
own validator; rules that cannot be represented in OpenAPI (conditional, async, custom predicates)
remain server-side only. `[JsonPropertyName]` and the application's JSON naming policy are honored.

## Related

- [ASP.NET Core](../02-guides/08-aspnet-core.md)
- [MediatR](../02-guides/09-mediatr.md)
- [DataAnnotations](../02-guides/10-dataannotations.md)
- [Blazor](../02-guides/11-blazor.md)
- [INotifyDataErrorInfo](../02-guides/21-inotify-data-error-info.md)
- [HotChocolate GraphQL](../02-guides/22-graphql.md)
- [gRPC](../02-guides/23-grpc.md)
- [Localization](../02-guides/13-localization.md)
