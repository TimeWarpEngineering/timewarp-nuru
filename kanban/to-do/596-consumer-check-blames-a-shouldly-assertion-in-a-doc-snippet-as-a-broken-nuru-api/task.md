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

## Checklist

- [ ] `nuru_errors` no longer blames `CS1061` on a built-in type receiver
- [ ] A `CS1061` on `TestTerminal` or another package type is still `broken` (show one case)
- [ ] The guide's `## Tests` section says the assertion is Shouldly
- [ ] `nuru-consumer-check.sh --version 4.0.0-beta.15 --docs-ref <task branch>` (or the script's
      equivalent) reports 0 doc defects, and the L624 snippet is `fragment`

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-16)

## Notes

Filed after the 4.0.0-beta.15 release. Doc defects do not hold promotion (tw-release step 4).
Separate from task 591, which covers product gaps the migration ports found.
