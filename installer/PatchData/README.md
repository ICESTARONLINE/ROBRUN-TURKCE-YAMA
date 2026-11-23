# installer/PatchData

Place the following files here before `dotnet publish` (gitignored except README + integrity.json):

| File | Purpose |
|------|---------|
| `tr_cache_worker.exe` | Addressables cache installer worker |
| `inbuild_assets_all_turkish_.bundle` | Turkish font glyphs |
| `localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_.bundle` | Turkish strings |
| `integrity.json` | Expected SHA-256 hashes |

See root `OPEN_SOURCE_AUDIT.md` for license scope.
