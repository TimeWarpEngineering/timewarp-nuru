# Rename GlobalUsings.cs files to global-usings.cs

## Description

Rename all `GlobalUsings.cs` files to `global-usings.cs` to comply with the kebab-case file naming standard defined in `documentation/developer/standards/file-naming.md`.

## Checklist

- [x] Update `.editorconfig` to configure GlobalUsingsAnalyzer to expect `global-usings.cs`
- [x] Rename `source/timewarp-nuru/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-nuru-parsing/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-nuru-core/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-nuru-analyzers/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-builder/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-nuru-repl/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-terminal/GlobalUsings.cs` to `global-usings.cs`
- [x] Rename `source/timewarp-nuru-mcp/GlobalUsings.cs` to `global-usings.cs`
- [x] Verify build succeeds after renaming
- [x] Verify tests pass

## Notes

The file `source/timewarp-nuru-completion/global-usings.cs` already follows the correct naming convention.

Reference: `documentation/developer/standards/file-naming.md` shows `global-usings.cs` as the correct format (line 24) and `GlobalUsings.cs` as incorrect (line 35).

The `GlobalUsingsAnalyzer` NuGet package (v1.4.0) is configurable via `.editorconfig`:
```
dotnet_diagnostic.GlobalUsingsAnalyzer0001.filename = global-usings.cs
```
This allows renaming without removing the analyzer's benefit of enforcing all usings go in the global usings file.
