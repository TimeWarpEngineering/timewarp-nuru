# Review and fix all CI tests for source generator solution

## Description

The CI test runner (`tests/ci-tests/run-ci-tests.cs`) currently fails due to obsolete references like `IRequest` that no longer exist in our source generator-based architecture. We need to review ALL test files to:

1. Determine if each test is still relevant for the source generator approach
2. Fix tests that are relevant but have outdated code
3. Remove or archive tests that are no longer applicable
4. Get all tests passing in the CI runner

Current error:
```
tests/timewarp-nuru-core-tests/nuru-route-registry-01-basic.cs(162,36): error CS0246: The type or namespace name 'IRequest' could not be found
```

## Checklist

### Phase 1: Fix Immediate Build Error
- [ ] Fix `nuru-route-registry-01-basic.cs` - remove/update `IRequest` references (lines 161-162)

### Phase 2: Core Tests Review (tests/timewarp-nuru-core-tests/)
- [ ] Review `nuru-route-registry-01-basic.cs` - evaluate relevance
- [ ] Review `compiled-route-builder-01-basic.cs`
- [ ] Review `compiled-route-test-helper.cs`
- [ ] Review `invoker-registry-01-basic.cs`
- [ ] Review `capabilities-01-basic.cs`
- [ ] Review `capabilities-02-integration.cs`
- [ ] Review `test-terminal-context-01-basic.cs`
- [ ] Review `nested-compiled-route-builder-01-basic.cs`

### Phase 3: Generator Tests (tests/timewarp-nuru-core-tests/generator/)
- [ ] Review `generator-01-intercept.cs`
- [ ] Review `generator-02-top-level-statements.cs`
- [ ] Review `generator-03-short-only-options.cs`
- [ ] Review `generator-04-static-service-injection.cs`
- [ ] Review `generator-05-return-await.cs`
- [ ] Review `generator-06-user-usings.cs`
- [ ] Review `generator-07-nullable-type-conversion.cs`
- [ ] Review `generator-08-typed-params-with-options.cs`
- [ ] Review `generator-09-optional-positional-params.cs`
- [ ] Review `generator-10-required-options.cs`
- [ ] Review `generator-11-attributed-routes.cs`
- [ ] Review `generator-12-method-reference-handlers.cs`
- [ ] Review `generator-13-ioptions-parameter-injection.cs`
- [ ] Review `generator-14-options-validation.cs`

### Phase 4: Lexer Tests (tests/timewarp-nuru-core-tests/lexer/)
- [ ] Review all lexer tests (15 files)

### Phase 5: Parser Tests (tests/timewarp-nuru-core-tests/parser/)
- [ ] Review all parser tests (15 files)

### Phase 6: Routing Tests (tests/timewarp-nuru-core-tests/routing/)
- [ ] Review all routing tests (20+ files)

### Phase 7: Widget/UI Tests
- [ ] Review table-widget tests (5 files)
- [ ] Review panel-widget tests (3 files)
- [ ] Review rule-widget tests (2 files)
- [ ] Review message-type tests (2 files)
- [ ] Review hyperlink tests (1 file)

### Phase 8: Analyzer Tests (tests/timewarp-nuru-analyzers-tests/)
- [ ] Review auto/ analyzer tests (9 files)
- [ ] Review interpreter/ tests (4 files)
- [ ] Review manual/ tests (2 files)

### Phase 9: Completion Tests (tests/timewarp-nuru-completion-tests/)
- [ ] Review static/ completion tests (13 files)
- [ ] Review dynamic/ completion tests (13 files)
- [ ] Review engine/ tests (3 files)

### Phase 10: REPL Tests (tests/timewarp-nuru-repl-tests/)
- [ ] Review main repl tests (35+ files)
- [ ] Review tab-completion/ tests (9 files)
- [ ] Review command-line-parser/ tests (2 files)

### Phase 11: MCP Tests (tests/timewarp-nuru-mcp-tests/)
- [ ] Review all MCP tests (6 files)

### Phase 12: Final Validation
- [ ] Run `./tests/ci-tests/run-ci-tests.cs` successfully
- [ ] Document any tests removed/archived with justification

## Notes

The source generator solution moved away from:
- `IRequest` interface pattern (MediatR-style)
- Runtime reflection-based route registration

New architecture uses:
- Source generators that emit route registration at compile time
- `NuruRouteRegistry` for storing generated routes
- `CompiledRoute` and `CompiledRouteBuilder` for route definitions

Test files discovered: 180+ .cs files across test directories

Key files to understand the current architecture:
- `source/timewarp-nuru/timewarp-nuru.csproj` (referenced by CI runner)
- `NuruRouteRegistry` class
- `CompiledRoute` and related builder classes
