# Update Solution File And Validate

## Description

Rename the solution file and update all project paths. Validate the entire solution builds.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `TimeWarp.Nuru.slnx` → `timewarp-nuru.slnx`
- Update all project paths in solution file
- Verify all projects load correctly
- Full solution build passes
- All tests pass

## Checklist

- [ ] Rename solution file to kebab-case
- [ ] Update all project paths in .slnx
- [ ] Run `dotnet build timewarp-nuru.slnx`
- [ ] Run full test suite
- [ ] Verify IDE (VS/Rider) loads solution correctly

## Notes

- HIGH RISK - final integration point
- Must be done after all project directories are renamed
- .slnx format may differ from traditional .sln
- This is the validation gate for the migration
