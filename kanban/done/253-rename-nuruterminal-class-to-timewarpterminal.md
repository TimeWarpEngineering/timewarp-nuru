# Rename NuruTerminal class to TimeWarpTerminal

## Description

Rename the public sealed class `NuruTerminal : ITerminal` to `TimeWarpTerminal` to align with TimeWarp naming conventions and improve brand consistency.

## Checklist

- [ ] Find all instances of NuruTerminal class in the codebase
- [ ] Rename the class declaration from NuruTerminal to TimeWarpTerminal
- [ ] Update all references to use the new class name
- [ ] Update any documentation or comments that reference NuruTerminal
- [ ] Ensure all tests pass after the rename
- [ ] Update any public API documentation that mentions NuruTerminal

## Notes

This is a breaking change that will affect:
- Public API consumers
- Tests that reference NuruTerminal
- Documentation and samples
- Any derived classes or implementations

Make sure to search comprehensively for all references including:
- Using statements
- Type references in method signatures
- Generic type parameters
- Reflection-based code
- Configuration files
