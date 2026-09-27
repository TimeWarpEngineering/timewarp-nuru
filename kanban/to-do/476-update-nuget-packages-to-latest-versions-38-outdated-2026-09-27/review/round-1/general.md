# Round 1 — general
**Date:** 2026-09-28
**Scope reviewed:** branch `task/476-update-nuget-packages-to-latest-versions-38-outdat` vs `origin/master` — `Directory.Packages.props`, plus the analyzer, build-task, and MCP call sites those versions feed.

## Summary

Central versions moved to latest stable except two recorded pins. `Microsoft.CodeAnalysis.CSharp` stays at 5.6.0; the analyzer and build projects restore that version, and the packed `build/net10.0/Microsoft.CodeAnalysis.CSharp.dll` matches the 5.6.0 net10.0 asset. `Microsoft.Build.Utilities.Core` is 18.9.6. NuGet's stable list after 18.7.1 is 18.8.2, 18.9.6, 18.10.1; 18.10.1 has `lib/net11.0` and `lib/net472` and no `lib/net10.0`, and its netcoreapp2.0 targets warn that net10.0 is unsupported. The packed Framework and Utilities DLLs match the 18.9.6 net10.0 assets. ICSharpCode.Decompiler 11.1.0.9782 still ships `lib/netstandard2.0`, and the types the decompiler uses (`UniversalAssemblyResolver`, `CSharpDecompiler`, `DecompileAsString`, `AddSearchDirectory`, and the four `DecompilerSettings` flags) are present. Both packed decompiler entries match that DLL. ModelContextProtocol 2.2.0 still exposes `AddMcpServer`, `WithStdioServerTransport`, `WithTools`, and `McpServerTool`; `program.cs` is unchanged. `dotnet build timewarp-nuru.slnx -c Release` is 0 warnings, 0 errors, and the packed `TimeWarp.Nuru.3.0.0-beta.78.nupkg` contains all 16 required payload entries. `ganda nuget outdated` reports the two pins only. No bugs, suggestions, or nits.

## Issues

<!-- none -->
