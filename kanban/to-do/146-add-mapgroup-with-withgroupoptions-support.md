# Add MapGroup with WithGroupOptions support

## Description

Add ASP.NET Minimal API-style `MapGroup()` to Nuru, enabling hierarchical command organization with shared options that propagate to child routes. This provides Docker-style "global options" scoped to command groups.

## Motivation

- CLI tools like Docker have global options (`--debug`, `--config`) available to all subcommands
- Nuru currently requires repeating options on every route pattern
- ASP.NET developers already understand `MapGroup()` - familiar mental model
- Groups naturally structure help output and improve discoverability

## Proposed API

```csharp
var docker = builder.MapGroup("docker")
    .WithDescription("Container management tool")
    .WithGroupOptions("--debug,-D --log-level {level?}");

docker.Map("run {image}", (string image, bool debug, string? logLevel) => ...);
docker.Map("build {path}", (string path, bool debug, string? logLevel) => ...);

// Nested groups accumulate options
var compose = docker.MapGroup("compose")
    .WithDescription("Multi-container orchestration")
    .WithGroupOptions("--file,-f {path?}");

compose.Map("up", (bool debug, string? logLevel, string? file) => ...);   
compose.Map("down", (bool debug, string? logLevel, string? file) => ...);
```

**Resulting routes:**
- `docker run {image} --debug,-D? --log-level {level?}`
- `docker build {path} --debug,-D? --log-level {level?}`
- `docker compose up --debug,-D? --log-level {level?} --file,-f {path?}`
- `docker compose down --debug,-D? --log-level {level?} --file,-f {path?}`

## Key Design Points

1. **Build-time expansion** - Groups expand to full route patterns at registration. No runtime changes to parsing/matching.

2. **Options are optional by default** - Group options should probably be optional (`?`) since not every command invocation needs them.

3. **Accumulated inheritance** - Nested groups inherit parent group options.

4. **Help structure** - Groups define sections in help output with their own "Group Options" display.

5. **Shell completion** - Completion engine offers group options for any command within the group.

## Checklist

- [ ] Design `RouteGroup` class to hold prefix, description, and group options
- [ ] Implement `MapGroup()` extension method on builder
- [ ] Implement `MapGroup()` extension method on `RouteGroup` (for nesting)
- [ ] Implement `WithDescription()` fluent method
- [ ] Implement `WithGroupOptions()` fluent method
- [ ] Implement `Map()` overloads on `RouteGroup` that expand patterns
- [ ] Update help generation to display group structure and group options
- [ ] Update shell completion to include group options
- [ ] Add sample demonstrating nested groups with options
- [ ] Add tests for pattern expansion
- [ ] Add tests for nested option accumulation
- [ ] Documentation

## Design Decisions

### Options follow literals (not Docker-style)

**Decision:** Group options append after literals, same as current Nuru patterns.

```
# Nuru style (options after literals) - CHOSEN
docker compose up --debug --file ./compose.yaml

# Docker style (global options before subcommand) - NOT DOING
docker --debug compose --file ./compose.yaml up
```

**Rationale:** Docker's style requires multi-phase parsing (parse globals, strip, determine subcommand, parse subcommand options). Nuru's single-pass matcher handles everything as one pattern. Zero runtime changes needed.

## Notes

- This is syntactic sugar - all existing parsing, matching, and binding logic remains unchanged
- Group options expand into the route pattern with `?` (optional) modifier
- Consider: collision detection if route defines same option as group
- Reference: ASP.NET `MapGroup()` for API conventions
