# Release 4.0.0-beta.14

## Description

Cut TimeWarp.Nuru 4.0.0-beta.14 from master after the generator-fixes stack and the dependency pins landed.

Ships:
- 583 generator null-checks on user value-type handler returns (CS0037/CS0023 fix)
- 584 custom converter constraint alias generates compilable code; new NURU_S010 build error for unknown or misspelled constraint aliases
- 585 anonymous-type handler returns no longer break the generated user-types JSON context
- 586 `{name:string}` route parameters bind without conversion
- 588 Microsoft.CodeAnalysis.CSharp.CodeStyle 5.12.0 and the IDE0028 sweep
- Pins: TimeWarp.Mediator and Mediator.Generators 15.0.0-beta.8, Contracts 2.0.0-beta.32, Builder 2.1.0-beta.1, Terminal 2.0.0-beta.2, SourceGenerators 1.0.0-beta.16, Microsoft.CodeAnalysis.CSharp and Analyzers 5.12.0 (Roslyn floor now 5.12)

## Checklist

- [x] Bump `<Version>` to 4.0.0-beta.14 in `source/Directory.Build.props`
- [ ] PR merged, master CI green
- [ ] `dev release --dry-run` then `dev release`
- [ ] release:published run green; all packages on nuget.org
- [ ] `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --version 4.0.0-beta.14`; summary pasted on the release

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-15)

## Notes

Behaviour change for consumers: NURU_S010 (from 584) turns a misspelled constraint alias into a build error where before it silently generated code that failed to compile.
