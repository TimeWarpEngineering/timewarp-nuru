# Rename GlobalUsings.cs files to global-usings.cs

## Description

Rename all `GlobalUsings.cs` files to `global-usings.cs` to comply with the kebab-case file naming standard defined in `documentation/developer/standards/file-naming.md`.

## Checklist

- [ ] Rename `source/timewarp-nuru/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-nuru-parsing/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-nuru-core/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-nuru-analyzers/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-builder/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-nuru-repl/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-terminal/GlobalUsings.cs` to `global-usings.cs`
- [ ] Rename `source/timewarp-nuru-mcp/GlobalUsings.cs` to `global-usings.cs`
- [ ] Verify build succeeds after renaming
- [ ] Verify tests pass

## Notes

The file `source/timewarp-nuru-completion/global-usings.cs` already follows the correct naming convention.

Reference: `documentation/developer/standards/file-naming.md` shows `global-usings.cs` as the correct format (line 24) and `GlobalUsings.cs` as incorrect (line 35).
