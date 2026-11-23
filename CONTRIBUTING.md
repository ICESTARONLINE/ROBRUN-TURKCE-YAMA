# Contributing

## Ground rules

1. Do **not** commit game binaries, Addressables `.bundle` payloads, PFX files, or secrets.
2. Do **not** add obfuscators, packers (e.g. UPX), admin elevation, AV disablement, or SmartScreen bypasses.
3. Keep install/uninstall paths user-visible and limited to the documented cache directory (+ legacy restore).
4. Prefer clear Turkish or English UI/docs; security-facing docs should stay accurate in English where SignPath requires it.

## Development

1. Fork and branch from `main`.
2. Place local PatchData as described in `installer/PatchData/README.md`.
3. Build with the README instructions.
4. Open a pull request; describe behavior changes and risk impact.

## Review roles (for signed releases)

See `CODE_SIGNING_POLICY.md`:

- **Developer** — implements changes
- **Reviewer** — reviews PR before merge to release branch
- **Signing approver** — approves SignPath signing requests (after SignPath onboarding)

Fill real names/handles in that file when the GitHub org is ready.

## CI

GitHub Actions builds on `windows-latest`. Signing job remains gated by `SIGNPATH_ENABLED=true` and configured secrets.
