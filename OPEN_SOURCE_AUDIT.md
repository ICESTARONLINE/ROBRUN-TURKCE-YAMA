# Open-source audit (SignPath / GitHub readiness)

## Stack

| Area | Technology |
|------|------------|
| Installer UI | C# / WinUI 3 / Windows App SDK / .NET 9 |
| Output | Single-file self-contained `ROBRUN_TR.exe` (`dotnet publish`) |
| Worker | Python (`tools/tr_worker/install_cache.py`) → PyInstaller `tr_cache_worker.exe` |
| Site | Static HTML/CSS/JS (+ optional PHP counter) under `website/` |

## Suitable for public GitHub (original / tooling)

- `installer/*.cs`, `*.xaml`, `*.csproj`, `app.manifest`
- `tools/tr_worker/*.py`, `*.spec`, translation JSON authored for this project
- Docs, workflows, site HTML/CSS/JS (without large media/EXE)

## Do **not** commit / not MIT (report only — not deleted)

| Path / type | Reason |
|-------------|--------|
| `ROB RUN.exe`, `StandaloneWindows64/`, root `*.bundle`, catalogs | Game files / copyrighted |
| `installer/PatchData/*.bundle`, prebuilt `tr_cache_worker.exe` | Game-derived localization/font payloads & binary blob |
| `extracted/`, `tools/RobRun_TR*`, `RobRun_TR/` | Third-party dump / reverse packaging |
| `tools/py312/` | Full Python redistributable tree |
| `music.wav` / large trailer assets | Media; license unclear for OSS |
| `*.pfx`, tokens, `.env` | Secrets |

These remain on the local machine / release pipeline only. `.gitignore` excludes them.

## Secrets scan (policy)

- No API keys found in installer source for SignPath (placeholders only).
- Do not commit PFX, `SIGNPATH_API_TOKEN`, or hosting credentials.
- Download counter JSON on site is runtime data — ignored for git.

## Behavioral notes (security transparency)

- Manifest: `asInvoker` (no admin UAC).
- Install writes Addressables cache under LocalLow; confirms paths in UI.
- `LaunchGuard` only relocates oddly named downloads (`ROBRUN_TR (1).exe`) to `%LocalAppData%\RobRunTurkceYama\` with `ABOUT.txt`.
- Worker launched with `--game <user path>` or `--kaldir`; SHA-256 integrity check against `integrity.json`.
- Optional HTTPS check of `https://robrunturkceyama.com/release.json` for version/hash.
- No SmartScreen bypass, no disabling of AV, no fake signatures.

## SignPath risk flags

1. **Game-derived PatchData** is required for a fully working signed EXE but is **not** clearly redistributable under an OSI license → may block SignPath OSS acceptance until clarified or split.
2. Nested `tr_cache_worker.exe` (PyInstaller) can look packer-like to AV; source build in CI preferred; UPX disabled in spec.
3. Organization / project IDs for SignPath are **not** invented here — fill after approval.
