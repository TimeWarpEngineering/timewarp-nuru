# Fix broken tests using obsolete HelpProvider.GetHelpText API

## Description

Several test files reference `HelpProvider.GetHelpText()` which no longer exists. The API
has changed and tests need to be updated to use the current help generation approach.

## Error

```
error CS0117: 'HelpProvider' does not contain a definition for 'GetHelpText'
```

## Affected Files

- `tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs`
  - Line 75: `HelpProvider.GetHelpText(endpoints, "testapp", useColor: false)`
  - Line 164: Same call

## Checklist

- [ ] Investigate current help generation API
- [ ] Update test to use current API or mock help output
- [ ] Verify tests compile and pass

## Notes

Found during #341 migration. The `HelpProvider` class likely had its API changed or the
method was moved/renamed. Need to check what replaced `GetHelpText()`.
