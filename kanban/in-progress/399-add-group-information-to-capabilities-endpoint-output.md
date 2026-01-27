# Add group information to capabilities endpoint output

## Description

The `--capabilities` endpoint currently outputs commands as a flat list without reflecting the hierarchical group structure. TimeWarp.Nuru supports route groups via both `[NuruRouteGroup]` attributes and fluent API `.WithGroupPrefix()`, and the internal `RouteDefinition` model already captures `GroupPrefix` information. Enhance the capabilities output to include group structure and descriptions, making CLIs more organized and self-documenting.

## Checklist

- [ ] Analyze current capabilities endpoint generation code
- [ ] Design JSON schema for hierarchical group representation (with group name, description, and nested commands)
- [ ] Modify capabilities emitter to group routes by `RouteDefinition.GroupPrefix`
- [ ] Add support for group descriptions (may require extending IR model if not already present)
- [ ] Handle nested groups (e.g., "admin config" where "config" is nested under "admin")
- [ ] Ensure commands appear ONLY in their group OR in top-level commands array (no duplication)
- [ ] Update tests to verify grouped output
- [ ] Update documentation with new capabilities JSON format
- [ ] Verify with ganda CLI (real-world Nuru app with many grouped commands)

## Notes

# Comprehensive Implementation Plan: Add Group Information to Capabilities Endpoint

## Executive Summary

Transform the capabilities endpoint from a flat command list to a hierarchical JSON structure that reflects route groups. Groups will be represented hierarchically with nested groups inside parent groups. Commands appear ONLY in their group or at top-level (no duplication). This is a **breaking change** to the capabilities format.

## Key Decisions

Based on clarifications:
- ✅ **Skip descriptions for groups** (Phase 1) - focus on structure first
- ✅ **Hierarchical nesting** - groups contain nested groups
- ✅ **Show empty parent groups** - include all groups in hierarchy even if no direct commands
- ✅ **Breaking change OK** - move grouped commands out of top-level array
- ✅ **Single-word group constraint** - GroupPrefix must be single word (like NuruRoute), multi-level via nesting only

## Current State Analysis

### What Works Today
1. `RouteDefinition.GroupPrefix` captures group prefix (line 31 in route-definition.cs)
2. `RouteDefinition.FullPattern` combines prefix + pattern (line 69-71)
3. Both `[NuruRouteGroup]` and `.WithGroupPrefix()` set GroupPrefix
4. Capabilities emitter outputs flat command list (capabilities-emitter.cs:80-93)

### What's Missing
1. No hierarchical group structure in capabilities JSON
2. Commands with groups duplicated in flat list (wastes tokens)
3. No way to understand command organization from capabilities output
4. Nested groups like "admin config get" not represented as hierarchy

## Technical Approach

### Phase 1: Data Model & IR Enhancement

**Goal**: Create models to represent hierarchical group structure

**Files to Create**:
- `source/timewarp-nuru-analyzers/generators/models/group-definition.cs`
  ```csharp
  /// <summary>
  /// Represents a route group with optional nested groups and commands.
  /// </summary>
  public sealed record GroupDefinition(
    string Name,                                    // Single word: "admin", "docker", "config"
    ImmutableArray<GroupDefinition> NestedGroups,   // Child groups
    ImmutableArray<RouteDefinition> Commands);      // Commands directly in this group
  ```

**Files to Modify**:
- `source/timewarp-nuru/capabilities/capabilities-response.cs`
  - Add `GroupCapability` class with nested structure
  - Modify `CapabilitiesResponse` to include `Groups` array
  - Keep `Commands` array for ungrouped routes only

  ```csharp
  internal sealed class CapabilitiesResponse
  {
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }

    // NEW: Hierarchical groups
    public IReadOnlyList<GroupCapability>? Groups { get; init; }

    // Commands WITHOUT group prefix (ungrouped only)
    public required IReadOnlyList<CommandCapability> Commands { get; init; }
  }

  internal sealed class GroupCapability
  {
    public required string Name { get; init; }           // "admin", "config"
    public IReadOnlyList<GroupCapability>? Groups { get; init; }  // Nested groups
    public required IReadOnlyList<CommandCapability> Commands { get; init; }
  }
  ```

**Validation**:
- Add analyzer to enforce single-word group names (like NURU037 for NuruRoute)
- Diagnostic: "Group prefix must be a single word. Use WithGroupPrefix() nesting for multi-level hierarchy."

### Phase 2: Group Hierarchy Builder

**Goal**: Build hierarchical tree from flat list of routes with GroupPrefix

**Files to Create**:
- `source/timewarp-nuru-analyzers/generators/utils/group-hierarchy-builder.cs`

**Algorithm**:
```csharp
/// <summary>
/// Builds hierarchical group structure from routes with space-separated GroupPrefix values.
/// Example: "admin config get" has GroupPrefix="admin config" → admin > config > command
/// </summary>
internal static class GroupHierarchyBuilder
{
  public static (ImmutableArray<GroupDefinition> Groups, ImmutableArray<RouteDefinition> UngroupedCommands)
    BuildHierarchy(ImmutableArray<RouteDefinition> allRoutes)
  {
    // 1. Separate ungrouped vs grouped routes
    var ungrouped = routes where GroupPrefix is null/empty
    var grouped = routes where GroupPrefix is not null/empty

    // 2. Parse GroupPrefix into path segments
    //    "admin config" → ["admin", "config"]

    // 3. Build tree structure:
    //    - Create/find "admin" group
    //    - Create/find "config" nested under "admin"
    //    - Add command to "config" group

    // 4. Return root groups + ungrouped commands
  }
}
```

**Key Logic**:
- Split `GroupPrefix` by spaces to get path: `"admin config"` → `["admin", "config"]`
- Build tree by walking path and creating/finding nodes
- Leaf node gets the command
- Parent nodes may have empty commands array (shown per user preference)

### Phase 3: Capabilities Emitter Refactor

**Goal**: Generate hierarchical JSON from group tree

**Files to Modify**:
- `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs`

**Changes**:
1. Call `GroupHierarchyBuilder.BuildHierarchy()` before emission
2. Emit groups recursively with nesting
3. Emit ungrouped commands at top level only

**New Structure**:
```csharp
private static void EmitGroups(StringBuilder sb, ImmutableArray<GroupDefinition> groups)
{
  if (groups.IsEmpty) return;

  sb.AppendLine("      \"groups\": [");
  for (int i = 0; i < groups.Length; i++)
  {
    EmitGroup(sb, groups[i], isLast: i == groups.Length - 1);
  }
  sb.AppendLine("      ],");
}

private static void EmitGroup(StringBuilder sb, GroupDefinition group, bool isLast)
{
  sb.AppendLine("        {");
  sb.AppendLine($"          \"name\": \"{EscapeJsonString(group.Name)}\",");

  // Emit nested groups recursively
  if (!group.NestedGroups.IsEmpty)
  {
    EmitNestedGroups(sb, group.NestedGroups);
  }

  // Emit commands in this group
  EmitGroupCommands(sb, group.Commands);

  sb.AppendLine(isLast ? "        }" : "        },");
}
```

### Phase 4: JSON Serialization Context Update

**Files to Modify**:
- `source/timewarp-nuru/capabilities/capabilities-json-serializer-context.cs`
  - Add `[JsonSerializable(typeof(GroupCapability))]`
  - Add `[JsonSerializable(typeof(IReadOnlyList<GroupCapability>))]`

### Phase 5: Testing

**New Test Files**:

1. **Unit Tests** (`tests/timewarp-nuru-tests/capabilities/capabilities-03-groups.cs`):
   ```csharp
   - Should_group_commands_by_single_level_prefix()
   - Should_nest_groups_hierarchically()
   - Should_show_ungrouped_commands_at_top_level()
   - Should_not_duplicate_grouped_commands_in_top_level_array()
   - Should_show_parent_groups_with_empty_commands()
   - Should_handle_three_level_nesting()
   ```

2. **Analyzer Tests** (`tests/timewarp-nuru-analyzers-tests/validation/`):
   ```csharp
   - Should_error_on_multi_word_group_prefix_in_attribute()
   - Should_error_on_multi_word_group_prefix_in_fluent()
   ```

3. **Integration Test** with sample app:
   ```csharp
   NuruApp.CreateBuilder()
     .WithGroupPrefix("admin")
       .Map("status").WithHandler(() => "admin status").Done()
       .WithGroupPrefix("config")
         .Map("get {key}").WithHandler((string k) => k).Done()
       .Done()
     .Done()
     .Map("version").WithHandler(() => "1.0").Done()
     .Build();
   ```

   **Expected JSON**:
   ```json
   {
     "name": "app",
     "version": "1.0.0",
     "groups": [
       {
         "name": "admin",
         "groups": [
           {
             "name": "config",
             "commands": [
               {
                 "pattern": "admin config get {key}",
                 "messageType": "unspecified",
                 "parameters": [{"name": "key", "type": "string", "required": true}],
                 "options": []
               }
             ]
           }
         ],
         "commands": [
           {
             "pattern": "admin status",
             "messageType": "unspecified",
             "parameters": [],
             "options": []
           }
         ]
       }
     ],
     "commands": [
       {
         "pattern": "version",
         "messageType": "unspecified",
         "parameters": [],
         "options": []
       }
     ]
   }
   ```

**Files to Modify**:
- `tests/timewarp-nuru-tests/capabilities/capabilities-02-integration.cs`
  - Update existing tests for new JSON structure
  - Verify commands no longer appear at top level if grouped

### Phase 6: Validation & Constraints

**New Diagnostic** (`NURU0XX`):
- **Title**: "Group prefix must be a single word"
- **Message**: "Group prefix '{0}' contains spaces. Use WithGroupPrefix() nesting for multi-level groups."
- **Severity**: Error
- **Location**: Detect in:
  - `[NuruRouteGroup("docker volume")]` ← error
  - `.WithGroupPrefix("docker volume")` ← error

**Valid Patterns**:
```csharp
// ✅ Valid: Single-word groups
[NuruRouteGroup("docker")]
.WithGroupPrefix("admin").WithGroupPrefix("config")

// ❌ Invalid: Multi-word groups
[NuruRouteGroup("docker volume")]  // ERROR
.WithGroupPrefix("admin config")    // ERROR
```

**Files to Modify**:
- `source/timewarp-nuru-analyzers/validation/` - add group prefix validator
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.cs` - add new diagnostic

### Phase 7: Real-world Validation

**Verify with ganda CLI**:
```bash
# In ganda worktree
ganda --capabilities | jq '.groups[] | {name, commandCount: (.commands | length), nestedGroups: (.groups | length)}'
```

**Expected**: See hierarchy of ganda's groups (repo, workspace, kanban, etc.)

### Phase 8: Documentation

**Files to Update**:
1. **documentation/features/capabilities.md** (or create if missing)
   - Document new JSON schema
   - Show hierarchical examples
   - Note breaking change from v1 format

2. **CHANGELOG.md**
   - Add breaking change notice
   - Document migration path

3. **source/timewarp-nuru/capabilities/capabilities-response.cs**
   - Update XML docs with examples

## Implementation Order

```
Phase 1: Data Models (2-3 hours)
  ├─ Create GroupDefinition.cs
  ├─ Update CapabilitiesResponse.cs + GroupCapability
  └─ Update JSON serialization context

Phase 2: Hierarchy Builder (3-4 hours)
  ├─ Create GroupHierarchyBuilder.cs
  ├─ Implement tree-building algorithm
  └─ Unit test the builder in isolation

Phase 3: Emitter Refactor (2-3 hours)
  ├─ Integrate hierarchy builder
  ├─ Refactor EmitCommands → EmitGroups + EmitUngroupedCommands
  └─ Recursive group emission

Phase 4: Validation (2 hours)
  ├─ Add single-word group constraint diagnostic
  ├─ Validate in attribute extractor
  └─ Validate in fluent API interpreter

Phase 5: Testing (3-4 hours)
  ├─ Create capabilities-03-groups.cs
  ├─ Update capabilities-02-integration.cs
  ├─ Add analyzer validation tests
  └─ Test three-level nesting

Phase 6: Real-world Validation (1 hour)
  └─ Test with ganda CLI

Phase 7: Documentation (1-2 hours)
  ├─ Update capabilities docs
  ├─ Update CHANGELOG
  └─ Add examples to XML docs

Total: ~14-19 hours
```

## Files Roadmap

### Files to Create (4 new files)
1. `source/timewarp-nuru-analyzers/generators/models/group-definition.cs`
2. `source/timewarp-nuru-analyzers/generators/utils/group-hierarchy-builder.cs`
3. `tests/timewarp-nuru-tests/capabilities/capabilities-03-groups.cs`
4. `tests/timewarp-nuru-analyzers-tests/validation/group-prefix-validation-test.cs`

### Files to Modify (6 existing files)
1. `source/timewarp-nuru/capabilities/capabilities-response.cs` - Add GroupCapability
2. `source/timewarp-nuru/capabilities/capabilities-json-serializer-context.cs` - Add serialization
3. `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` - Hierarchical output
4. `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.cs` - Add validation
5. `tests/timewarp-nuru-tests/capabilities/capabilities-02-integration.cs` - Update tests
6. `CHANGELOG.md` - Document breaking change

## Breaking Changes

**Before** (current):
```json
{
  "commands": [
    {"pattern": "admin status", ...},
    {"pattern": "admin config get {key}", ...},
    {"pattern": "version", ...}
  ]
}
```

**After** (new):
```json
{
  "groups": [
    {
      "name": "admin",
      "groups": [
        {
          "name": "config",
          "commands": [{"pattern": "admin config get {key}", ...}]
        }
      ],
      "commands": [{"pattern": "admin status", ...}]
    }
  ],
  "commands": [
    {"pattern": "version", ...}
  ]
}
```

**Migration Path for Consumers**:
- Check for presence of `groups` field
- If present, traverse hierarchy for full command list
- If absent, fallback to flat `commands` array (old format)

## Risk Mitigation

1. **Risk**: Breaking existing AI tool consumers
   - **Mitigation**: Document in CHANGELOG, consumers can detect new format via presence of `groups` field

2. **Risk**: Complex tree-building logic has bugs
   - **Mitigation**: Extensive unit tests on `GroupHierarchyBuilder` in isolation before integration

3. **Risk**: Single-word constraint breaks existing apps
   - **Mitigation**: New diagnostic with clear error message guides users to fix

4. **Risk**: Performance impact of tree building
   - **Mitigation**: Minimal - done at compile time once per build

## Success Criteria

✅ Grouped commands appear ONLY in their group, not in top-level commands array
✅ Nested groups (admin > config) represented hierarchically in JSON
✅ Ungrouped commands appear only at top level
✅ Empty parent groups shown with empty commands array
✅ Single-word group prefix constraint enforced with diagnostic
✅ All existing tests pass (after updating for new format)
✅ New capabilities-03-groups.cs has 100% pass rate
✅ ganda CLI shows proper hierarchy in --capabilities output
