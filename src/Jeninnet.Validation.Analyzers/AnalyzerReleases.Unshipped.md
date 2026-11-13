### New Rules

Rule ID | Category            | Severity | Notes
--------|---------------------|----------|------
VAL0016 | ValidationGenerator | Warning  | Sync `Validate` invoked on a validator marked `[AsyncOnlyValidator]`; the runtime guard throws `AsyncValidatorInvokedSynchronouslyException`
VAL0017 | ValidationGenerator | Info     | Rule bounds cover the entire value range (tautological rule; always satisfied)
VAL0018 | ValidationGenerator | Error    | Attribute condition method is missing or has an invalid signature
VAL0019 | ValidationGenerator | Error    | Attribute lifecycle hook is missing or has an invalid signature
VAL0020 | ValidationGenerator | Error    | Custom validator method is missing or has an invalid signature
VAL0021 | ValidationGenerator | Error    | Date/time rule value cannot be parsed as the property type
VAL0022 | ValidationGenerator | Error    | Generic model cannot be validated by the source generator
VAL0023 | ValidationGenerator | Warning  | RuleFor member name does not match the member selected by the expression
VAL0024 | ValidationGenerator | Warning  | Count/MinCount/MaxCount applied to a non-collection target
VAL0025 | ValidationGenerator | Warning  | Async-only validator used by the synchronous DataAnnotations adapter
VAL0027 | ValidationGenerator | Error    | [Sanitize] cannot be applied to the member (no accessible setter, init-only setter, unsupported type, or unknown culture)
VAL0028 | ValidationGenerator | Warning  | [SkipValidation] is combined with validation attributes on the same property
VAL0029 | ValidationGenerator | Error    | Conditional attribute references a missing, unreadable, or self-referencing member
VAL0030 | ValidationGenerator | Error    | Conditional attribute value cannot be compared with the target member
VAL0031 | ValidationGenerator | Error    | Conditional attribute targets the same member more than once
VAL0032 | ValidationGenerator | Error    | Relative-date attribute requires DateTime, DateTimeOffset, or DateOnly
VAL0033 | ValidationGenerator | Error    | Exclusive range has equal bounds and can never match
VAL0034 | ValidationGenerator | Error    | Multiple-of divisor must be a positive finite number
