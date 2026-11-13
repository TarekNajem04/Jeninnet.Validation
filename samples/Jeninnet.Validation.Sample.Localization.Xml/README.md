# XML Localization Sample

## Purpose

Demonstrates loading validation messages from XML files using the XML localization provider. Like JSON, XML files are culture-agnostic flat dictionaries; a wrapper pattern provides multi-culture support.

## Package References

- Jeninnet.Validation
- Jeninnet.Validation.Localization
- Jeninnet.Validation.Localization.Xml

## Folder Structure

```
Messages/
  en.xml                       English messages
  ar.xml                       Arabic messages
Product.cs                     Product model
ProductValidator.cs            Validation rules using custom message keys
Program.cs                     DI setup, message lookup, validation demo
```

## Running

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Xml
```

## Expected Output

The console displays:
- Part 1: Simple AddXmlLocalization() API with a single file
- Part 2: Culture-aware wrapper for multi-culture support
- Part 3: Validation with localized error messages
- Part 4: Missing-key fallback behavior

## When to Use

Use the XML provider when:
- Your team already uses XML for configuration files
- You prefer XSD validation for message file schema
- You need to transform messages with XSLT
- Your tooling ecosystem is XML-based

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Localization.Xml/Jeninnet.Validation.Sample.Localization.Xml.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
