# Fix repo audit failures on master (bin-dev, capabilities, global-usings-analyzer, runfile exec bit and shebang)

## Description

`ganda repo audit` fails on timewarp-nuru master with five checks. Every recent task walk saw them,
wrote them off as pre-existing, and merged anyway. The post-commit hook refuses to sign an
attestation ("Blocking audit failures — not signing") but cannot block anything.

| Check | Failure | Auto-fixable |
|-------|---------|--------------|
| bin-dev | `bin/dev` is missing | yes |
| global-usings-analyzer | TimeWarp.SourceGenerators not pinned/referenced; `.editorconfig` missing `dotnet_diagnostic.TW0007.filename = global-usings.cs` | yes |
| runfile-executable | 7 shebang runfiles without the executable bit | yes |
| runfile-shebang | 1 runfile with a non-standard shebang (`documentation/developer/design/dsl/fluent-api-example.cs`) | yes |
| dev-cli-capabilities | cannot run `bin/dev --capabilities` | no — verify after bin-dev |

The flow-level fix (audit gate in the task-work routine so this never recurs) is tracked in ganda and
taratibu, not here.

## Requirements

1. Run `ganda repo audit --fix` in the claim worktree and commit the fixes.
2. Re-run `ganda repo audit`. If dev-cli-capabilities still fails, make `bin/dev --capabilities`
   expose the commands the check expects, or report exactly what the check wants and why it cannot be met.
3. The global-usings-analyzer fix adds an analyzer. Build the solution and run the CI test gate. Fix
   any new TW0007 diagnostics (file naming) it raises; do not suppress them.
4. `ganda repo audit` exits 0 at the end. Paste its final output into Results.

## Checklist

- [ ] `ganda repo audit --fix` applied and committed
- [ ] dev-cli-capabilities passing
- [ ] Build and CI test gate green with the new analyzer
- [ ] `ganda repo audit` clean

## Notes

- Implementer: **commit and push your product changes before reporting done.**
- Run the build and test gate in the foreground. You are one-shot and never receive background notifications.
- Do not touch `~/.timewarp/ganda/keys/`.
