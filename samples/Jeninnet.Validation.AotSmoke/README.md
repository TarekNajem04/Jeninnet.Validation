# NativeAOT Smoke Harness

A small console sample that **publishes with NativeAOT and runs** the validation
engine end to end. It is a sample, not a unit-test project, and it is not part
of the normal `dotnet test` run.

## What it checks

The harness exits `0` only when all checks pass:

- generated validator executes
- generated email and range rules execute
- generated custom rule executes
- rule sets select rules
- fluent async validation executes
- strict-async guard throws on a synchronous call to an async-only validator
- async-only rule executes on the async path
- localization resolves a message through the sibling pipeline
- source-generated `System.Text.Json` round-trip works
- no reflection-based validator scanning is used (`AddValidation()` reads the generated catalog)

## Run

Windows:

```powershell
dotnet publish samples/Jeninnet.Validation.AotSmoke/Jeninnet.Validation.AotSmoke.csproj `
  -c Release -r win-x64 --self-contained -o out/aot-smoke
./out/aot-smoke/Jeninnet.Validation.AotSmoke.exe
```

Linux:

```bash
dotnet publish samples/Jeninnet.Validation.AotSmoke/Jeninnet.Validation.AotSmoke.csproj \
  -c Release -r linux-x64 --self-contained -o out/aot-smoke
./out/aot-smoke/Jeninnet.Validation.AotSmoke
```

NativeAOT prerequisites: the .NET 10 SDK plus a C/C++ toolchain (`clang` and
`zlib1g-dev` on Linux, Visual Studio C++ tools on Windows).

## CI

`.github/workflows/aot-smoke.yml` publishes and runs the harness on
`linux-x64` for changes under `src/**` or this folder. The workflow also builds
the two shipping libraries with trim/AOT analyzers in Release.

## Notes

- `NU1903` is suppressed because the SDK's `Microsoft.DotNet.ILCompiler` build
  tool carries a vendor-gated advisory; it is not shipped application code.
- The full AOT/trimming guarantee and exclusions are documented in
  [`docs/en/04-operations/02-aot-and-trimming.md`](../../docs/en/04-operations/02-aot-and-trimming.md).

## Related

- [Sample index](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/appendix/03-samples.md)
- [Documentation](https://github.com/TarekNajem04/Jeninnet.Validation/blob/main/docs/en/README.md)
