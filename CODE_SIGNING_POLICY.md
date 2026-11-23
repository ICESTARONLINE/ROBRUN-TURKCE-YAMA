# Code Signing Policy — ROBRUN TR

This policy describes how **real** Authenticode signing will be handled if/when the project is accepted by [SignPath](https://signpath.org/) (or another legitimate CA/signing service).

**Status:** Policy + CI stubs only. **No signing is active. No certificate is claimed.**

## Roles (fill with real people after GitHub org setup)

| Role | Name / GitHub | Duties |
|------|---------------|--------|
| **Project owner** | _TBD_ | Overall responsibility, SignPath applicant |
| **Developer** | _TBD_ | Source changes, CI green builds |
| **Reviewer** | _TBD_ | PR review before merge to signing branch |
| **Signing approver** | _TBD_ | Approve SignPath signing requests; must not rubber-stamp |

Separation: the person who submits a signing request should not be the sole approver when SignPath/policy requires dual control.

## Source of signed binaries

Signed `ROBRUN_TR.exe` must originate from:

1. Public GitHub repository source (this project)
2. GitHub-hosted Actions runners (`windows-latest`)
3. Artifact uploaded by the workflow, then submitted to SignPath via their GitHub action
4. Signed output published as a GitHub Release **without post-sign modification**

Re-packing, re-hashing content inside, or “fixing” a signed EXE after SignPath returns it is **forbidden**.

## Privacy

See `PRIVACY.md`. Signing providers may process organization identity and build metadata under their terms ([SignPath Terms](https://signpath.org/terms)).

## Verification after signing

```powershell
signtool verify /pa /v .\ROBRUN_TR.exe
Get-FileHash .\ROBRUN_TR.exe -Algorithm SHA256
```

Publish a separate `SHA256SUMS-signed.txt` for the signed artifact. Never reuse the unsigned hash.

## Forbidden practices

- Self-signed certs presented as “Trusted”
- Stolen/leased certificates
- SmartScreen bypass / reputation fraud services
- Disabling signature verification for users

## CI gate

Workflow signing job runs only when:

- `vars.SIGNPATH_ENABLED == 'true'`
- Secrets `SIGNPATH_API_TOKEN` and vars for organization/project/policy slugs are set
- SignPath onboarding is complete

Until then, CI produces **unsigned** artifacts only.
