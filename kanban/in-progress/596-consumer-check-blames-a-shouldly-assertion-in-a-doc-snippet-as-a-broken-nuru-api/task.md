# Consumer check blames a Shouldly assertion in a doc snippet as a broken Nuru API

## Description

The 4.0.0-beta.15 consumer check (`skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.15`)
passed (40 passed, 0 failed) and reported 2 DOC-DEFECT rows, both from one snippet:

```
documentation/user/guides/migrating-from-system-commandline.md#L624  broken
CS1061: 'bool' does not contain a definition for 'ShouldBeTrue' ...
```

The snippet is the `## Tests` example added by task 496. Its last line is
`terminal.OutputContains("Deploying").ShouldBeTrue();`. `ShouldBeTrue` is Shouldly, which the snippet
compile does not reference. No Nuru API is wrong. The beta.14 check reported 0 doc defects because the
guide was not in `v4.0.0-beta.14`.

## Cause

`nuru_errors` in `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh` (the last rule, "Missing
members") blames every `CS1061`/`CS0117` unless the receiver is one of seven host extension points
(`IServiceCollection`, `ILoggingBuilder`, ...). `bool` is not in that list, so a missing assertion
extension on `bool` counts as a Nuru API the package lacks, and the snippet is `broken` instead of
`fragment`.

Other snippets use the same assertion and are not flagged, because the compiler never reaches that line:

- `documentation/user/features/terminal-abstractions.md` (lines 601 to 671): no `using TimeWarp.Terminal;`,
  so `TestTerminal` is `CS0246` and `terminal` has no type.
- `documentation/user/guides/subset-publishing.md#L585`: the only errors reported are `CS0246` on `[Fact]`.

The migration guide snippet is the only one that includes `using TimeWarp.Terminal;` and puts the
assertion in top-level statements, so `OutputContains` binds to `bool` and `CS1061` appears. Adding
`using Shouldly;` to the snippet would not clear it: the using becomes `CS0246` and the `CS1061` stays.

Evidence: `artifacts/nuru-verify/4.0.0-beta.15/docs/snippets.tsv` line 85 and `docs/build.log`
(project `s-documentation-user-guides-migrating-from-system-commandline-md-624`).

## Fix

In `nuru_errors`, do not blame a `CS1061` whose receiver is a C# built-in type (`bool`, `string`, `int`,
and the like). A missing extension on one of those is an assertion or helper library the reader adds,
the same case as the host extension points already excluded. A missing member on a Nuru or
TimeWarp.Terminal type (`TestTerminal`, `ITerminal`, `NuruAppBuilder`) must still be blamed.

Also name Shouldly in the guide's `## Tests` section, in one sentence or a `using Shouldly;` line, so a
reader who copies the snippet knows where `ShouldBeTrue` comes from.

## Goal (implementer restatement)

The consumer check's doc-snippet classifier (`nuru_errors` in
`skills/tw-nuru-verify/scripts/nuru-consumer-check.sh`) must stop counting a missing member on a C#
built-in type (`bool`, `string`, `int`, ...) as a broken Nuru API. Those are assertion or helper
extensions the reader adds (here Shouldly's `ShouldBeTrue`), so the snippet is a `fragment`.

Acceptance:

- `CS1061` whose receiver is a built-in keyword type (optionally `?` or `[]`) is not blamed.
- `CS1061` on a package type (`TestTerminal`, `ITerminal`, `NuruAppBuilder`) is still blamed (shown).
- The migration guide's `## Tests` section names Shouldly as the source of `ShouldBeTrue`.
- Re-running the doc step for 4.0.0-beta.15 against the task branch reports 0 doc defects and the
  L624 snippet is `fragment`.

## Checklist

- [x] `nuru_errors` no longer blames `CS1061` on a built-in type receiver
- [x] A `CS1061` on `TestTerminal` or another package type is still `broken` (show one case)
- [x] The guide's `## Tests` section says the assertion is Shouldly
- [x] `nuru-consumer-check.sh --version 4.0.0-beta.15 --docs-ref <task branch>` (or the script's
      equivalent) reports 0 doc defects, and the L624 snippet is `fragment`

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-16)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 8b6163b4-ed04-4f23-9e84-d0676118d037, max-turns 500, Done — 2026-11-17T10:46:46Z
- Review oracle: general (grok), session 01a1e91d-eb5a-7e52-b7ff-20fe8aafdeb8, effort 2, round 1 (2026-11-17). Open finding M1. No disposition yet.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1e91d-eb5a-7e52-b7ff-20fe8aafdeb8, max-turns 120, artifacts general.md, merged.md — 2026-11-17T11:00:06Z
- Implement-fix-2 oracle (claude-opus-5-5), 2026-11-17: fixed review M1 in `skills/tw-nuru-verify/SKILL.md`.

## Results

- `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh`: the "Missing members" rule in `nuru_errors`
  now also drops `CS1061` whose receiver is a C# built-in keyword type (`bool`, `byte`, `sbyte`, `char`,
  `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`,
  `string`, optionally `?` or `[]`). `object` and `dynamic` are left out on purpose: a Nuru API that
  returns `object` and loses a member would still be blamed. `CS0117` on built-ins is unchanged (the
  task scoped the fix to `CS1061`).
- Review round 1 fix (M1): `skills/tw-nuru-verify/SKILL.md`, the `broken` bullet of the *docs* step,
  now states the same rule as `nuru_errors`: host extension points cover `CS1061` and `CS0117`;
  built-in keyword receivers (same list, optional `?`/`[]`) cover `CS1061` only; `object`, `dynamic`,
  and package types (`TestTerminal`, `ITerminal`, `NuruAppBuilder`) stay blamed.
- `documentation/user/guides/migrating-from-system-commandline.md` `## Tests`: one sentence says
  `ShouldBeTrue()` comes from the Shouldly package, and the snippet starts with `using Shouldly;`.
  The snippet moved from L624 to L626.

Classifier cases (the `nuru_errors` function extracted with sed and called directly):

| Errors | Result |
|--------|--------|
| L624 set: `CS0246 DeployCommand`, `CS1061 'bool' ... ShouldBeTrue`, `CS0246 Shouldly` | fragment |
| `CS1061: 'string[]' ... ShouldBe` / `CS1061: 'int?' ... ShouldBe` | fragment |
| `CS1061: 'TestTerminal' does not contain a definition for 'OutputContainz'` | **broken** |
| `CS1061: 'ITerminal' does not contain a definition for 'Foo'` | **broken** |
| `CS1061: 'object' ...` / `CS1061: 'boolean' ...` | **broken** |
| `CS1061: 'IServiceCollection' ...` (existing host exclusion) | fragment |

Consumer check against the task branch:
`nuru-consumer-check.sh --version 4.0.0-beta.15 --steps docs --docs-ref task/596-consumer-check-blames-a-shouldly-assertion-in-a --out /tmp/nv596`
→ `1 passed, 0 failed, 0 doc defects`; docs: 47 compile, 86 fragments, 133 total. The snippet
`migrating-from-system-commandline.md#L626` is `fragment` (first error: `CS0246` on `Shouldly`).

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Build succeeded, 0 warnings, 0 errors |
| Test | `./bin/dev test` | Exit 0. Multi-mode 2260 total, 2252 passed, 8 skipped, 0 failed; standalone 6/6 passed |
| Audit | `ganda repo audit` | Exit 0, repository passes; 1 advisory warning `vscode-window-icon` (`.vscode/settings.json` missing), pre-existing and non-blocking, not touched here |

Round-2 fix pass (after M1, skill doc edit only):

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Build completed successfully, 0 errors |
| Test | `./bin/dev test` | Exit 0. Multi-mode 2260 total, 2252 passed, 8 skipped, 0 failed; standalone 6/6 passed |
| Audit | `ganda repo audit` | Exit 0, repository passes; same pre-existing advisory warning `vscode-window-icon`, non-blocking |

### How to validate

Smoke:

```bash
bash skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.15 --steps docs \
  --docs-ref task/596-consumer-check-blames-a-shouldly-assertion-in-a --out /tmp/nv596
awk -F'\t' '$1 ~ /system-commandline-md-626/ {print $4}' /tmp/nv596/docs/snippets.tsv
```

Expect: the summary line ends `(1 passed, 0 failed, 0 doc defects)` and the awk prints `fragment`.
Before this change the beta.15 run reported 2 DOC-DEFECT rows from this snippet
(`CS1061: 'bool' does not contain a definition for 'ShouldBeTrue'`).

## Review

- Round 1: Blocked — open M1
- Round 1 fix pass: M1 fixed (SKILL.md `broken` bullet matches `nuru_errors`)

## Notes

Filed after the 4.0.0-beta.15 release. Doc defects do not hold promotion (tw-release step 4).
Separate from task 591, which covers product gaps the migration ports found.
