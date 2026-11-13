# Brand assets

| File | Purpose |
| --- | --- |
| `jeninnet-validation-icon-master.png` | 512x512 master icon for documentation, social cards, and future exports. |
| `jeninnet-validation-icon-128.png` | RGBA 128x128 icon packed into every NuGet package (`PackageIcon`). |

The icon is a navy rounded tile with a cyan-to-emerald validation shield, a bold checkmark,
and a source-generator spark. It is generated deterministically from source:

```powershell
pwsh ./scripts/assets/generate-icon.ps1
```

Do not hand-edit the PNG files. Change the renderer and regenerate both sizes so the master and
the package icon stay consistent. The 128x128 asset must remain an 8-bit RGBA PNG; the
`PackagingBaselineTests` architecture test verifies its dimensions and color type.
