# Endpoint FileInfo, DirectoryInfo and IPAddress properties do not bind

## Description

`GetTypeConstraintFromClrType` maps `Version` but not `FileInfo`, `DirectoryInfo`, or `IPAddress`, although
fluent `{x:FileInfo}` constraints convert them through `TypeConversionMap`. An endpoint property of one of those
types does not bind. (Review M5 on task 496.)

## Goal (implementer restatement)

An endpoint property typed as `FileInfo`, `DirectoryInfo`, or `IPAddress` should take its value from the
argument string, using the conversion `TypeConversionMap` already emits for a fluent `{name:FileInfo}`
constraint. `GetTypeConstraintFromClrType` maps `Version` and `Uri` onto those constraint names and returns
null for these three, so the generator never converts them. The same three are missing from the `[Operation]`
scalar set, so an operation property of one of those types is treated as a JSON member instead of an argv value.

Accept all three. Do not keep the old behavior.

Done when:

- A `[NuruRoute]` `[Parameter]` or `[Option]` of each type binds, and a nullable one stays unset when the
  argument is omitted.
- A string `IPAddress.TryParse` rejects is an invalid-value error.
- An `[Operation]` property of each type binds from argv.
- A test covers each type.
- The migration guide, the migrate skill, and the glossary say these properties bind, and they name the
  4.0.0-beta.14 behavior that did not.

## Checklist

- [x] Decide per item: fix all three (`FileInfo`, `DirectoryInfo`, `IPAddress`)
- [x] Fix with tests for each accepted item
- [x] Update "Behavior differences to check" in `documentation/user/guides/migrating-from-system-commandline.md`
      and `skills/tw-nuru-migrate-system-commandline/SKILL.md` for each fix
- [x] `ganda runfile cache --clear`, then the new runfile. The full suite is the pull request CI

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-17), split from 591 during groom
- Implementation: implementer oracle (2026-11-19)

## Results

Accepted all three. `GetTypeConstraintFromClrType` now maps `FileInfo`, `DirectoryInfo`, and `IPAddress`
onto `fileinfo`, `directoryinfo`, and `ipaddress`, the constraint names `TypeConversionMap` already converts
for a fluent route. An `[Operation]` property of one of those types is a scalar, so it binds from argv
instead of as a JSON member. Nullable reference annotations on those three operation types are stripped
before the scalar check. Other nullable displays are unchanged.

The migration guide's scan example uses `DirectoryInfo?` and `FileInfo?` directly. The guide, the migrate
skill, and `glossary.md` say these properties bind, and they name the 4.0.0-beta.14 behavior that did not.

`./bin/dev test` does not take a file argument in this repo. The test gate is `dotnet run --file`.
The new file is a standalone runfile (`#if !JARIBU_MULTI`). CI multi-mode includes it by the
`tests/timewarp-nuru-tests/**/*.cs` glob.

`ganda repo audit` exited 0 (44 passed, 1 skipped). One advisory warning, `vscode-window-icon`:
`.vscode/settings.json` is missing. That file is gitignored local IDE config. Not changed here.

### How to validate

Smoke: `dotnet run --file tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs`

Expect: 7 passed. `t591007-scan /tmp/scan-dir` prints `path:scan-dir|output:none|address:none`.
`t591007-scan --output /tmp/scan-out.txt` prints `output:scan-out.txt`. `t591007-read /etc/hosts` prints
`file:hosts`. `t591007-scan --address 192.168.1.1` prints `address:192.168.1.1`, and `--address ::1` prints
`address:::1`. `--address not-an-ip` exits 1 and the error contains `Expected: ipaddress`.
`t591007-scan` with no arguments prints `path:none|output:none|address:none`.
`t591007-place --Input /tmp/place-in.txt --Destination /tmp/place-out --Address 10.0.0.8` prints
`input:place-in.txt|destination:place-out|address:10.0.0.8`.

Gates:

- `./bin/dev build` — succeeded, 0 warnings
- `ganda runfile cache --clear` — cleared 486 entries
- `dotnet run --file tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs` — 7 passed
- `ganda repo audit` — exit 0; advisory `vscode-window-icon` only

The new runfile has a shebang. The first commit left it mode 100644, and `ganda repo attest` then failed `runfile-executable`. The follow-up commit marks `tests/timewarp-nuru-tests/routing/routing-44-endpoint-file-directory-ip.cs` mode 100755. That commit's hook attested tree `0185ad2cf1cf`.

## Notes

Found by task 496 (System.CommandLine migration guide) against TimeWarp.Nuru 4.0.0-beta.14. Parent: 591.
