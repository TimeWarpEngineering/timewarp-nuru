# Split C# files over the 500-line cap and restore TWA0038

## Description

TimeWarp.SourceGenerators 1.0.0-beta.15 (adopted in task 579) ships TWA0038: a 500-line cap per
file. 44 files are over it, including the generator core: dsl-interpreter.cs (2447),
json-args-emitter.cs (2296), endpoint-extractor.cs (2080), route-matcher-emitter.cs (1938),
interceptor-emitter.cs (1908), service-extractor.cs (1197), handler-invoker-emitter.cs (1108),
operation-endpoint-extractor.cs (1063), handler-extractor.cs (1046), nuru-generator.cs (848),
plus tools/dev-cli/endpoints/workflow-command.cs (1415) and tests/.../operation-endpoint-derivation.cs
(1544). 579 set `dotnet_diagnostic.TWA0038.severity = none` in `.editorconfig`.

## Requirements

- Split each over-cap file by responsibility (partial classes per concern for the emitters and
  extractors; test files by scenario group). No behavior change; generator snapshot tests stay green.
- Use the full list from `find source tools tests -name "*.cs" | xargs wc -l | awk "$1>500"`.
- Remove the TWA0038 override from `.editorconfig` so the package default (warning, error under
  warnings-as-errors) applies.
- Because generator and emitter work regresses under Grok fix passes, run this with the Claude
  implementer profile.

## Checklist

- [ ] Generator emitters and extractors split
- [ ] dev-cli workflow-command.cs split
- [ ] Test files split
- [ ] TWA0038 override removed, build clean, CI green

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)

## Notes

Follow-up of 579. Consider child tasks per project if one PR is too large to review.
