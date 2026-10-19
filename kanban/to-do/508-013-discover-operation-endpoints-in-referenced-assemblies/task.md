# Discover [Operation] endpoints in referenced assemblies

## Description

Filed from 508-006 (Roslynk CLI finding 6).

Endpoints are discovered only in the compiling project's syntax. So Roslynk compiles its operation files into its CLI as linked sources.

Discovering `[Operation]` request types in referenced assemblies (metadata symbols) would let a CLI reference a contracts assembly instead.

Concerns to cover:

- Property initializer defaults are not visible from metadata (no syntax).
- Nested handler visibility.
- The JSON context MSBuild task (`source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs`) parses only source files.
- Opt-in vs automatic discovery (performance, surprise routes).

## Checklist

- [ ] Decide the opt-in shape (for example an assembly attribute naming the contracts assembly, or `Map<T>()` on a metadata type)
- [ ] Extract bindings from metadata symbols
- [ ] Defaults strategy when no initializer syntax is available
- [ ] JSON context coverage
- [ ] Tests
- [ ] Docs

## Session

- Created: 4119648 (2026-10-19)

## Notes

Today's workaround is linked sources in the CLI project.
