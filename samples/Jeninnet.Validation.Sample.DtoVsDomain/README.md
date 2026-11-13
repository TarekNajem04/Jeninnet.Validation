# DTO vs Domain Sample

Demonstrates separation of concerns between API DTOs, domain entities, and validation logic.

## Layers

| Layer | Types | Responsibility |
|-------|-------|---------------|
| **API** | `CreateUserDto`, `UpdateUserDto` | Input boundary — validated immediately |
| **Application** | `CreateUserDtoValidator`, `UpdateUserDtoValidator` | Validation logic — testable, reusable |
| **Domain** | `User`, `Address` | Pure data — no validation, no dependencies |

## Key Principle

> Domain entities should NOT contain validation logic.
> Validators are separate so they can be tested, swapped, or composed independently.

## Demonstrations

1. **DTO validation** — `CreateUserDtoValidator` validates input before it reaches the domain.
2. **Domain purity** — `User` entity has no validation attributes or rules.
3. **Valid DTO → Domain mapping** — Only validated DTOs are mapped to domain entities.
4. **Separation of concerns** — Each layer has single responsibility.

## Output

```
=== DTO vs DOMAIN SAMPLE ===
...
```

## Run

```bash
dotnet run --project samples/Jeninnet.Validation.Sample.DtoVsDomain/Jeninnet.Validation.Sample.DtoVsDomain.csproj
```

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
