# Add --capabilities Flag for AI Tool Discovery

## Description

Add a well-known `--capabilities` flag to Nuru CLIs that returns machine-readable JSON metadata about all commands. This enables AI tools (OpenCode, Claude, etc.) to discover CLI capabilities without MCP complexity.

## Parent

148-nuru-3-unified-route-pipeline

## Dependencies

- 156-add-iidempotent-interface-and-idempotency-metadata-to-routes (MessageType metadata)

## Background

Instead of wrapping Nuru CLIs in MCP (Model Context Protocol), AI tools can discover CLI capabilities via a well-known flag, similar to `--help` and `--version`.

See: `.agent/workspace/2024-12-15T14-30-00_unified-contracts-and-idempotency-analysis.md`

## Requirements

### Well-Known Conventions

| Flag | Purpose | Audience |
|------|---------|----------|
| `--help` | Human-readable usage | Humans |
| `--version` | Version information | Both |
| `-i` / `--interactive` | Enter REPL mode | Humans |
| `--capabilities` | Machine-readable metadata | AI agents |

### Output Format

JSON with commands, parameters, options, descriptions, and message types.

## Checklist

### Design
- [ ] Define `CapabilitiesResponse` model
- [ ] Define `CommandCapability` model
- [ ] Define `ParameterCapability` model
- [ ] Define `OptionCapability` model

### Implementation
- [ ] Add `--capabilities` as built-in route (like `--help`, `--version`)
- [ ] Implement `GetCapabilities()` method on app to gather route metadata
- [ ] Mark route as `.AsQuery().Hidden()` (hidden from `--help`)
- [ ] JSON serialization with proper formatting

### Integration
- [ ] Auto-register `--capabilities` in `NuruApp.Build()`
- [ ] Include app name, version, description from metadata
- [ ] Include all registered routes with full metadata

### Testing
- [ ] Unit tests for `CapabilitiesResponse` serialization
- [ ] Integration test verifying JSON output format
- [ ] Test that hidden routes are excluded
- [ ] Test that `--capabilities` itself is hidden from `--help`

## Notes

### Usage

```bash
$ mytool --capabilities
```

### Output Format

```json
{
  "name": "mytool",
  "version": "1.0.0",
  "description": "My CLI application",
  "commands": [
    {
      "pattern": "users list",
      "description": "List all users",
      "messageType": "query",
      "parameters": [],
      "options": [
        { "name": "format", "alias": "f", "type": "string", "required": false, "default": "table" }
      ]
    },
    {
      "pattern": "user create {name}",
      "description": "Create a new user",
      "messageType": "command",
      "parameters": [
        { "name": "name", "type": "string", "required": true }
      ],
      "options": [
        { "name": "email", "alias": "e", "type": "string", "required": false }
      ]
    }
  ]
}
```

### AI Decision Flow

```
User: "List all users and create a new one called Alice"

AI runs: mytool --capabilities

AI thinks:
  1. "users list" is query → run it freely
  2. "user create Alice" is command → ask first

AI: "I listed the users (found 3). Should I also create user 'Alice'?"
```

### Response Models

```csharp
public sealed class CapabilitiesResponse
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<CommandCapability> Commands { get; init; }
}

public sealed class CommandCapability
{
    public required string Pattern { get; init; }
    public string? Description { get; init; }
    public required string MessageType { get; init; }  // "query", "command", "idempotent-command"
    public required IReadOnlyList<ParameterCapability> Parameters { get; init; }
    public required IReadOnlyList<OptionCapability> Options { get; init; }
}
```

### OpenCode Integration

```yaml
# AI tool can auto-discover
tools:
  - command: mytool
    discover: true  # Runs `mytool --capabilities`
```
