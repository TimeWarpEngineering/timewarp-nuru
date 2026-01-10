# Remove Mediator Dependency

## Summary

Remove the unused Mediator library dependency. The "mediator pattern" functionality in TimeWarp.Nuru is now accomplished via attributed routes and behaviors - we no longer use the external Mediator library.

## Background

The `timewarp-nuru-testapp-mediator` test app has compilation errors due to `Unit` type ambiguity between `Mediator.Unit` and `TimeWarp.Nuru.Unit`. Rather than fixing these errors, we should remove the Mediator dependency entirely since it's no longer used.

## Checklist

- [ ] Remove Mediator packages from `Directory.Packages.props`:
  ```xml
  <ItemGroup Label="Mediator - martinothamar source-generator based">
    <PackageVersion Include="Mediator.Abstractions" Version="3.0.1" />
    <PackageVersion Include="Mediator.SourceGenerator" Version="3.0.1" />
  </ItemGroup>
  ```
- [ ] Delete `tests/test-apps/timewarp-nuru-testapp-mediator/` directory
- [ ] Search for any other `using Mediator` references in the codebase
- [ ] Remove any `<PackageReference Include="Mediator.*">` from project files
- [ ] Verify full solution builds successfully
- [ ] Update solution file if needed (remove project reference)

## Notes

- The routing scenarios tested in `timewarp-nuru-testapp-mediator` (sub-commands, options, catch-all, etc.) are covered by samples and `timewarp-nuru-testapp-delegates`
- TimeWarp.Nuru has its own `Unit` type, so the external Mediator library is redundant
- This cleanup will fix the full solution build which currently fails on the mediator test app
