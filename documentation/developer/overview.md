# Developer Documentation

Structured documentation for TimeWarp.Nuru developers, organized by purpose and information flow.

## Documentation Categories

### [Reference](./reference/) - What IS
**The actual implementation as it exists in source code**
- API documentation derived from actual source files
- Current feature capabilities and limitations
- Actual syntax and behavior
- Must be generated/verified from SOURCE CODE, never from design docs

### [Design](./design/) - What SHOULD BE
**Goals, principles, and architectural decisions**
- Design patterns and philosophy
- Best practices and intended usage
- Architectural decisions and rationale
- Immutable vision that guides implementation

### [Roadmap](./roadmap/) - What WILL BE
**Planned changes and future features**
- Proposed restructuring and refactoring
- Feature planning and tracking
- Breaking changes and migration paths
- Timeline and prioritization

### [Standards](./standards/) - How We Work
**Enforced conventions and rules**
- Coding standards and style guides
- Framework conventions
- Analyzer rules and configurations
- Build-time enforced requirements

### [Guides](./guides/) - How To Do Things
**Practical instructions and tutorials**
- Implementation guides
- Debugging techniques
- Common patterns and solutions
- Step-by-step tutorials

## Critical Information Flow Rules

### Unidirectional Flow
```
SOURCE CODE ──────→ Reference Documentation
     ↑                    (never from Design)
     │
Design Documents
     ↑
     │
Architectural Vision
```

### Documentation Access Control

| Role                      | Can Read               | Can Write                 | Restrictions                         |
| ------------------------- | ---------------------- | ------------------------- | ------------------------------------ |
| **Architect Mode**        | All sections           | Design, Roadmap           | Only role that modifies Design       |
| **Reference Writer Mode** | Source code, Reference | Reference                 | MUST NOT read Design docs            |
| **Developer Mode**        | All sections           | Guides, Standards updates | Cannot modify Design or Reference    |
| **Implementation Mode**   | All sections           | Source code               | Must follow Design, update Reference |

## Key Principles

1. **Reference reflects reality**: The Reference section MUST be derived from actual source code, not from design intentions
2. **Design is immutable by implementation**: Design documents are NEVER changed to accommodate implementation limitations
3. **Clear boundaries**: Each section has a specific purpose and scope
4. **Prevent circular reasoning**: Reference Writers cannot read Design to avoid generating "what is" from "what should be"

## Where to Find Information

| Looking for...                           | Check section | Example                        |
| ---------------------------------------- | ------------- | ------------------------------ |
| How a feature actually works             | Reference     | Current route syntax           |
| Why something was designed a certain way | Design        | Greenfield patterns philosophy |
| What's coming next                       | Roadmap       | Parser restructure plans       |
| Coding conventions                       | Standards     | C# style guide                 |
| How to implement a feature               | Guides        | Adding help to routes          |