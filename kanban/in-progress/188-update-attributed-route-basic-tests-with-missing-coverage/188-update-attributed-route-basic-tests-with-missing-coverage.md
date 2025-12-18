# Update Attributed Route Basic Tests with Missing Coverage

## Description

Update the existing `attributed-route-generator-01-basic.cs` tests to add missing coverage for features that exist in the sample app but are not tested.

## Parent

150-implement-attributed-routes-phase-1

## Dependencies

- Task 186: Test utilities should be created first (to use shared helpers)

## Checklist

### Update Existing Test File
- [x] Update `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-01-basic.cs`
- [ ] Refactor to use shared test helpers from Task 186 (Task 186 not yet completed - no test utilities exist)

### Add Missing Coverage
- [x] Test default route `[NuruRoute("")]` is registered (Test 9: DefaultTestRequest)
- [x] Test catch-all parameter `[Parameter(IsCatchAll=true)]` is registered (Test 10: CatchAllTestRequest)
- [x] Test int/typed options are registered correctly (Test 11: TypedOptionTestRequest)
- [x] Test typed parameters are registered correctly (covered by CatchAllTestRequest with string[])

### Verify Sample App Coverage
- [x] Ensure every feature in `samples/attributed-routes/` has a corresponding test
- [x] Document any gaps between sample and tests (see updated table below)

### Bug Fixes
- [x] Fix Test 6: Pattern assertion used AST format `{value}` instead of display format `<value>`

## Notes

### Coverage Analysis (Updated)

From sample app analysis - all gaps now covered:

| Feature | Sample Has | Test Has | Status |
|---------|------------|----------|--------|
| Basic route | GreetQuery | SimpleTestRequest (Test 1) | Covered |
| Parameter | GreetQuery.Name | ParameterTestRequest (Test 2) | Covered |
| Bool option | DeployCommand.Force | OptionTestRequest (Test 3) | Covered |
| String option | DeployCommand.ConfigFile | OptionTestRequest (Test 3) | Covered |
| Aliases | GoodbyeCommand | AliasTestRequest (Test 4) | Covered |
| Route group | DockerGroupBase | TestGroupBase (Test 5) | Covered |
| Grouped route | DockerRunCommand | GroupedTestRequest (Test 5) | Covered |
| Default route `[NuruRoute("")]` | DefaultQuery | DefaultTestRequest (Test 9) | **Added** |
| Catch-all `IsCatchAll=true` | ExecCommand | CatchAllTestRequest (Test 10) | **Added** |
| Int options | DeployCommand.Replicas | TypedOptionTestRequest (Test 11) | **Added** |

### Reference Files

- Current tests: `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-01-basic.cs`
- Sample app: `samples/attributed-routes/attributed-routes.cs`

### Implementation Notes

- Task 186 test utilities do not exist yet - tests written without shared helpers
- Fixed pre-existing bug in Test 6 where pattern format was incorrect
