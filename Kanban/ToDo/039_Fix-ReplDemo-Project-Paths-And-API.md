# Fix ReplDemo Project Paths and API Usage

## Description

Fix broken project reference paths in repl-colored.cs and repl-with-hints.cs, and update all demos (except repl-basic-demo.cs) to use the recommended AddReplSupport() builder API pattern.

## Requirements

- Fix project paths in repl-colored.cs to use ../../Source/ prefix
- Fix project paths in repl-with-hints.cs to use ../../Source/ prefix
- Remove unnecessary TimeWarp.Nuru.Completion reference from repl-with-hints.cs
- Update repl-arrow-history.cs to use AddReplSupport() instead of passing ReplOptions to RunReplAsync()
- Update repl-colored.cs to use AddReplSupport() pattern
- Update repl-with-hints.cs to use AddReplSupport() pattern
- Verify all demos compile and run correctly

## Checklist

### Implementation
- [ ] Fix repl-colored.cs project paths
- [ ] Fix repl-with-hints.cs project paths
- [ ] Remove unnecessary Completion reference from repl-with-hints.cs
- [ ] Update repl-arrow-history.cs to use AddReplSupport()
- [ ] Update repl-colored.cs to use AddReplSupport()
- [ ] Update repl-with-hints.cs to use AddReplSupport()
- [ ] Verify all demos compile

## Notes

- repl-basic-demo.cs is already correct and should not be modified
- The correct project paths should be:
  - `#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj`
  - `#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj`
- Use repl-basic-demo.cs as the reference implementation for the AddReplSupport() pattern
