# Make verify-samples treat warnings as failures

## Description

The `./runfiles/verify-samples.cs` script currently treats warnings as passing (shows ✅). Warnings should either cause failures or be prominently flagged so they don't go unnoticed.

## Checklist

- [ ] Update `verify-samples.cs` to capture and detect warning output
- [ ] Decide on behavior: fail on warnings OR show warning indicator (⚠️) 
- [ ] Update summary to report warning count separately from failures
- [ ] Test with samples that produce warnings

## Notes

Currently samples with warnings like `MSG0005: MediatorGenerator found message without any registered handler` show as passing. This masks potential issues that should be addressed.
