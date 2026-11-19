# Endpoint FileInfo, DirectoryInfo and IPAddress properties do not bind

## Description

`GetTypeConstraintFromClrType` maps `Version` but not `FileInfo`, `DirectoryInfo`, or `IPAddress`, although
fluent `{x:FileInfo}` constraints convert them through `TypeConversionMap`. An endpoint property of one of those
types does not bind. (Review M5 on task 496.)

## Goal (implementer restatement)

An endpoint property typed `FileInfo`, `DirectoryInfo`, or `IPAddress` takes its value from the argument
string, through the conversion `TypeConversionMap` already emits for a fluent `{name:FileInfo}` constraint.
`GetTypeConstraintFromClrType` maps those CLR names onto `fileinfo`, `directoryinfo`, and `ipaddress`. An
`[Operation]` property of one of those types is a scalar argv value, the same way `Uri` and `Version` are.

A nullable reference annotation is not a different type for that check. `IsScalar` compares the display of
the type with the annotation removed, for every reference type, so `FileInfo?`, `DirectoryInfo?`,
`IPAddress?`, `Uri?`, and `Version?` stay in the same scalar set as the non-nullable forms. Matching three
nullable names by hand leaves the other reference scalars classified as JSON members.

Done when:

- A `[NuruRoute]` `[Parameter]` or `[Option]` of each type binds, and a nullable one stays unset when the
  argument is omitted.
- A string `IPAddress.TryParse` rejects is an invalid-value error.
- An `[Operation]` property of each type binds from argv.
- A nullable `[Operation]` property of `FileInfo?`, `DirectoryInfo?`, `IPAddress?`, and `Uri?` binds when
  the option is given and stays unset when it is omitted.
- A test covers each type.
- The migration guide, the migrate skill, and the glossary say these properties bind, and they name the
  4.0.0-beta.14 behavior that did not.

## Checklist

- [x] Decide per item: fix all three (`FileInfo`, `DirectoryInfo`, `IPAddress`)
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then the new runfile. The full suite is the pull request CI
- [x] Review M1: `IsScalar` compares the unannotated display for every reference type, and `routing-44`
      covers nullable `[Operation]` `FileInfo?`, `DirectoryInfo?`, `IPAddress?`, and `Uri?`

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- Implementation: implementer oracle (2026-11-19)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f47b-9129-7813-9543-db3c81c2b493, max-turns 500, Done — 2026-11-19T16:06:57Z
- Review round 1: review oracle (claude, model claude-opus-5-5), effort 2, roster general, turn cap 120 —
  2026-11-19. 1 open suggestion (M1); fix pass requested. Artifacts: `review/review-framework.md`,
  `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 60ea781d-3829-4221-9b51-afd0d8e39f1c, max-turns 120, artifacts general.md, merged.md — 2026-11-19T16:09:06Z
- Fix pass (implement-fix-2, 2026-11-19): `IsScalar` drops a nullable reference annotation for every
  reference type before the display match. `routing-44` adds `t591007-optional` for `FileInfo?`,
  `DirectoryInfo?`, `IPAddress?`, and `Uri?`.

## Results

Accepted all three. `GetTypeConstraintFromClrType` now maps `FileInfo`, `DirectoryInfo`, and `IPAddress`
onto `fileinfo`, `directoryinfo`, and `ipaddress`, the constraint names `TypeConversionMap` already converts
for a fluent route. An `[Operation]` property of one of those types is a scalar, so it binds from argv
instead of as a JSON member.

Review M1: `IsScalar` no longer special-cases `FileInfo?`, `DirectoryInfo?`, and `IPAddress?`. When the
type is a reference type with a nullable annotation, the check uses
`WithNullableAnnotation(NullableAnnotation.NotAnnotated)` and compares that display. `Uri?` and `Version?`
stay in the same scalar set as `Uri` and `Version`. `routing-44` operation `t591007-optional` binds
nullable `FileInfo`, `DirectoryInfo`, `IPAddress`, and `Uri` when the options are given, and leaves them
unset when the command is run with no options. `Version?` takes the same unannotated-display path and has
no separate case in this runfile.

The migration guide's scan example uses `DirectoryInfo?` and `FileInfo?` directly. The guide, the migrate
skill, and `glossary.md` say these properties bind, and they name the 4.0.0-beta.14 behavior that did not.

`./bin/dev test` does not take a file argument in this repo. The test gate is `dotnet run --file`.
The new file is a standalone runfile (`#if !JARIBU_MULTI`). CI multi-mode includes it by the
`tests/timewarp-nuru-tests/**/*.cs` glob.

`ganda repo audit` exited 0 (44 passed, 1 skipped). One advisory warning, `vscode-window-icon`:
`.vscode/settings.json` is missing. That file is gitignored local IDE config. Not changed here.

### How to validate

Smoke: `dotnet run --file tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs`

Expect: 9 passed. `t591007-scan /tmp/scan-dir` prints `path:scan-dir|output:none|address:none`.
`t591007-scan --output /tmp/scan-out.txt` prints `output:scan-out.txt`. `t591007-read /etc/hosts` prints
`file:hosts`. `t591007-scan --address 192.168.1.1` prints `address:192.168.1.1`, and `--address ::1` prints
`address:::1`. `--address not-an-ip` exits 1 and the error contains `Expected: ipaddress`.
`t591007-scan` with no arguments prints `path:none|output:none|address:none`.
`t591007-place --Input /tmp/place-in.txt --Destination /tmp/place-out --Address 10.0.0.8` prints
`input:place-in.txt|destination:place-out|address:10.0.0.8`.
`t591007-optional --Input /tmp/opt-in.txt --Destination /tmp/opt-out --Address 10.1.2.3 --Location https://example.com/a`
prints `input:opt-in.txt|destination:opt-out|address:10.1.2.3|location:https://example.com/a`.
`t591007-optional` with no arguments prints `input:none|destination:none|address:none|location:none`.

Gates (fix pass, 2026-11-19):

- `./bin/dev build` — succeeded, 0 warnings
- `ganda runfile cache --clear` — cleared 353 entries
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs` — 9 passed
- `ganda repo audit` — exit 0; 44 passed, 1 skipped, 1 advisory `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local IDE config; not changed)

The first implement recorded the same build, a cache clear of 486 entries, this runfile at 7 passed, and
the same advisory audit. The runfile is a standalone `#if !JARIBU_MULTI` file. `./bin/dev test` does not
take a file argument here. CI multi-mode includes it by the `tests/timewarp-nuru-tests/**/*.cs` glob.

The runfile stays mode 100755. The first commit left it 100644 and `ganda repo attest` failed
`runfile-executable`. The follow-up commit marks it 100755. That commit's hook attested tree `0185ad2cf1cf`.

### Review

Round 1 (effort 2, roster general): 0 bug, 1 suggestion, 0 nit. M1 implemented on this fix pass.
`IsScalar` compares the unannotated display for every reference type. `routing-44` covers nullable
`[Operation]` `FileInfo?`, `DirectoryInfo?`, `IPAddress?`, and `Uri?`. Ledger: `review/round-1/merged.md`.
Disposition stays with review round 2.

## Review

- Round 1: M1 implemented on the fix pass. Review round 2 follows.

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
