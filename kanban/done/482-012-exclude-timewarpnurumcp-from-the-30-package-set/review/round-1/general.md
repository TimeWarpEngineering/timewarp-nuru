# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit `502bca0a` vs parent (csproj pack gate, changelog, migration guide, readme, tools docs). Re-checked the release pack path (`source/Directory.Build.props`, `.timewarp/dev.jsonc`, `PackableProjectService`, `workflow-command.cs` push cross-check) and SDK 10.0.400 `NuGet.Build.Tasks.Pack.targets`.

## Summary

The project sets `IsPackable` false unless `-p:NuruMcpPack=true`. MSBuild evaluates that to `false` by default and `true` with the opt-in, which is what `PackableProjectService` and `dev release` use. There is no `checkVersionConfig.packages` override. SDK `GenerateNuspec` runs only when `IsPackable` is `true`, so `GeneratePackageOnBuild` staying `true` from `source/Directory.Build.props` does not emit a package. A Release build of the project finished with 0 warnings and left no `TimeWarp.Nuru.Mcp*` under `artifacts/packages`. Default `dotnet pack` exited 0 and wrote no nupkg. Opt-in pack wrote `TimeWarp.Nuru.Mcp.3.0.0-beta.78.nupkg`. Source stays in the solution and the dev build list. Changelog, migration guide, root readme, and the MCP tool page name the exclusion. `GetSyntax` / `GenerateHandler` are untouched. `skills/tw-nuru/SKILL.md` exists. The one gap is the tools overview install block, which this commit edited around but still presents a current install.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/user/tools/overview.md:33
- Description: The new banner under "MCP Server" says the tool is frozen and not part of 3.0, but the same page's highlights row and Installation section still tell the reader to install `TimeWarp.Nuru.Mcp` as a current global tool. The root readme in this commit qualifies that command as older prereleases. A reader who jumps to Installation does not get that qualifier, so the page this change edited still instructs a 3.0-era install of a package the release will not publish.
- Suggestion: Qualify the Installation subsection the same way as `readme.md` ("not part of the 3.0 release; older prereleases install with") and mark the highlights row as frozen / not part of 3.0.
- Status: open
