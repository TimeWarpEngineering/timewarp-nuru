# Add Library Version to MCP Server

## Problem
Users interacting with the TimeWarp.Nuru MCP server cannot determine which version of the library the MCP server is providing information about.

## Current Behavior
- MCP server provides route pattern validation, syntax help, and examples
- No version information is exposed to users
- Users cannot verify if they're working with current library capabilities

## Expected Behavior
MCP server should expose the TimeWarp.Nuru library version number so users can:
1. Verify they're getting current information
2. Correlate MCP responses with library documentation
3. Troubleshoot version-specific issues

## Implementation Options

### Option 1: Add Version to Tool Descriptions
Include version in each tool's description metadata:
```typescript
description: "Validates a TimeWarp.Nuru route pattern (v2.1.0-beta.20)"
```

### Option 2: Create Dedicated Version Tool
Add new MCP tool: `get_library_version`
```typescript
{
  name: "mcp__timewarp-nuru__get_version",
  description: "Get the TimeWarp.Nuru library version",
  returns: { version: "2.1.0-beta.20", date: "2025-10-16" }
}
```

### Option 3: Include in Status Tool
Extend existing `cache_status` tool to include library version.

## Acceptance Criteria
- [ ] Users can determine TimeWarp.Nuru library version via MCP
- [ ] Version information is accurate and matches the library
- [ ] Version is easily discoverable (doesn't require deep inspection)
- [ ] Documentation updated to explain how to check version

## Related Files
- MCP server implementation (likely in an mcp-server directory or external repo)
- Package/library version source (Directory.Packages.props or .csproj)
