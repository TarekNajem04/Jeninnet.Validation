# Authentication Sample

Demonstrates authentication request validation using RuleSet, ValidateAndThrow, WithMessage, OverridePropertyName, and Localization.

## Validators

| Validator | Models | Features |
|-----------|--------|----------|
| RegisterRequestValidator | RegisterRequest | RuleSet (Default + PasswordStrength) |
| LoginRequestValidator | LoginRequest | OverridePropertyName, WithMessage |
| ChangePasswordRequestValidator | ChangePasswordRequest | ValidateAndThrow, RuleSet, custom Must |
| ForgotPasswordRequestValidator | ForgotPasswordRequest | WithMessage, EmailAddress |
| ResetPasswordRequestValidator | ResetPasswordRequest | RuleSet (Default + PasswordStrength + Confirmation) |

## Demonstrations

1. **RuleSet** — RegisterRequest validates basic fields (Default) and password strength (PasswordStrength).
2. **OverridePropertyName** — LoginRequest shows Username as `LoginName` in errors.
3. **ValidateAndThrow** — ChangePasswordRequest throws `ValidationException` on failure.
4. **WithMessage** — ForgotPasswordRequest uses business-friendly custom messages.
5. **Localization** — Standard validation keys are localized via JSON providers (English + Arabic).
6. **Valid data** — All rules pass when valid input is provided.

## Output

```
=== AUTHENTICATION SAMPLE ===
Demonstrates RuleSet, ValidateAndThrow, WithMessage,
OverridePropertyName, and Localization.
...
Done.
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.Authentication/Jeninnet.Validation.Sample.Authentication.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
