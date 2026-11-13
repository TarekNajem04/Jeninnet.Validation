# Jeninnet.Validation.Sample.Localization.Base

Shared base library for all localization sample projects. Provides common model types, validators, helper methods, and constants to eliminate code duplication across samples.

## Purpose

All localization samples share the same validation patterns: they validate a model, look up localized messages, and display results. This base project centralizes that shared code so each sample only contains its unique provider registration and output logic.

## Package References

- Jeninnet.Validation
- Jeninnet.Validation.Localization

## Folder Structure

```
User.cs                          User model (Name, Email, Password, Age) + static User.Invalid
Customer.cs                      Customer model (Name, Email, Age)
UserValidator.cs                 Validation rules for User
CustomerValidator.cs             Validation rules for Customer
Helper.cs                        Utility methods: ShowMessageAsync, ShowLocalizedErrorsAsync
Constants.cs                     Shared constants (culture codes, message keys, property labels)
GlobalUsings.cs                  Global using directives
```

## Shared Types

| Type | Description |
|------|-------------|
| `User` | Record with `Name`, `Email`, `Password`, `Age` + static `User.Invalid` instance |
| `Customer` | Record with `Name`, `Email`, `Age` |
| `UserValidator` | `Validator<User>` with NotEmpty, EmailAddress, Length(8,64), InclusiveBetween(18,60) |
| `CustomerValidator` | `Validator<Customer>` with NotEmpty, EmailAddress, InclusiveBetween(18,60) |
| `Helper` | `ShowMessageAsync` for direct key lookup, `ShowLocalizedErrorsAsync` for validation with localized errors |
| `Constants` | Culture codes (`en-US`, `ar-SY`, `nl-NL`), message keys (`NotEmpty`, `Email`, `Length`, etc.), property labels |

## Usage by Samples

Consumer samples reference this project and add a `global using Jeninnet.Validation.Sample.Localization.Base;` directive. They then use `User`, `Customer`, `Helper`, and `Constants` as if they were defined locally.

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
