# User Documentation

Documentation for developers building CLI applications with TimeWarp.Nuru.

## Purpose

This section contains guides, tutorials, and reference materials for **users** of the TimeWarp.Nuru library - developers who want to build command-line applications using Nuru's routing framework.

## User vs Developer Documentation

| User Documentation | Developer Documentation |
|--------------------|-------------------------|
| **Audience**: CLI app builders | **Audience**: Nuru contributors |
| **Focus**: How to use Nuru | **Focus**: How Nuru works internally |
| **Content**: Guides, tutorials, API reference | **Content**: Architecture, design decisions |
| **Location**: `documentation/user/` | **Location**: `documentation/developer/` |

## Documentation Structure

### [Getting Started](getting-started.md)
Quick start guide to building your first CLI app with TimeWarp.Nuru.

### [Use Cases](use-cases.md)
Real-world scenarios and patterns:
- Greenfield CLI applications
- Progressive enhancement of existing tools
- Command interception and routing strategies

### [Guides](guides/)
Practical implementation guides:
- Architecture choices (Direct, Mediator, Mixed)
- Deployment strategies (AOT, runfiles, cross-platform)
- Migration from other frameworks
- Best practices

### [Features](features/)
Detailed feature documentation:
- Routing patterns and syntax
- Roslyn Analyzer (compile-time validation)
- Logging system
- Auto-help generation
- Output handling (stdout/stderr, JSON)

### [Tools](tools/)
Supporting tools and integrations:
- MCP Server (AI-assisted development)
- Testing utilities
- Build tools

### [Reference](reference/)
Technical reference materials:
- Performance benchmarks
- Supported types
- API documentation

### [Tutorials](tutorials/)
Step-by-step learning paths:
- Your first CLI app
- Adding dependency injection
- Testing CLI applications

## Related Documentation

- **[Samples](../../Samples/)** - Complete working examples you can run
- **[Developer Documentation](../developer/overview.md)** - For contributors to TimeWarp.Nuru
- **[Cocona Comparison](../../Samples/CoconaComparison/)** - Migration guides from Cocona
