# Manually build design-time model for test pattern

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Manually construct a `RouteDefinition` (design-time model) for the calculator's `add` command. This becomes:
1. The **expected output** for testing a parser later
2. The **input** to step-2 (manual runtime construction)

No parsing yet - just hand-build the correct design-time model.

## Context

Test structure:
```csharp
// Arrange - route pattern string (step-2 input, step-3 when we have parser)
string pattern = "add {x:int} {y:int}";

// Act - parser creates this (step-3, not this task)
RouteDefinition actual = Parse(pattern);

// Assert - manually built expected (THIS TASK - step-1)
RouteDefinition expected = /* hand-built */;
actual.ShouldBe(expected);
```

We're building the **Assert** first, so we know what correct looks like.

## Checklist

- [x] Create runfile in `sandbox/` that manually constructs `RouteDefinition`
- [x] Build for pattern `"add {x:int} {y:int}"`:
  - [x] `LiteralDefinition` for "add"
  - [x] `ParameterDefinition` for "x" with type "int"
  - [x] `ParameterDefinition` for "y" with type "int"
  - [x] `HandlerDefinition` (for delegate returning int)
- [x] Verify the model compiles and looks correct
- [x] This becomes input to step-2 (runtime construction)

## Results

**Completed 2024-12-23**

Created `sandbox/build-design-time-model.cs` that:
- Manually constructs `RouteDefinition` for "add {x:int} {y:int}"
- Copies minimal model types inline (since originals are internal)
- Prints model details for verification

Output verified:
```
OriginalPattern: "add {x:int} {y:int}"
MessageType: Query
Segments:
  [0] Literal: "add" (specificity: 1000)
  [1] Parameter: {x:int} - CLR Type: System.Int32
  [2] Parameter: {y:int} - CLR Type: System.Int32
Handler: Delegate, returns int, not async
Bindings: x and y both require string->int conversion
```

Ready for step-2: use this `RouteDefinition` to construct runtime structures.

## Notes

### Design-Time Model Types (from #240)

Located in `source/timewarp-nuru-analyzers/models/`:
- `RouteDefinition` - main route container
- `LiteralDefinition` - literal segments like "add"
- `ParameterDefinition` - parameter segments like `{x:int}`
- `HandlerDefinition` - handler method info
- `ParameterBinding` - how params bind to handler args
