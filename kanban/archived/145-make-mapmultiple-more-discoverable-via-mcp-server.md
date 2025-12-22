# Make MapMultiple more discoverable via MCP server

## Description

When users ask about command aliases (e.g., `workspace,ws`), the MCP server should guide them to `MapMultiple` as the solution. Currently, users may not discover this API exists.

## Context

- Route patterns do NOT support inline literal aliases like `workspace,ws` (comma syntax only works for options like `--verbose,-v`)
- `MapMultiple` exists specifically for mapping multiple patterns to the same handler
- This is a common CLI pattern (npm i/install, cargo b/build, kubectl get po/pods)

## Checklist

- [ ] Add `MapMultiple` examples to MCP example retrieval
- [ ] Update `get_syntax` tool to mention `MapMultiple` for command aliases
- [ ] Consider adding a dedicated MCP tool or FAQ entry for "how to create command aliases"
- [ ] Ensure `validate_route` error for `workspace,ws` suggests `MapMultiple`

## Notes

Example of what users want:
```csharp
// They try this (invalid):
builder.Map("workspace,ws --list", ...);

// They should use this:
builder.MapMultiple(
    ["workspace --list", "ws --list"],
    handler,
    "List workspace items");
```

Help system automatically groups routes with the same description, so `workspace` and `ws` appear together in help output.

## Archived

**Reason:** Obsolete - `MapMultiple` is being removed entirely. It's just syntactic sugar for a foreach loop calling `Map`, the source generator doesn't support it, and help grouping is based on description matching (not registration method). Users can call `Map` multiple times with the same description to get alias grouping in help output. See task 205 for removal.
