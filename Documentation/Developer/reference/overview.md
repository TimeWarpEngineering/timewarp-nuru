# Reference Documentation

Documentation of the actual implementation as it exists in source code.

## Purpose

Reference documentation describes **what IS** - the current reality of the codebase:
- API documentation derived from actual source files
- Current feature capabilities and limitations
- Actual syntax and behavior as implemented
- Must be generated/verified from SOURCE CODE, never from design docs

## Key Principle

**Reference reflects reality**: All documentation here MUST be derived from actual source code, not from design intentions or planned features.

## Available References

### Core Framework
- [Glossary](glossary.md) - Terminology definitions used in the actual codebase
- [Route Pattern Syntax](RoutePatternSyntax.md) - Currently supported route syntax
- [Error Handling](error-handling.md) - How errors are actually handled in the framework

### Parser Implementation
- [Parser Classes: Syntax vs Semantics](ParserClassesSyntaxVsSemantics.md) - Current parser architecture
- [Parsing Flow Dependency Analysis](ParsingFlowDependencyAnalysis.md) - How parsing currently works

### Tools and Extensions
- [Using TimeWarp.Nuru Analyzers](UsingAnalyzers.md) - Available compile-time analyzers

## Important Note

This section does NOT contain:
- Design goals or philosophy (see [Design](../design/))
- Coding standards or conventions (see [Standards](../standards/))
- Future plans or proposals (see [Roadmap](../roadmap/))
- How-to guides or tutorials (see [Guides](../guides/))

If documentation cannot be verified against source code, it doesn't belong here.