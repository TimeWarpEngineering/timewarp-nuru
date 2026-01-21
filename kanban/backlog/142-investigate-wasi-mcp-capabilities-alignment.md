# Investigate WASI/MCP/Nuru Capabilities Alignment

## Description

Investigate aligning TimeWarp.Nuru's endpoint schema with WASI's capability-based security model and MCP's tool capabilities. The goal is to establish a ubiquitous language for capabilities across these systems, enabling future interoperability where Nuru CLI apps can expose schemas with capabilities that map naturally to both MCP tool descriptors and WASI sandbox permissions.

Microsoft's [Wassette](https://github.com/microsoft/wassette) project provides a reference implementation for bridging WASI sandboxes with MCP's agent-friendly tool ecosystem.

## Requirements

- Research WASI Preview 2 capabilities model (descriptor flags, modes, WIT interfaces)
- Understand MCP tool capability descriptors
- Design a shared capabilities vocabulary for Nuru endpoints
- Evaluate Wassette as a bridge implementation

## Checklist

- [ ] Review WASI Preview 2 WIT specifications for capabilities
- [ ] Document MCP tool capability schema patterns
- [ ] Draft shared "Capabilities Descriptor" specification
- [ ] Prototype capability attributes for Nuru routes
- [ ] Evaluate Wassette integration path
- [ ] Create follow-up implementation tasks

## Notes

**Source URL:** https://grok.com/share/bGVnYWN5LWNvcHk_10511a09-0a78-428a-bf4b-f705cdc92501

### Vision

Create alignment across three systems using a ubiquitous capabilities language:

1. **Nuru Console Apps** - Expose schema with capabilities for each endpoint
2. **MCP Servers** - Convey capabilities of each tool to AI agents
3. **WASI** - Express capabilities for sandboxed containers

While WASI isn't the current runtime context for Nuru or MCP, this forward-thinking approach prepares for a future where AI agents invoke CLI tools in secure sandboxes.

### WASI Capabilities Overview

**Preview 1 (Legacy):** Rights-based bitmasks (e.g., `FILE_RIGHTS::READ`)

**Preview 2 (Recommended):** 
- `DescriptorFlags`: Operations allowed (READ, WRITE, APPEND, DSYNC)
- `OFlags`: Open flags (CREATE, TRUNC, DIRECTORY)
- `Mode`: POSIX-like permissions (owner/group/other)
- All operations require a `Descriptor` handle for sandboxed resolution
- Defined via WIT (WebAssembly Interface Type) IDL

### Proposed Shared Capabilities Model

A language-agnostic descriptor (JSON/YAML) borrowing from all three systems:

```json
{
  "id": "app:deploy/prod",
  "operations": [
    {
      "method": "POST",
      "path": "/deploy/prod",
      "capabilities": {
        "rights": ["write:config", "exec:shell"],
        "scopes": ["repo:timewarp/nuru"],
        "schema": {
          "request": { "type": "object", "properties": { "env": { "type": "string" } } },
          "response": { "type": "object", "properties": { "status": { "enum": ["success", "failed"] } } }
        }
      }
    }
  ],
  "sandbox": { "wasi:compatible": true, "preopen": ["/host/deploy"] }
}
```

### Potential Nuru Integration

```csharp
builder.AddRoute("deploy/prod {env:string}", async (string env) => {
    // Handler logic
})
.WithCapabilities(new[] { "write:config", "exec:shell" });
```

### Wassette Reference

Microsoft's Wassette (https://github.com/microsoft/wassette):
- Lightweight Go runtime for WebAssembly Components via MCP
- Uses Wasmtime for WASI-style sandboxing
- AI agents can dynamically load/invoke Wasm tools from OCI registries
- Components defined via WIT interfaces
- Could serve as bridge for Nuru → MCP → WASI execution

### Prototype Path

1. Package a Nuru endpoint with capability annotations
2. Compile to Wasm Component via wasm-tools + WIT
3. Push to OCI registry
4. Invoke via Wassette from AI agent
5. Validate capabilities on load

### Query/Command Distinction

**Related:** Task 150 (Endpoints) identified a design gap - the current Nuru route syntax (string, fluent, attribute) does not distinguish between:
- `IBaseQuery` (read operations)
- `IBaseCommand` (write operations)
- `IBaseRequest` (general requests)

Martin Mediator's hierarchy (`IMessage` → `IBaseRequest`/`IBaseCommand`/`IBaseQuery`) provides this semantic information, but Nuru doesn't currently expose it in route definitions.

For MCP schema generation, this distinction matters:
- Queries are safe, idempotent, read-only operations
- Commands may have side effects

This investigation should consider how to surface query/command semantics in the capabilities model.

### Related Resources

- WASI GitHub: https://github.com/WebAssembly/WASI
- Wassette: https://github.com/microsoft/wassette
- MCP Specification: https://modelcontextprotocol.io
