# ASP.NET Core MVC Sample

Demonstrates Jeninnet.Validation integration with ASP.NET Core MVC controllers.

## Highlights

- Model validation via `Validator<T>` with DI
- `ValidationFilter<T>` action filter for automatic validation
- `Results.ValidationProblem()` for structured error responses
- `ForEach` collection element validation
- `Matches` regex validation for SKU format
- `In` allowed-values validation for category

## Static SSR validation

The sample also hosts a Blazor static SSR page at `/static-ssr`. It renders
`data-val-*`/`data-jeninnet-*` attributes from a generated validator descriptor with
`JeninnetStaticSsrValidation`, loads `wwwroot/js/jeninnet-unobtrusive.js`, and validates the posted
model on the server. The browser module is copied from `client/src`; keep the copies in sync.

- `Components/Pages/StaticSsrRegister.razor` — the form and server-side validation summary.
- `Models/StaticSsrRegistration.cs` — the attribute-annotated model.
- `wwwroot/js` — the dependency-free browser runtime.

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Mvc
```

Then visit `http://localhost:5000/api/Products/self-test` for the MVC endpoints or
`http://localhost:5000/static-ssr` for the static SSR form.

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
