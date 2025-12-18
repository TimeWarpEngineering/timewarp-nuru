# Update Attributed Route Basic Tests with Missing Coverage

## Description

Update the existing `attributed-route-generator-01-basic.cs` tests to add missing coverage for features that exist in the sample app but are not tested.

## Parent

150-implement-attributed-routes-phase-1

## Dependencies

- Task 186: Test utilities should be created first (to use shared helpers)

## Checklist

### Update Existing Test File
- [ ] Update `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-01-basic.cs`
- [ ] Refactor to use shared test helpers from Task 186

### Add Missing Coverage
- [ ] Test default route `[NuruRoute("")]` is registered
- [ ] Test catch-all parameter `[Parameter(IsCatchAll=true)]` is registered
- [ ] Test int/typed options are registered correctly
- [ ] Test typed parameters are registered correctly

### Verify Sample App Coverage
- [ ] Ensure every feature in `samples/attributed-routes/` has a corresponding test
- [ ] Document any gaps between sample and tests

## Notes

### Current Coverage Gaps

From sample app analysis:

| Feature | Sample Has | Test Has |
|---------|------------|----------|
| Default route `[NuruRoute("")]` | Yes (`DefaultRequest`) | No |
| Catch-all `IsCatchAll=true` | Yes (`ExecRequest`) | No |
| Int options | Yes (`DeployRequest.Replicas`) | No |
| Typed parameters | Implicit | Not verified |

### Reference Files

- Current tests: `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-01-basic.cs`
- Sample app: `samples/attributed-routes/attributed-routes.cs`
