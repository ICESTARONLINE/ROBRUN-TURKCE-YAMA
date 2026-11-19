# SignPath Foundation — application draft (English)

> Fill bracketed fields after the public GitHub repository exists.  
> **Do not submit until PatchData licensing / redistribution is clarified.**

---

## Project

- **Name:** ROBRUN TR (ROB RUN Turkish localization helper)
- **GitHub repository:** `https://github.com/<ORG_OR_USER>/ROBRUN-TR`  _(TBD)_
- **Homepage:** https://robrunturkceyama.com/
- **Publisher / applicant:** ICESTAR — contact: _TBD email_
- **Primary binary:** `ROBRUN_TR.exe` (Windows x64)

## Description

ROBRUN TR is an unofficial, open-source WinUI installer that helps users apply a Turkish language experience for the Steam game ROB RUN by writing Addressables cache files under the user’s LocalLow directory. It does not require administrator rights and is intended to avoid modifying Steam game binaries during normal install.

## License

- Original installer/tooling source: **MIT** (see `LICENSE`, scope-limited).
- Third-party libraries: see `THIRD_PARTY_NOTICES.md`.
- **Game-derived localization/font bundles are not MIT-licensed.** See unmet criteria below.

## How to build

Documented in `README.md`:

1. .NET 9 + WinUI publish → `ROBRUN_TR.exe`
2. Optional: Python/PyInstaller build of `tr_cache_worker.exe`
3. GitHub Actions workflow: `.github/workflows/build.yml`

## Security summary

- `asInvoker` manifest
- SHA-256 integrity checks (`integrity.json`)
- Explicit user confirmation of write paths
- No AV disablement / no fake signatures / no SmartScreen circumvention
- Details: `SECURITY.md`, `OPEN_SOURCE_AUDIT.md`, `CODE_SIGNING_POLICY.md`

## First release link

- _TBD_ — GitHub Release tag `v1.1.0` (unsigned until SignPath approval)  
- SHA-256: published in `SHA256SUMS.txt` with the release

## Privacy policy URL

- Repository: `PRIVACY.md`
- Site: https://robrunturkceyama.com/ (mirror/link when published)

## Code signing policy URL

- `CODE_SIGNING_POLICY.md` in repository root

## Unmet / blocking acceptance criteria (honest)

1. **Redistribution rights for PatchData** (Unity Addressables Turkish string/font bundles derived from ROB RUN) are **not** established under an OSI license. SignPath OSS programs typically require clear rights to what is shipped.
2. SignPath **organization-id / project-slug / API token** are not created yet (will not be invented in-repo).
3. Dual-control reviewers/approvers are placeholders in `CODE_SIGNING_POLICY.md`.
4. Nested PyInstaller worker may need extra scrutiny; prefer CI-built worker from source.

## Requested next steps after GitHub is public

1. Publish source without copyrighted game dumps (already gitignored).
2. Resolve PatchData licensing or ship a source-only tool that generates payloads from user-owned game files only.
3. Apply at SignPath OSS with this document + policy links.
4. Install SignPath GitHub App; set Actions secrets; set `SIGNPATH_ENABLED=true`.
5. Enable signing job; publish **signed** release + `SHA256SUMS-signed.txt`.

## References

- https://signpath.org/terms  
- https://docs.signpath.io/trusted-build-systems/github  
