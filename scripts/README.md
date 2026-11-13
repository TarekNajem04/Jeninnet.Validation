# Repository scripts

PowerShell tooling for repository maintenance. Each script is preview-first unless its README says otherwise, and each folder documents its own parameters, configuration, and outputs.

| Script | Purpose | Documentation |
|----|----|----|
| `scripts/common` | Shared banner, repository-root, configuration, and summary helpers | [README](common/README.md) |
| `scripts/clean` | Removes generated files and directories, including docs/API output and IDE caches | [README](clean/README.md) |
| `scripts/coverage` | Runs tests with XPlat Code Coverage and generates an HTML report | [README](coverage/README.md) |
| `scripts/format` | Applies and verifies .NET formatting | [README](format/README.md) |
| `scripts/sonar` | Runs the SonarScanner preview used by CI | [README](sonar/README.md) |
| `scripts/badges` | Generates the repository README badges | `generate.ps1` |
| `scripts/assets` | Generates the brand icon master and the 128×128 NuGet package icon | `generate-icon.ps1` |
| `scripts/quality` | Verifies the standard source-file attribution header | `check-source-headers.ps1` |
| `scripts/docs` | Verifies documentation links, root README claims, generated API coverage, and Arabic text | `check-doc-links.ps1`, `check-readme-claims.ps1`, `check-api-reference.ps1`, `check-arabic-text.ps1` |
| `scripts/api` | Checks public-API baselines, proves the RS0016/RS0017 gate, and syncs Unshipped/Shipped entries | [README](api/README.md) |
| `scripts/localization` | Checks the message-key catalog (constants vs packs), with an allow-list and a deterministic report | [README](localization/README.md) |

The documentation scripts are part of the documentation gate:

```powershell
pwsh scripts/docs/check-doc-links.ps1
pwsh scripts/docs/check-readme-claims.ps1
pwsh scripts/docs/check-api-reference.ps1
```

The API coverage check (`check-api-reference.ps1`) runs after DocFX metadata generation
(`dotnet docfx metadata docs/docfx.json`) and verifies that at least one public type from every
packable assembly appears in the generated manifest. The DocFX configuration (`docs/docfx.json`)
and the per-project `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` baselines are committed; the
generated `docs/api` and `docs/_site` folders are ignored.

`check-readme-claims.ps1` enforces two rules: every backticked `Jeninnet.Validation.*` name in `README.md` must resolve to a project under `src/`, `integrations/`, `samples/`, `tests/`, or `consumers/`; and the "N packable projects" claim must match the number of `src/` projects that declare `<IsPackable>true</IsPackable>`.

The coverage and formatting scripts are documented in their folders. They are not required to build the library unless you are reproducing the CI checks locally.

## Related documentation

- [Repository README](../README.md)
- [Documentation contribution guide](../docs/CONTRIBUTING.md)
- [Coverage tool](coverage/README.md)
- [Format tool](format/README.md)
- [Sonar preview](sonar/README.md)

## License

MIT — see [`LICENSE`](../LICENSE).
