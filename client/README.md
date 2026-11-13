# @jeninnet/validation-client

A dependency-free browser-side evaluator for the `ClientRules` payload emitted by
`Jeninnet.Validation.ClientRules`.

The server remains authoritative. The client runtime covers the synchronous, expression-friendly
subset of rules and reports `needsServer` whenever the payload contains a conditional, nested,
custom, asynchronous, or otherwise unsupported rule.

## Requirements

- A modern browser with ES2022 support for the runtime.
- Node.js 20 or later for the test suite and local development.

## Installation

```bash
npm install @jeninnet/validation-client
```

## Usage

```js
import { createValidator } from "@jeninnet/validation-client";

const rules = await fetch("/client-rules").then(response => response.json());
const validator = createValidator(rules);

const result = validator.validate({
    email: "not-an-email",
    name: ""
});

if(result.errors.length > 0) {
    render(result.errors);
}

if(result.needsServer) {
    await serverFallback(result);
}
```

The package ships ESM JavaScript and TypeScript declarations; it has no runtime dependencies and
does not use `eval` or `Function`.

## Static SSR forms

Import the unobtrusive module to enhance a server-rendered form that carries
`data-jeninnet-validation` and per-field `data-jeninnet-rules` attributes (rendered by
`JeninnetStaticSsrValidation`):

```js
import "@jeninnet/validation-client/unobtrusive";
```

The module validates on submit, blocks invalid submissions, writes messages into
`[data-jeninnet-message-for]` elements, and clears them on input. It uses the same evaluator and
honors `serverOnly` rules: those never block the browser, and the server remains authoritative. The
module auto-attaches on import; call `attach(form)` or `attachAll(root)` for forms added later.

## Supported rules

`NotNull`, `NotEmpty`, `StringNotEmpty`, `Length`, `ExactLength`, `Regex`, `Email`, `Url`,
`IPAddress`, `Phone`, `CreditCard`, `Base64String`, `FileExtensions`, `Contains`, `StartsWith`,
`EndsWith`, `Count`, `MinCount`, `MaxCount`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`,
`LessThanOrEqual`, `InclusiveBetween`, `ExclusiveBetween`, `Range`, `In`, `NotIn`,
`ScalePrecision`, `After`, and `Before`.

Everything else is marked `serverOnly: true` in the payload. A rule with `hasCondition: true` is
also server-only because the condition is an opaque delegate.

## Messages

The built-in `messages` module contains `en` and `ar` packs. Override them per validator:

```js
const validator = createValidator(rules, {
    culture: "ar",
    messages: {
        ar: { NotEmpty: "{PropertyName} مطلوب." }
    }
});
```

## Development

```bash
npm test
```

The tests use Node's built-in test runner and require no installation or dependencies.

## Related

- [`Jeninnet.Validation.ClientRules`](https://www.nuget.org/packages/Jeninnet.Validation.ClientRules) — emits the payload consumed here.
- [Client rules guide](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/02-guides/12-schema-openapi-client.md)

## License

MIT. See [LICENSE](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/LICENSE).
