# Original Feedback: TimeWarp.Nuru CLI Framework
**Reviewer: Community Contributor**
**Date: 2025-08-27**
**Source: GitHub Repository Feedback**

## Feedback Content

```
few hours
12:53 PM
So, I read the installation and use guide. Haven't tried it yet.

1. First thing I noticed is that Map naming might be wrong. I expected that to be used for web only. Only after I continued to read the rest of the code did I understand the project use-case. I would name it something like AddCommany as that is what it is, and it works better in CLI world.

2. I would also introduce a way to handle errors. Maybe link it after the first method. Example .Map(...).OnError(...). This will give more information when user inputs wrong data.

Other than that, seems like a really good way of handling CLI commands. Very easy to use.
```

## Feedback Summary

### Point 1: Naming Convention
- **Issue**: `Map` naming is confusing for CLI context
- **Expectation**: Expected web-only usage
- **Suggestion**: Rename to `AddCommand` or similar
- **Reasoning**: Better alignment with CLI terminology

### Point 2: Error Handling
- **Issue**: No specific error handling mechanism
- **Suggestion**: Add `.OnError()` fluent API
- **Example**: `.Map(...).OnError(...)`
- **Benefit**: Better user feedback for invalid input

### Overall Assessment
- **Positive**: "seems like a really good way of handling CLI commands. Very easy to use."
- **Focus**: API naming and error handling improvements

## Context
- Reviewer read installation and usage guide
- Has not yet tried the framework
- Feedback focused on developer experience and API design