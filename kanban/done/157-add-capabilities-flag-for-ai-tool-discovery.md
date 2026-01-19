# Add --capabilities Flag for AI Tool Discovery

## Description

Add a well-known `--capabilities` flag to Nuru CLIs that returns machine-readable JSON metadata about all commands. This enables AI tools (OpenCode, Claude, etc.) to discover CLI capabilities without MCP complexity.

## Dependencies

- ~~156-add-iidempotent-interface-and-idempotency-metadata-to-routes (MessageType metadata)~~ ✅ Complete

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
- [x] Define `CapabilitiesResponse` model (inline in emitter)
- [x] Define `CommandCapability` model (inline in emitter)
- [x] Define `ParameterCapability` model (inline in emitter)
- [x] Define `OptionCapability` model (inline in emitter)

### Implementation
- [x] Add `--capabilities` as built-in route (like `--help`, `--version`)
- [x] Implement capabilities gathering from route metadata
- [x] Mark route as `.AsQuery().Hidden()` (hidden from `--help`)
- [x] JSON serialization with proper formatting

### Integration
- [x] Auto-register `--capabilities` in `NuruApp.Build()`
- [x] Include app name, version, description from metadata
- [x] Include commitHash and commitDate from assembly metadata
- [x] Include all registered routes with full metadata
- [x] Removed dead code: `DisableCapabilitiesRoute` option

### Testing
- [x] Integration test verifying JSON output format
- [x] Test that hidden routes are excluded
- [x] Test query message type output
- [x] Test idempotent-command message type (kebab-case)
- [x] Test typed parameter info
- [x] Test option info with alias

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
