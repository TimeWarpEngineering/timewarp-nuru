# Update TimeWarp.Jaribu for Terminal beta.4 compatibility

## Description

TimeWarp.Jaribu beta.8 is compiled against TimeWarp.Terminal beta.2 API. When updating to Terminal beta.4, Jaribu fails with MissingMethodException for WriteTable. Need to update Jaribu to a version compatible with Terminal beta.4.

This is a **blocking** issue for ongoing development.

## Checklist

- [ ] Identify the compatible version of TimeWarp.Jaribu for Terminal beta.4
- [ ] Update TimeWarp.Jaribu dependency in the project
- [ ] Verify tests pass after update
- [ ] Document the version compatibility requirement

## Notes

**Type:** Blocker task
**Tags:** blocking, dependencies

**Impact:** Prevents updating TimeWarp.Terminal to beta.4 due to binary incompatibility with current TimeWarp.Jaribu version.
