# Add README.md files for TimeWarp.Builder and TimeWarp.Terminal packages

## Description

Create dedicated README.md files for the newly extracted `TimeWarp.Builder` and `TimeWarp.Terminal` NuGet packages. These READMEs will be included in the NuGet packages and displayed on nuget.org.

## Checklist

### TimeWarp.Builder README

- [ ] Create `source/timewarp-builder/README.md`
- [ ] Document package purpose (fluent builder interfaces and scope extensions)
- [ ] Document key interfaces:
  - `IBuilder<T>` - standalone builders that create objects via `Build()`
  - `INestedBuilder<TParent>` - nested builders that return to parent via `Done()`
- [ ] Document scope extension methods:
  - `Also()` - side effects during chaining
  - `Apply()` - configuration with return
  - `Let()` - transformation to different type
  - `Run()` - terminal action
- [ ] Add usage examples

### TimeWarp.Terminal README

- [ ] Create `source/timewarp-terminal/README.md`
- [ ] Document package purpose (terminal abstractions and widgets)
- [ ] Document key interfaces:
  - `IConsole` - basic console I/O abstraction
  - `ITerminal` - extended terminal with cursor, colors, hyperlinks
- [ ] Document implementations:
  - `NuruConsole` / `NuruTerminal` - production implementations
  - `TestConsole` / `TestTerminal` - test implementations
- [ ] Document widgets: Panel, Table, Rule
- [ ] Document ANSI color support
- [ ] Add usage examples

### Verification

- [ ] Build solution to ensure READMEs are packaged correctly
- [ ] Verify README content matches package functionality

## Notes

### Reference

See `source/timewarp-nuru-logging/README.md` for existing README style and format.

### Package Configuration

Both packages already include the repository root `readme.md` via:
```xml
<None Include="$(RepositoryRoot)readme.md" Pack="true" PackagePath="" />
```

The package-specific README should be added similarly or replace the generic one.
