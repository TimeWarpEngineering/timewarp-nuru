# V2 Generator - Core Endpoint Generation

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Implement V2 generator to emit the core runtime structures (routes, matchers, extractors, router). Adapt the work from sandbox/sourcegen into the real analyzer project.

## Checklist

- [ ] Move/adapt `RuntimeCodeEmitter` from sandbox to analyzer project
- [ ] Move/adapt extractors (FluentChainExtractor, DelegateAnalyzer, etc.) to analyzer project
- [ ] V2 generator analyzes `Map()` fluent chains
- [ ] V2 generator emits `GeneratedRoutes`, `Router`, matchers, extractors
- [ ] Re-run tests with `UseNewGen=true`, track progress on passing tests
- [ ] Document any tests that need V2-specific updates
