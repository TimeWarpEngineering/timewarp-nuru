# Create Factory Method Preference Analyzer (NURU_D003)

## Description

Create an analyzer that suggests using factory methods instead of `new NuruAppBuilder()` to guide developers toward preferred patterns.

## Requirements

- Detect usage of `new NuruAppBuilder()` constructor
- Suggest using `NuruApp.CreateBuilder(args)` or `NuruApp.CreateSlimBuilder(args)` instead
- Severity: Info (suggestion, not error)

## Checklist

### Design
- [ ] Define diagnostic descriptor for NURU_D003
- [ ] Design detection logic for NuruAppBuilder constructor calls

### Implementation
- [ ] Create analyzer class in timewarp-nuru-analyzers project
- [ ] Implement NURU_D003: new NuruAppBuilder() usage detection
- [ ] Add code fix provider to convert to CreateBuilder
- [ ] Add code fix provider to convert to CreateSlimBuilder
- [ ] Ensure proper handling of various constructor overloads

### Testing
- [ ] Add unit tests for NURU_D003 scenarios
- [ ] Test code fix transformations
- [ ] Verify no false positives

### Documentation
- [ ] Document analyzer rule in developer reference

## Notes

Tags: analyzer, developer-experience

Diagnostic details:
- NURU_D003 (Info): "Consider using NuruApp.CreateBuilder(args) or NuruApp.CreateSlimBuilder(args) factory methods instead of new NuruAppBuilder()."
