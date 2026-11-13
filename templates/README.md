# Jeninnet.Validation templates

## `jeninnet-validator`

Creates a class library with a sample `Customer` model and a fluent `CustomerValidator`.

```bash
dotnet new install ./templates/jeninnet-validator
dotnet new jeninnet-validator -n MyCompany.Validation -o src/MyCompany.Validation
```

The generated project references `Jeninnet.Validation` `1.0.0-preview.1` and targets .NET 10 by
default; it runs unchanged on .NET 11.
Restore works once the preview package is available from your configured feed.

To uninstall the template:

```bash
dotnet new uninstall Jeninnet.Validation.ValidatorTemplate
```

The template is intentionally small: it gives a compiling starting point, not a full solution.
Add the generated project to your solution and replace the sample model/rules with your own.

## Requirements

- .NET 10 or .NET 11 SDK.
- A configured NuGet feed that can resolve `Jeninnet.Validation` `1.0.0-preview.1`.

## Related

- [Installation guide](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/01-getting-started/02-installation.md)
- [Fluent authoring](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/01-fluent-authoring.md)
- [Package map](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/02-package-map.md)

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
