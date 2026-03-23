# Add shared dev-cli endpoints as source-only content

## Description

Add shared dev-cli endpoints (`clean`, `self-install`, `check-version`) as source-only content files that can be consumed by any TimeWarp repository via NuGet package.

This standardizes dev-cli across all TimeWarp repositories. Instead of each repo having its own copy of common commands, we're providing shared endpoints via a source-only NuGet package.

**This also serves as a reference example** for how to create reusable endpoint packages for Nuru, since source-only packages are required due to source generator constraints.

**GitHub Issue:** #200

## Checklist

- [x] Create `content/any/endpoints/` directory structure
- [x] Create `build/TimeWarp.Nuru.DevCli.props`
- [x] Create `clean-command.cs` endpoint
- [x] Create `self-install-command.cs` endpoint
- [x] Create `check-version-command.cs` endpoint
- [x] Create separate `TimeWarp.Nuru.DevCli` package
- [x] Add readme with usage instructions
- [ ] Publish TimeWarp.Nuru.DevCli package (requires user action)

## Notes

### Package Structure

Created as a **separate package** `TimeWarp.Nuru.DevCli`:

```
source/timewarp-nuru-devcli/
├── content/
│   └── any/
│       └── endpoints/
│           ├── clean-command.cs
│           ├── self-install-command.cs
│           └── check-version-command.cs
├── build/
│   └── TimeWarp.Nuru.DevCli.props
├── readme.md
└── timewarp-nuru-devcli.csproj
```

### Why Separate Package?

1. **Clean separation** - Main TimeWarp.Nuru package stays focused on the CLI framework
2. **Reference example** - Demonstrates how to create source-only endpoint packages for Nuru
3. **Opt-in** - Users only install if they need dev-cli endpoints

### Why Source-Only?

Nuru uses source generators for route matching. The generator needs to see endpoint class definitions at compile time. Traditional NuGet packages with compiled DLLs hide the source from the generator, so **only source-only content packages work**.

### Endpoints

1. **clean-command.cs** - Clean solution and build artifacts
   - Dependencies: `IRepoCleanService` from TimeWarp.Amuru

2. **self-install-command.cs** - AOT compile dev CLI to ./bin
   - Dependencies: None (standalone)

3. **check-version-command.cs** - Verify version is ready to release
   - Dependencies: `IRepoCheckVersionService` from TimeWarp.Amuru

### Dependencies

- TimeWarp.Nuru (CLI framework)
- TimeWarp.Amuru 1.0.0-beta.22+ (for repo services)
- TimeWarp.Terminal (for ITerminal)

### Related

- ganda task #117: Refactor dev-cli shared endpoints to TimeWarp.Nuru
- TimeWarp.Amuru issue #52: Add repo services for dev-cli shared endpoints
