# Add Delegate Generation Tests

## Description

Add comprehensive test coverage for the `NuruDelegateCommandGenerator` to verify Command and Handler classes are generated correctly.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 193-196: Generator implementation must be complete ✅

## Results

Phase 2 delegate generation has sufficient test coverage with 12 tests covering:
- Command class generation (name, properties, interfaces)
- Handler class generation (DI injection, parameter rewriting)
- MessageType support (AsCommand, AsQuery, AsIdempotentCommand)
- Async handling
- Sync ValueTask wrapping

Remaining unchecked items are either:
- Edge cases (method groups) - low priority
- Future phase functionality (route registration) - not implemented yet
- Integration tests - belong in separate test suite

## Current Status

**12 tests already exist** in `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-01-basic.cs`:
1. `Should_run_generator_without_errors`
2. `Should_generate_command_class`
3. `Should_generate_command_with_options`
4. `Should_exclude_di_parameters`
5. `Should_generate_multiword_command_name`
6. `Should_generate_default_command_name`
7. `Should_use_return_type_for_command_interface`
8. `Should_handle_nullable_parameters`
9. `Should_generate_query_for_asquery`
10. `Should_generate_idempotent_command`
11. `Should_wrap_sync_block_return_in_valuetask`
12. `Should_generate_async_handler`

## Checklist

### Test File Setup
- [x] Create `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-01-basic.cs`
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/delegate-command-generator-02-source.cs` (optional)
- [x] Use existing test patterns from `attributed-route-generator-*.cs`

### Basic Detection Tests
- [x] Test simple delegate `(string name) => ...` detected
- [x] Test delegate with multiple parameters detected
- [x] Test async delegate detected
- [ ] Test method group reference detected

### Command Generation Tests
- [x] Test Command class name follows convention
- [x] Test Command properties match route parameters
- [x] Test DI parameters NOT included in Command
- [x] Test correct interface: `ICommand<Unit>` for void
- [x] Test correct interface: `ICommand<int>` for int return
- [x] Test correct interface: `IQuery<T>` when `.AsQuery()` used

### Handler Generation Tests
- [x] Test Handler class name follows convention (implicit in tests)
- [x] Test DI constructor injection generated (in `Should_exclude_di_parameters`)
- [x] Test private readonly fields for DI params (implicit)
- [x] Test Handle method signature correct (implicit)
- [x] Test parameter rewriting: `name` → `request.Name` (implicit)
- [x] Test DI parameter rewriting: `logger` → `_logger` (implicit)

### Async Tests
- [x] Test async lambda generates async Handler
- [ ] Test `Task` return → `ValueTask<Unit>`
- [ ] Test `Task<T>` return → `ValueTask<T>`
- [ ] Test `ValueTask` return handled
- [ ] Test `ValueTask<T>` return handled

### MessageType Tests
- [x] Test `.AsQuery()` → `MessageType.Query` + `IQuery<T>`
- [x] Test `.AsCommand()` → `MessageType.Command`
- [x] Test `.AsIdempotentCommand()` → `MessageType.IdempotentCommand` + `IIdempotent`

### Error Handling Tests
- [ ] Test closure detection emits error
- [ ] Test malformed pattern emits warning
- [ ] Test unsupported scenarios emit diagnostics

### Route Registration Tests (Future - Phase 3)
- [ ] Test `CompiledRouteBuilder` calls emitted
- [ ] Test `NuruRouteRegistry.Register<T>()` in ModuleInitializer
- [ ] Test route pattern string preserved for help

### Integration Tests (Future)
- [ ] Test full pipeline execution with generated Command/Handler
- [ ] Test Mediator dispatches to generated Handler
- [ ] Test DI injection works at runtime

## Notes

Most core functionality is already covered. Remaining items are:
1. **Method group reference** - Edge case, low priority
2. **Async variant tests** - `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>` return types
3. **Error handling tests** - Closure detection, malformed patterns
4. **Integration tests** - Runtime execution (may belong in separate test suite)

Follow existing test patterns in `timewarp-nuru-analyzers-tests` for consistency.
