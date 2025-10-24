# Add Version Info Tool to MCP Server

## Overview

Add an MCP tool that provides TimeWarp.Nuru version information including version number, git commit hash, and build date from Assembly metadata.

## Requirements

### Version Information Tool

Create a new MCP tool that returns:
- **Version Number** - From assembly version (e.g., "2.1.0-beta.24")
- **Git Commit Hash** - Short or full commit SHA
- **Build Date** - When the assembly was built
- **Git Branch** - Optional: branch name if available

### Implementation Details

1. **Tool Definition**
   - Name: `GetVersionInfo` or similar
   - Description: "Get TimeWarp.Nuru version, commit hash, and build information"
   - No parameters required
   - Returns formatted string with all version details

2. **Assembly Metadata**
   - Use `AssemblyInformationalVersionAttribute` for full version string
   - Use `AssemblyVersionAttribute` for version number
   - Store git commit hash in custom assembly attribute or build property
   - Store build date in assembly metadata

3. **Build Integration**
   - Configure MSBuild to inject git information during build
   - Consider using `MinVer` or similar tool for git-based versioning
   - Ensure information is available in both Debug and Release builds

4. **Output Format**

```
TimeWarp.Nuru Version Information
==================================
Version: 2.1.0-beta.24
Commit:  4142400f
Date:    2025-10-24T03:08:00Z
Branch:  master
```

## Technical Considerations

### Assembly Attributes

```csharp
// In AssemblyInfo.cs or Directory.Build.props
[assembly: AssemblyInformationalVersion("2.1.0-beta.24+4142400")]
[assembly: AssemblyMetadata("GitCommitHash", "4142400f")]
[assembly: AssemblyMetadata("BuildDate", "2025-10-24T03:08:00Z")]
```

### MSBuild Properties

```xml
<PropertyGroup>
  <!-- Enable source link which includes git info -->
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>

  <!-- Custom git properties -->
  <GitCommitHash Condition="'$(GitCommitHash)' == ''">$([System.DateTime]::UtcNow.ToString(yyyyMMddHHmmss))</GitCommitHash>
  <BuildDate>$([System.DateTime]::UtcNow.ToString(o))</BuildDate>
</PropertyGroup>
```

### Reading Assembly Metadata

```csharp
var assembly = typeof(NuruApp).Assembly;
var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
var commitHash = assembly.GetCustomAttribute<AssemblyMetadataAttribute>("GitCommitHash")?.Value;
var buildDate = assembly.GetCustomAttribute<AssemblyMetadataAttribute>("BuildDate")?.Value;
```

## Benefits

1. **Diagnostics** - Users can report exact version when filing issues
2. **Debugging** - Developers know exact commit being run
3. **Traceability** - Link runtime behavior to specific git commits
4. **MCP Integration** - Claude can check version compatibility and suggest upgrades

## Related Files

- `Source/TimeWarp.Nuru.Mcp/Tools/GetVersionInfoTool.cs` (new)
- `Source/Directory.Build.props` (modify to add git info)
- `Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj` (possibly modify)

## Acceptance Criteria

- [ ] New MCP tool `GetVersionInfo` callable from Claude
- [ ] Returns version number from assembly
- [ ] Returns git commit hash from assembly metadata
- [ ] Returns build date from assembly metadata
- [ ] Git information automatically injected during build
- [ ] Works in both local builds and CI/CD
- [ ] Formatted output is clear and readable
- [ ] Tool listed in MCP tools documentation
