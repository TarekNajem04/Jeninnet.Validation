# Why `.config` is in this repository

`.config/dotnet-tools.json` is the repository's **.NET local tool manifest**. It is not a
machine-specific folder and must stay in source control.

It pins the documentation tool used by CI:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "docfx": {
      "version": "2.81.0",
      "commands": ["docfx"]
    }
  }
}
```

`.github/workflows/docs-build.yml` runs:

```bash
dotnet tool restore
dotnet docfx metadata docs/docfx.json --warningsAsErrors
dotnet docfx build docs/docfx.json --warningsAsErrors
```

Including the manifest:

- makes the DocFX version reproducible on every machine and CI runner;
- lets `dotnet tool restore` install the pinned tool without a global install;
- keeps documentation builds identical for contributors and maintainers.

Do **not** add `.config/` to `.gitignore`. If you need a machine-local tool for an experiment,
use `dotnet tool install --global ...` or a separate manifest via `--tool-manifest`, and avoid
committing local-only files under this folder.
