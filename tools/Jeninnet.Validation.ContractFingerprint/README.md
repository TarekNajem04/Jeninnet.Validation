# Jeninnet.Validation.ContractFingerprint

Internal tooling that computes a deterministic, versioned fingerprint over the validation
descriptors used by the JSON Schema, OpenAPI, and client-rule writers.

## Usage

```powershell
# Print the fingerprint document.
dotnet run -c Release -f net10.0 --project tools/Jeninnet.Validation.ContractFingerprint

# Write it to a file.
dotnet run -c Release -f net10.0 --project tools/Jeninnet.Validation.ContractFingerprint -- \
  --output artifacts/contract-fingerprints/net10.json

# Prove the sensitivity and stability properties.
dotnet run -c Release -f net10.0 --project tools/Jeninnet.Validation.ContractFingerprint -- \
  --self-test
```

The CI gate is `scripts/quality/check-contract-fingerprints.ps1`. It runs the tool on `net10.0` and
`net11.0`, proves the two outputs are byte-identical, compares them with
`scripts/quality/validation-contract-fingerprints.json`, and runs the self-test. The negative test
is `scripts/quality/test-contract-fingerprint-gate.ps1`.

## Contract

The fingerprint covers:

- validator name, model name, member paths;
- rule order, rule kind, message key, authored-message flag, severity, error code, display name;
- condition presence (`always` / `conditional`), rule parameters, and nested descriptors.

The canonical text is hashed with SHA-256 and prefixed with `v1:sha256:`. Members are sorted
ordinally; rule order is preserved because it affects fail-fast and cascade behavior. Parameter keys
and collection values are sorted ordinally. The format is deterministic across `net10.0` and
`net11.0`.

## Limits

- This is a **contract signal, not a security guarantee**. It detects descriptor drift, not
  malicious code.
- Predicate bodies are opaque delegates and do not have a stable token; only condition presence and
  descriptor parameters are fingerprinted.
- Custom state is not hashed; use message keys, error codes, and display names for contract changes.
- The fingerprint source must not use reflection, `Activator`, or dynamic code. The checker enforces
  the token scan and the tool works only with the descriptor interfaces.
