# Compatibility and Versions

## Runtime and toolchain

- .NET 11.0 (STS). Every shipping library targets `net11.0`; the analyzer targets `netstandard2.0`.
- C# 15.
- The current preview is `1.0.0-preview.1`.

## Compatibility matrix

| Surface | Supported |
|---|---|
| .NET | 11.0 (STS); every shipping library targets `net11.0`; analyzer `netstandard2.0` |
| C# | 15 |
| ASP.NET Core | 11.0 |
| Microsoft.Extensions.* | 11.0.x |
| MediatR | 15.x |
| `Jeninnet.Localization.*` | `1.0.0-preview.1` (the sibling localization preview line); only required by the localization packages, not by core validation |
| Native AOT / trimming | `Jeninnet.Validation`, `Abstractions`, `Assertions`, `ClientRules`, `Diagnostics`, `Extensions.Validation`, `JsonSchema`, `Localization`, `Rules.Domain` |
| Host integration | `AspNetCore`, `Localization.AspNetCore`, `MediatR`, `OpenApi`, `DataAnnotations`, `Blazor` follow the host’s AOT posture; `DataAnnotations`, `Blazor`, and `Localization.AspNetCore` are not AOT targets |

The repository ships 25 packable packages. See the [package map](../appendix/02-package-map.md).

## Versioning

The product version is centralized in `Directory.Build.props`:

```xml
<Version>1.0.0-preview.1</Version>
<FileVersion>1.0.0.0</FileVersion>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<PackageVersion>$(Version)</PackageVersion>
```

Package projects do not declare their own version. Previews use `1.0.0-preview.N`, release
candidates use `1.0.0-rc.N`, and stable uses `1.0.0`; additional preview or RC releases can be cut
before stable. Additive changes use minor versions; breaking changes require a major version.

`AssemblyVersion` and `FileVersion` stay numeric because they cannot carry a prerelease label. The
NuGet package version is the prerelease value.

## Inheritance behavior

- `[Validate]` on a derived type includes the rules declared on its base classes; a base type
  annotated alone does not generate validators for its derived types.
- `[Validate]` also targets interfaces. Annotated interfaces contribute matching public property
  rules to annotated implementing classes.
- `SetInheritanceValidator` dispatches by exact runtime type and skips unregistered types. This is
  an explicit map, not automatic discovery.

## Upgrading previews

1. Read the release notes for the target preview.
2. Update **all** Jeninnet.Validation packages together; mixed preview versions are unsupported.
3. Rebuild with warnings as errors; analyzer IDs and generator output can change between previews.
4. Run the full test suite, including generated/fluent parity tests.
5. Re-run the AOT publish if you ship trimmed or Native AOT output.
6. Re-generate JSON Schema, OpenAPI, and client rules; the descriptor shape can change.

```xml
<PackageReference Include="Jeninnet.Validation" Version="1.0.0-preview.1" />
<PackageReference Include="Jeninnet.Validation.AspNetCore" Version="1.0.0-preview.1" />
<PackageReference Include="Jeninnet.Validation.Localization" Version="1.0.0-preview.1" />
```

## Public API baselines

Every packable project commits two files next to its `.csproj`:

| File | Contents |
|---|---|
| `PublicAPI.Shipped.txt` | The reviewed surface that has shipped. |
| `PublicAPI.Unshipped.txt` | Additive APIs staged for the next release. |

`Microsoft.CodeAnalysis.PublicApiAnalyzers` compares the compiled public surface with those files.
The repository builds with warnings as errors, so an unrecorded API (`RS0016`) or a baseline entry
that no longer exists (`RS0017`) fails `dotnet build` and the `api-baselines` CI job.

- Adding an API is additive: add the code, run `pwsh scripts/api/sync-public-api.ps1`, review the
  `Unshipped` diff, and commit it with the change.
- At release, promote reviewed entries with `pwsh scripts/api/sync-public-api.ps1 -Promote`.
- The DocFX pages and the curated [API reference](08-api-reference.md) are generated from XML
  documentation; the baselines are the compatibility contract, not the documentation.
- `scripts/api/README.md` documents the detailed add/remove workflow, and
  `pwsh scripts/api/test-public-api-gate.ps1` proves the `RS0016`/`RS0017` behavior in a throwaway
  project.

## Obsolete and breaking changes

- Public API changes are never silent.
- Before `1.0.0` no package has shipped, so there are no consumers to migrate: delete
  the member and its baseline entry outright instead of marking it `[Obsolete]`.
- A breaking change is allowed only in a major version. In a minor release, mark the member
  `[Obsolete("Use X instead.", error: false)]`, keep its behavior, add the replacement, and publish
  a migration note.
- Keep the obsolete member for at least one minor release. It remains in `Shipped` because consumers
  can still call it.
- In the next major, remove the member, delete its baseline entry, and update the changelog and
  migration guide. `[Obsolete(..., error: true)]` belongs to that major release branch, not to a
  minor.
- A signature change is a removal plus an addition: follow the same deprecation window and record
  the new signature in `Unshipped`.
- Never delete a baseline entry to hide an accidental change. If the change is not intended, revert
  the source instead.
- Preview packages may still change before `1.0.0`; the baseline gate ensures every preview change
  is reviewed, and the policy becomes strict from `1.0.0`.
- Integration packages are optional and versioned with the product.
- Analyzer diagnostics are part of the developer contract; IDs are stable within a major line,
  severities may change with a note.

## AOT and trimming

The AOT-compatible libraries (`Jeninnet.Validation`, `Abstractions`, `JsonSchema`, `ClientRules`,
`Localization`, and `Assertions`) declare `IsAotCompatible` and build with zero trim/AOT analyzer
warnings. Reflection-based assembly scanning is annotated; explicit registration is the AOT-safe
pattern. The NativeAOT smoke harness publishes and runs the engine.

## Related

- [Roadmap](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/ROADMAP.md)
- [Known limitations](07-known-limitations.md)
- [AOT and trimming](../04-operations/02-aot-and-trimming.md)
- [Production checklist](../04-operations/08-production-checklist.md)
