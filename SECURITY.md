# Security Policy

## Supported versions

Security fixes target the latest `main` branch and tagged releases of `ROBRUN_TR.exe`.

## What this software does

- Runs as a standard user (`asInvoker`).
- Verifies SHA-256 of known patch payloads via `integrity.json`.
- Writes Turkish localization data to the ROB RUN Addressables cache under LocalLow.
- Optionally checks `https://robrunturkceyama.com/release.json` for version/hash metadata over HTTPS.

## What it must not do

- Request unnecessary admin rights
- Disable Windows security features
- Forge or spoof Authenticode signatures
- Exfiltrate personal data
- Modify arbitrary paths outside the selected game root (read/validate) and documented cache/restore behavior

## Reporting a vulnerability

Email or contact the maintainer via the project site: https://robrunturkceyama.com/ / https://icestar.online/

Please include:

- Affected version / SHA-256 of the EXE
- Reproduction steps
- Impact assessment

Do not file public issues for undisclosed exploitable flaws until a fix or advisory is ready.

## Supply chain

- Prefer GitHub Actions artifacts built from this repository.
- After SignPath approval, only distribute **signed** builds produced by the trusted pipeline.
- Verify `SHA256SUMS.txt` published with each release.
