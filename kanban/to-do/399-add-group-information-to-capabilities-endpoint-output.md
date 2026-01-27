# Add group information to capabilities endpoint output

## Description

The `--capabilities` endpoint currently outputs commands as a flat list without reflecting the hierarchical group structure. TimeWarp.Nuru supports route groups via both `[NuruRouteGroup]` attributes and fluent API `.WithGroupPrefix()`, and the internal `RouteDefinition` model already captures `GroupPrefix` information. Enhance the capabilities output to include group structure and descriptions, making CLIs more organized and self-documenting.

## Checklist

- [ ] Analyze current capabilities endpoint generation code
- [ ] Design JSON schema for hierarchical group representation (with group name, description, and nested commands)
- [ ] Modify capabilities emitter to group routes by `RouteDefinition.GroupPrefix`
- [ ] Add support for group descriptions (may require extending IR model if not already present)
- [ ] Handle nested groups (e.g., "admin config" where "config" is nested under "admin")
- [ ] Preserve backward compatibility with existing flat command list (or provide both formats)
- [ ] Update tests to verify grouped output
- [ ] Update documentation with new capabilities JSON format
- [ ] Verify with ganda CLI (real-world Nuru app with many grouped commands)

## Notes

### Current State
- Commands like `repo avatar`, `repo base`, `workspace commits`, `kanban create` are output as flat list
- No indication that `repo`, `workspace`, `kanban` are logical groupings
- Tools consuming capabilities (like AI assistants) cannot understand command organization

### Technical Context
- `RouteDefinition` already has `GroupPrefix` field (line 16 in route-definition.cs)
- `FullPattern` property combines group prefix + pattern (line 69-71)
- Routes can be grouped: `appModel.Routes.GroupBy(r => r.GroupPrefix ?? "")`
- Both fluent API and attribute-based routing support groups

### Desired Output Format
```json
{
  "name": "app",
  "version": "1.0.0",
  "groups": [
    {
      "name": "repo",
      "description": "Repository management commands",
      "commands": [
        {
          "pattern": "repo avatar",
          "description": "Display the repository avatar",
          "messageType": "command",
          "parameters": [],
          "options": []
        }
      ]
    },
    {
      "name": "workspace",
      "description": "Workspace operations",
      "commands": [...]
    }
  ],
  "commands": [
    // Ungrouped commands (where GroupPrefix is null/empty)
  ]
}
```

### Investigation Needed
- Where is capabilities endpoint currently generated? (likely in emitters)
- Does IR model support group descriptions or just group names?
- How to handle multi-level nesting (e.g., "admin config" groups)?
- Should we also output flat list for backward compatibility?

### Related Files
- `source/timewarp-nuru-analyzers/generators/models/route-definition.cs` (GroupPrefix field)
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs` (Routes collection)
- Capabilities endpoint emitter (location TBD)

### Testing
- Verify with `ganda --capabilities` after implementation
- Test nested groups from fluent API
- Test attribute-based groups with `[NuruRouteGroup]`
- Ensure ungrouped commands still appear correctly
