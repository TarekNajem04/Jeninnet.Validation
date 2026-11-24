# Package dependency budget

`check-package-dependency-budget.ps1` reads the produced `.nupkg` files and compares them with the
committed [`package-dependency-budget.json`](package-dependency-budget.json). The baseline freezes:

- `lib/<tfm>` assets, or analyzer/tool assets for the analyzer and migration packages;
- dependencies per TFM, including version and `exclude` metadata;
- README, icon, license, and repository metadata;
- a reviewed package-size budget (current size plus 10%, rounded up, with an 8 KB minimum).

## Check

```powershell
pwsh scripts/package/check-package-dependency-budget.ps1 -PackagesPath artifacts/packages
```

The gate fails when a package adds or removes a dependency, loses or adds an asset, changes
metadata, exceeds its budget, or introduces an analyzer dependency into a runtime package.

## Update the baseline

Only update after reviewing the package diff:

```powershell
pwsh scripts/package/check-package-dependency-budget.ps1 -PackagesPath artifacts/packages -Update
```

The JSON file is committed. A dependency or asset change without a baseline update fails CI and the
release gate.

## Negative tests

```powershell
pwsh scripts/package/test-package-dependency-budget-gate.ps1 -PackagesPath artifacts/packages
pwsh scripts/package/test-package-dependency-budget-gate.ps1 -PackagesPath artifacts/packages -Mode InjectedDependency
```

The first test copies one package, removes a lib assembly, and confirms the gate exits `1`. The
second injects `Roslynator.Analyzers` into a dependency group and confirms both the dependency
comparison and the runtime-analyzer rule fail the gate.

## CI and release gate

`ci.yml` runs the check after package packing. The release runner exposes it as the
`package-budget` gate, after `package-pack` and before the version/consumer checks.

## Baseline scope

The current baseline covers all 25 packable packages, including the five dual-target packages and
the analyzer package. The Migration tool package uses `tools/net10.0/any` assets and is classified
separately from runtime libraries.
