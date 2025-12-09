# Fix Wrong Case #:project Directive References

## Description

Multiple sample files contain `#:project` directives with incorrect casing that doesn't match the actual directory structure. These cause build failures on case-sensitive file systems (Linux).

The pattern uses `Source/TimeWarp.Nuru/` instead of `source/timewarp-nuru/` (and similar variations).

## Requirements

- All `#:project` directives must use correct lowercase paths matching actual directory structure
- All affected samples must build successfully with `dotnet clean` and `dotnet run`

## Checklist

### Implementation
- [ ] Fix `samples/dynamic-completion-example/dynamic-completion-example.cs` (2 refs)
- [ ] Fix `samples/aspire-telemetry/aspire-telemetry.cs` (2 refs)
- [ ] Fix `samples/aspire-host-otel/nuru-client.cs` (3 refs)
- [ ] Fix `samples/shell-completion-example/shell-completion-example.cs` (2 refs)
- [ ] Fix `samples/builtin-types-example.cs` (1 ref)
- [ ] Fix `samples/custom-type-converter-example.cs` (1 ref)
- [ ] Fix `samples/syntax-examples.cs` (1 ref)
- [ ] Verify all samples build with `dotnet clean`

### Documentation
- [ ] Update `samples/shell-completion-example/overview.md` (lines 23-24, 323-324)
- [ ] Update `samples/configuration/user-secrets-readme.md` (line 11)

## Notes

### Pattern Mapping Reference

| Old Pattern | New Pattern |
|-------------|-------------|
| `Source/` | `source/` |
| `TimeWarp.Nuru/` | `timewarp-nuru/` |
| `TimeWarp.Nuru.Completion/` | `timewarp-nuru-completion/` |
| `TimeWarp.Nuru.Repl/` | `timewarp-nuru-repl/` |
| `TimeWarp.Nuru.Telemetry/` | `timewarp-nuru-telemetry/` |
| `TimeWarp.Nuru.Sample/` | `timewarp-nuru-sample/` |

### Already Fixed
- `samples/calculator/calc-createbuilder.cs` was fixed separately

### Analysis Report
Full analysis available at `.agent/workspace/2024-12-09T20-30-00_wrong-case-project-references.md`
