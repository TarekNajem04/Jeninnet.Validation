# Public API baselines

Jeninnet.Validation treats the public surface as a contract. Every packable project under `src/`
commits two files next to its `.csproj`:

| File | Meaning |
|---|---|
| `PublicAPI.Shipped.txt` | The reviewed baseline for APIs that have shipped. |
| `PublicAPI.Unshipped.txt` | The staging area for new APIs that will ship in the next version. |

`Microsoft.CodeAnalysis.PublicApiAnalyzers` compares the compiled public surface with those files.
`RS0016` means a public API is missing from the baseline; `RS0017` means a baseline entry no longer
exists. The repository builds with warnings as errors, so either diagnostic fails the build and
the `api-baselines` CI job.

## Add a public API

1. Add the API to the source.
2. Run `dotnet build` and confirm the `RS0016` diagnostic.
3. Run the sync script. It invokes the analyzer code fix and writes the new entries to
   `PublicAPI.Unshipped.txt`:

   ```powershell
   pwsh scripts/api/sync-public-api.ps1
   ```

   In an IDE, the equivalent action is the **Declare public API** code fix (fix all in project or
   solution).
4. Review the diff. Commit the source and the `PublicAPI.Unshipped.txt` change together.
5. Keep the entry in `Unshipped` until the release is cut. At release time, promote every reviewed
   entry:

   ```powershell
   pwsh scripts/api/sync-public-api.ps1 -Promote
   ```

## Remove or change a public API

- Removing a member or changing its signature makes the stale baseline entry fail with `RS0017`.
- A breaking change is allowed only in a major version. Before removal, mark the member
  `[Obsolete("Use X instead.", error: false)]` for at least one minor release, keep the behavior,
  and add a migration note. Exception: before `1.0.0` nothing has shipped, so delete the member
  and its baseline entry outright instead of marking it `[Obsolete]`.
- When the removal is intentional, delete the stale entry from `Shipped` or `Unshipped` in the same
  change, update the changelog, and explain the migration path in the pull request.
- Never delete a baseline entry to silence an accidental change. If the change is not intended,
  revert the source change instead.

## Razor and Blazor components

`dotnet format` cannot apply `RS0016` fixes that map to a `.razor` file; the diagnostic points at
generated code and the code fix has no C# document to edit. Maintain
`src/Jeninnet.Validation.Blazor/PublicAPI.Shipped.txt` manually:

- The build lists the missing members by name.
- The generated `BuildRenderTree` is inside a `#nullable disable` region, so its baseline entry
  starts with `~` (the analyzer's oblivious marker).
- `RS0041` is suppressed in the Blazor project with a comment; the generated member is tracked with
  the `~` entry instead.

## Verify locally

```powershell
pwsh scripts/api/check-public-api-baselines.ps1
pwsh scripts/api/test-public-api-gate.ps1
dotnet build Jeninnet.Validation.slnx
```

`check-public-api-baselines.ps1` checks file presence, encoding, line endings, the `#nullable
enable` header, duplicates, and the analyzer wiring. `test-public-api-gate.ps1` creates a throwaway
project and proves the four transitions: clean baseline, `RS0016` for a new API, success after
recording it in `Unshipped`, and `RS0017` for a removed API.

## Add a new packable project

1. Create `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` with `#nullable enable` as the first
   line.
2. Run `pwsh scripts/api/sync-public-api.ps1` to populate `Unshipped`, review it, and promote it to
   `Shipped` only when the surface is intentional.
3. Add the project to `docs/docfx.json`; the architecture test and the API-reference check fail if a
   packable project is missing.
4. Update the package count in `README.md` and `ROADMAP.md` when the release package set changes.
5. Run the full build, tests, and documentation checks listed in `CONTRIBUTING.md`.

## File conventions

- UTF-8 without BOM, LF line endings, and a final newline.
- `#nullable enable` as the first line.
- One API per line, sorted with `StringComparer.Ordinal`, with no duplicate entries across
  `Shipped` and `Unshipped`.
- Do not hand-format entries unless the build confirms the exact text. The analyzer's display format
  includes modifiers, fully qualified types, default values, nullability markers (`?`/`!`), and the
  `~` oblivious marker.

## Related

- [Compatibility and versions](../../docs/en/03-reference/06-compatibility-and-versions.md)
- [API reference](../../docs/en/03-reference/08-api-reference.md)
- [Contributing](../../CONTRIBUTING.md)
- [Scripts overview](../README.md)
