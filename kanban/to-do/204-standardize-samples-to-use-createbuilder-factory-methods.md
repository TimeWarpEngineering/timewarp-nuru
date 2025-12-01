# Standardize Samples to Use CreateBuilder Factory Methods

## Description

Migrate all samples from `new NuruAppBuilder()` to appropriate factory methods to establish consistent patterns across the codebase.

## Requirements

- Use `NuruApp.CreateBuilder(args)` for full-featured samples (mediator, config, repl, pipeline)
- Use `NuruApp.CreateSlimBuilder(args)` for minimal samples (hello-world, delegate, testing)
- ~20 sample files in /samples directory need updating

## Checklist

### Implementation
- [ ] Audit all sample files to identify which builder pattern each should use
- [ ] Update mediator-based samples to use `NuruApp.CreateBuilder(args)`
- [ ] Update config-based samples to use `NuruApp.CreateBuilder(args)`
- [ ] Update repl samples to use `NuruApp.CreateBuilder(args)`
- [ ] Update pipeline samples to use `NuruApp.CreateBuilder(args)`
- [ ] Update hello-world samples to use `NuruApp.CreateSlimBuilder(args)`
- [ ] Update delegate-based samples to use `NuruApp.CreateSlimBuilder(args)`
- [ ] Update testing samples to use `NuruApp.CreateSlimBuilder(args)`
- [ ] Verify all samples compile and run correctly

### Documentation
- [ ] Update any documentation referencing old patterns

## Notes

Tags: samples, refactoring, mcp
