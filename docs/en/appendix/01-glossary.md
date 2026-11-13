# Glossary

| Term | Meaning |
|---|---|
| **Validator** | A class implementing `IValidator<T>`; fluent or generated. |
| **Rule** | A unit that reads a value and reports a failure. |
| **Member group** | The rules configured for one member by `RuleFor`. |
| **Rule set** | A named group selected at validation time. |
| **Default rule set** | The pseudo-set `"default"` that selects rules outside named sets. |
| **Lifecycle hook** | `PreValidate`, `PreValidateAsync`, `OnFailure`, or `OnFailureAsync`. |
| **Cascade** | Whether a member continues after a failure (`Continue`/`Stop`). |
| **Fail-fast** | Stop the whole walk at the first error. |
| **Descriptor** | Frozen metadata returned by `Describe()`. |
| **Generated validator** | Validator emitted by the Roslyn source generator. |
| **Strict-async guard** | Sync `Validate` throws for async-only validators. |
| **Collector** | Pooled `ValidationErrorCollector` used during a walk. |
| **Root context data** | Pooled per-walk bag for memoized values. |
| **ProblemDetails** | RFC 9457 error response used by the ASP.NET Core package. |
| **FieldIdentifier** | Blazor identity for a model field. |
| **Client rules** | Exported rule data for client-side early feedback; not an engine. |
| **OpenCover** | Coverage report format consumed by Sonar. |
| **CPD** | Sonar copy-paste detection (duplication). |
| **AOT** | Ahead-of-time compilation (NativeAOT). |
| **Trim** | Removing unused code from a published application. |
