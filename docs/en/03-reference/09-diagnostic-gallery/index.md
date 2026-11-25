# Diagnostic gallery

Every `VAL` diagnostic with a minimal trigger, the compiler output, and either a tested
fix or the reason no automatic fix is safe. The pages are generated from
`diagnostics.json` and checked byte-for-byte in CI.

| ID | Severity | Title | Fix status |
|---|---|---|---|
| [VAL0001](VAL0001.md) | `Error` | Invalid rule parameter | Available |
| [VAL0002](VAL0002.md) | `Error` | Rule applied to incompatible property type | Intentional |
| [VAL0003](VAL0003.md) | `Error` | Rule parameter type mismatch | Intentional |
| [VAL0004](VAL0004.md) | `Warning` | Nested validator not found | Intentional |
| [VAL0005](VAL0005.md) | `Warning` | Collection element validator not found | Intentional |
| [VAL0006](VAL0006.md) | `Warning` | Rule cannot be used on nullable type | Available |
| [VAL0007](VAL0007.md) | `Error` | Conflicting rules on property | Available |
| [VAL0008](VAL0008.md) | `Error` | Unsupported attribute argument type | Intentional |
| [VAL0009](VAL0009.md) | `Error` | Invalid rule combination | Available |
| [VAL0010](VAL0010.md) | `Error` | Rule parameter out of range | Available |
| [VAL0011](VAL0011.md) | `Info` | Consider adding [NotEmpty] for string property | Available |
| [VAL0012](VAL0012.md) | `Info` | Consider adding [NotNull] for nullable property | Available |
| [VAL0013](VAL0013.md) | `Info` | Rule can be simplified | Intentional |
| [VAL0014](VAL0014.md) | `Warning` | Redundant [NotNull] attribute | Available |
| [VAL0015](VAL0015.md) | `Error` | Target property not found | Intentional |
| [VAL0016](VAL0016.md) | `Warning` | Synchronous Validate on async-only validator | Available |
| [VAL0017](VAL0017.md) | `Info` | Tautological validation rule | Available |
| [VAL0018](VAL0018.md) | `Error` | Invalid condition method | Intentional |
| [VAL0019](VAL0019.md) | `Error` | Invalid lifecycle hook | Intentional |
| [VAL0020](VAL0020.md) | `Error` | Invalid custom validator | Intentional |
| [VAL0021](VAL0021.md) | `Error` | Invalid date/time rule value | Intentional |
| [VAL0022](VAL0022.md) | `Error` | Generic model is not supported | Intentional |
| [VAL0023](VAL0023.md) | `Warning` | RuleFor member name mismatch | Available |
| [VAL0024](VAL0024.md) | `Warning` | Collection rule requires a collection | Intentional |
| [VAL0025](VAL0025.md) | `Warning` | Async-only validator in synchronous adapter | Intentional |
| [VAL0027](VAL0027.md) | `Error` | Sanitize cannot be applied | Intentional |
| [VAL0028](VAL0028.md) | `Warning` | SkipValidation conflicts with validation attributes | Intentional |
| [VAL0029](VAL0029.md) | `Error` | Conditional attribute has an invalid target | Intentional |
| [VAL0030](VAL0030.md) | `Error` | Conditional attribute value is not supported | Intentional |
| [VAL0031](VAL0031.md) | `Error` | Conditional attribute is duplicated | Intentional |
| [VAL0032](VAL0032.md) | `Error` | Relative date rule requires a date/time member | Intentional |
| [VAL0033](VAL0033.md) | `Error` | Exclusive range has equal bounds | Intentional |
| [VAL0034](VAL0034.md) | `Error` | Invalid multiple-of divisor | Intentional |

**33 diagnostics:** 11 with a tested code fix and 22 intentional omissions.

The matrix view lives in the [diagnostics reference](../03-diagnostics.md). The analyzer
package README links here for the before/after learning path.
