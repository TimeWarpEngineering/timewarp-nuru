# Remove TimeWarp.Mediator References from Documentation

## Description

Nuru no longer uses TimeWarp.Mediator. The project migrated to martinothamar/Mediator (source-generator based) for better AOT support. All documentation and README files need to be updated to remove mentions of TimeWarp.Mediator.

## Files to Update

The following files contain references to TimeWarp.Mediator that need to be removed or updated:

1. **readme.md** (line 17) - Remove the commercial license comparison mentioning TimeWarp.Mediator
2. **claude.md** (line 76) - Remove "TimeWarp.Mediator integration" reference
3. **changelog.md** (line 54) - Update historical reference about TimeWarp.Mediator integration

## Checklist

### Implementation
- [ ] Update readme.md to remove TimeWarp.Mediator mention
- [ ] Update claude.md to remove TimeWarp.Mediator integration reference
- [ ] Update changelog.md entry (or add migration note)
- [ ] Search for any additional references that may have been missed
- [ ] Verify no broken links or references remain

### Documentation
- [ ] Ensure mediator references now point to martinothamar/Mediator where appropriate
- [ ] Verify consistency across all documentation

## Notes

- The migration from TimeWarp.Mediator to martinothamar/Mediator was completed in task 078-001
- Kanban done tasks contain historical references which should remain as-is (they document what happened)
- Some to-do tasks (008, 016) reference TimeWarp.Mediator and may also need updating
- Focus on user-facing documentation (readme, claude.md, changelog) rather than historical kanban records
