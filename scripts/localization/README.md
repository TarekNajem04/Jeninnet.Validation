# Message-key catalog

Build-time governance for validation message keys. The source of truth is
`ValidationMessageKeys` (constants in
`src/Jeninnet.Validation.Abstractions/Configuration/ValidationMessageKeys.cs`);
every `Resources/*.json` pack in `src/Jeninnet.Validation.Localization` must contain
every constant, and every pack entry must map to a constant.

## Check

```powershell
pwsh scripts/localization/check-message-keys.ps1
```

The script compares constants against all 40 packs per culture and writes a deterministic
(ordinal-sorted) report to `artifacts/message-keys/report.json` and `report.md`. The report
directory is git-ignored. Any violation fails with exit code 1:

- A constant without a pack entry (missing key).
- A pack entry without a constant (extra key), unless allow-listed.
- A stale allow-list entry that matches no pack key.
- A non-string or empty template.
- Duplicate constant names or values.

Use `-NoReport` to skip writing the report. The `KeysPath`, `ResourcesPath`, and
`AllowListPath` parameters default to the repository locations and exist so the gate itself
can be exercised against fixture copies.

The same comparison runs as `MessageKeyCatalogTests` inside the test suite, so the rule is
enforced both before the build (CI `message-keys` job) and inside it.

## Allow-list

`src/Jeninnet.Validation.Localization/MessageKeyAllowList.json` holds pack keys that are
deliberate despite having no constant (for example a transitional key during a rename). It is
a JSON array of strings, currently empty.

Custom consumer keys need no allow-list entry: rule-template keys (`ExactLinesCount`),
caller-supplied codes (`PWD_DIGIT`), and resx keys live outside both the constants and the
packs by design. Add an entry only for a key that ships inside a
built-in pack. Stale entries fail the check, so remove the entry when the transition ends.

## Adding or renaming a key

1. Add or rename the constant in `ValidationMessageKeys.cs`.
2. Add or rename the entry in every `Resources/*.json` pack (all 40 cultures).
3. Run `pwsh scripts/localization/check-message-keys.ps1` and fix what it reports.
4. Run the catalog tests: `dotnet test tests/Jeninnet.Validation.Tests --filter MessageKeyCatalog`.
5. Follow the language checklist in the
   [localization guide](../../docs/en/02-guides/13-localization.md) for translations.

## Related

- [Localization guide](../../docs/en/02-guides/13-localization.md)
- [Package README](../../src/Jeninnet.Validation.Localization/README.md)
- [Documentation contribution guide](../../docs/CONTRIBUTING.md)
- [Scripts overview](../README.md)
