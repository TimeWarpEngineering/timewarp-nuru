# Add shared dev-cli endpoints as source-only content

## Description

Add shared dev-cli endpoints (`clean`, `self-install`, `check-version`) as source-only content files that can be consumed by any TimeWarp repository via NuGet package.

This standardizes dev-cli across all TimeWarp repositories. Instead of each repo having its own copy of common commands, we're providing shared endpoints via a source-only NuGet package.

**GitHub Issue:** #200

## Checklist

- [ ] Create `content/any/endpoints/` directory structure
- [ ] Create `build/TimeWarp.Nuru.DevCli.props`
- [ ] Create `clean-command.cs` endpoint
- [ ] Create `self-install-command.cs` endpoint
- [ ] Create `check-version-command.cs` endpoint
- [ ] Update NuGet package metadata
- [ ] Publish TimeWarp.Nuru.DevCli.Shared package

## Notes

### Package Structure

```
TimeWarp.Nuru/
├── content/
│   └── any/
│       └── endpoints/
│           ├── clean-command.cs
│           ├── self-install-command.cs
│           └── check-version-command.cs
├── build/
│   └── TimeWarp.Nuru.DevCli.props
└── TimeWarp.Nuru.csproj
```

### Endpoints

1. **clean-command.cs** - Clean solution and build artifacts
   - Dependencies: `IRepoCleanService` from TimeWarp.Amuru

2. **self-install-command.cs** - AOT compile dev CLI to ./bin
   - Dependencies: None (standalone)

3. **check-version-command.cs** - Verify version is ready to release
   - Dependencies: `IRepoCheckVersionService` from TimeWarp.Amuru

### Dependencies

- TimeWarp.Amuru (for `IRepoCleanService`, `IRepoCheckVersionService`)
- TimeWarp.Terminal (for `ITerminal`)

### Related

- ganda task #117: Refactor dev-cli shared endpoints to TimeWarp.Nuru
- TimeWarp.Amuru issue #52: Add repo services for dev-cli shared endpoints
