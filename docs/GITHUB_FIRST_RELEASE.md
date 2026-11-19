# First GitHub release (unsigned) + SignPath path

## 1. Create repository

1. Open GitHub in your browser.
2. Create empty public repo: **ROBRUN-TR**
3. Locally:

```powershell
cd D:\robrun
git add LICENSE README.md CONTRIBUTING.md SECURITY.md PRIVACY.md CODE_SIGNING_POLICY.md SIGNPATH_APPLICATION.md OPEN_SOURCE_AUDIT.md THIRD_PARTY_NOTICES.md .gitignore .github tools/tr_worker installer website
# Review `git status` — must NOT include ROB RUN.exe, bundles, ROBRUN_TR.exe, PFX
git status
git commit -m "Initial open-source layout for ROBRUN TR (SignPath-ready stubs)"
git branch -M main
git remote add origin https://github.com/<USER>/ROBRUN-TR.git
git push -u origin main
```

## 2. Actions

- Ensure Actions are enabled.
- Run workflow **Build ROBRUN_TR**.
- Download artifact `ROBRUN_TR-unsigned` (only if PatchData was available on the runner).

## 3. GitHub Release

```powershell
# After a green build with PatchData, attach EXE + SHA256SUMS.txt
gh release create v1.1.0 publish_out/ROBRUN_TR.exe installer/release/SHA256SUMS.txt --title "v1.1.0" --notes "Unsigned release. Authenticode via SignPath pending approval."
```

Mark clearly: **unsigned**.

## 4. SignPath

1. Complete `SIGNPATH_APPLICATION.md` links.
2. Resolve PatchData licensing gap.
3. Apply to SignPath OSS.
4. Install SignPath GitHub App; set secrets/vars; `SIGNPATH_ENABLED=true`.
5. Only then distribute **signed** builds + `SHA256SUMS-signed.txt`.
