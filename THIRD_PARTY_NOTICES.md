# Third-party notices

## Dependencies (installer)

| Component | License | Notes |
|-----------|---------|--------|
| .NET 9 / Windows App SDK / WinUI 3 | Microsoft terms | Runtime / UI framework |
| Microsoft.Toolkit.Uwp.Notifications | MIT | Toast notifications |
| WebView2 | Microsoft terms | Windows App SDK dependency |

## Dependencies (cache worker)

| Component | License | Notes |
|-----------|---------|--------|
| Python 3.12 | PSF | Build host for PyInstaller worker |
| UnityPy | MIT | Addressables / asset parsing |
| Pillow (PIL) | HPND-like | Image processing |
| NumPy | BSD | Numeric dependency of UnityPy stack |
| PyInstaller | GPL compatible / various | Packaging `tr_cache_worker.exe` |

Exact versions are pinned in `tools/tr_worker/requirements.txt` when present.

## Game content (NOT licensed by this project)

**ROB RUN** and related trademarks/assets are owned by their respective rights holders (including Kefir and Steam partners).

This project is an unofficial fan localization helper. It may write Turkish localization/font data into the user's Addressables cache under:

`%USERPROFILE%\AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables\`

Steam game install files are not the primary write target. Legacy in-game patches, if present, may be restored from backups.

**Do not** treat Unity `.bundle` patch payloads as MIT-licensed. See `OPEN_SOURCE_AUDIT.md` and `PatchData/README.md`.
