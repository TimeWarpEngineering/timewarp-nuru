# Ship 3.0.0-beta.74 with enum-array repeated options (440) and keyword identifier escaping (460)

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Notes

[Additional context]

## Description

Cut TimeWarp.Nuru 3.0.0-beta.74 carrying the two stable-3.0.0 blockers fixed by Grok and
review-verified (tasks 440, 460), plus the CI standalone fix for generator-39. Standard
convention lever: bump PR → green CI (layout gate) → `dev release` promote → nuget.org
verify. Last beta planned before stable 3.0.0 (operator decision 2026-08-10: beta.74 first).

## Checklist

- [x] Bump source/Directory.Build.props to 3.0.0-beta.74
- [x] PR dev → master, auto-merge on green CI (PR #222, merged)
- [x] `dev release` from synced master worktree (tag v3.0.0-beta.74 at b4020b1)
- [x] Verify nuget.org package complete (build/net10.0 present)

## Session

- Implementation: Claude (2026-08-10)

## Results

**Shipped: TimeWarp.Nuru 3.0.0-beta.74 on nuget.org, complete** (5,172,574 B, all 10
build/net10.0 DLLs verified by downloading the published nupkg). Carries: enum-array
repeated options (440), keyword identifier escaping (460), generator-39 standalone CI fix.
Chain: PR #222 merged → master CI run 31390508753 green (package layout gate verified) →
guards 7/7 → tag v3.0.0-beta.74 at b4020b1 → release run 31391331689 succeeded
(tag-pin, attestation, promote, OIDC push). Last planned beta before stable 3.0.0.

### How to validate

```bash
curl -sfL https://api.nuget.org/v3-flatcontainer/timewarp.nuru/3.0.0-beta.74/timewarp.nuru.3.0.0-beta.74.nupkg -o n.nupkg && unzip -l n.nupkg | grep build/net10.0
```

**Expect:** 10 DLL entries including TimeWarp.Nuru.Build.dll. Feature checks:
`dotnet run tests/timewarp-nuru-tests/routing/routing-32-enum-repeated-options.cs` (11/11),
`dotnet run tests/timewarp-nuru-tests/generator/generator-39-keyword-param-identifiers.cs` (2/2).
