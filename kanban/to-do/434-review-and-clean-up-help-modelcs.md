# Review and clean up help-model.cs

## Description

The `HelpModel` record in `source/timewarp-nuru-analyzers/generators/models/help-model.cs` defines several properties that are **never used** by the help emitters. Decide whether to remove dead code, wire up existing properties, or redesign for future needs.

## Dead Properties

None of these are referenced in the emitters (`help-emitter.cs` or `route-help-emitter.cs`):

- `ShowHeader` - Whether to show application header
- `ShowUsage` - Whether to show usage line
- `ShowCommands` - Whether to show command list
- `ShowOptions` - Whether to show global options
- `GroupByCategory` - Whether to group commands by category
- `MaxWidth` - Maximum width for help output
- `IndentSize` - Number of spaces for indentation

## Where HelpModel is Referenced

- `app-model.cs:46` - `HelpModel? HelpOptions` property
- `ir-app-builder.cs:28` - stored as private field
- `ir-app-builder.cs:97` - assigned `HelpModel.Default`
- `generator-model.cs:87` - accessor property
- `iir-app-builder.cs:60` - interface method

## Checklist

- [ ] Decide approach: remove dead code, wire up properties, or redesign
- [ ] If removing: strip unused properties from `HelpModel`, update all references
- [ ] If wiring up: implement toggle behavior in `help-emitter.cs` and `route-help-emitter.cs`
- [ ] If redesigning: consider examples, custom headers/footers, parameter descriptions
- [ ] Update tests to cover any changes
- [ ] Verify CI passes

## Notes

- Help rendering is done at compile time by source generator emitters
- The emitters currently hardcode all sections without checking any `HelpModel` flags
- No support exists for examples, custom headers/footers, or parameter descriptions
- Related feature request: extend help with Examples section, Header, Footer
