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

- [ ] Create runfile in `sandbox/` that manually constructs `RouteDefinition`
- [ ] Build for pattern `"add {x:int} {y:int}"`:
  - [ ] `LiteralDefinition` for "add"
  - [ ] `ParameterDefinition` for "x" with type "int"
  - [ ] `ParameterDefinition` for "y" with type "int"
  - [ ] `HandlerDefinition` (minimal, for add handler)
- [ ] Verify the model compiles and looks correct
- [ ] This becomes input to step-2 (runtime construction)

## Notes

### What We're Building

```csharp
// Manually constructed - no parser involved
var addRoute = RouteDefinition.Create(
  originalPattern: "add {x:int} {y:int}",
  segments:
  [
    new LiteralDefinition(Position: 0, Value: "add"),
    new ParameterDefinition(
      Position: 1,
      Name: "x",
      TypeConstraint: "int",
      Description: null,
      IsOptional: false,
      IsCatchAll: false,
      ResolvedClrTypeName: "global::System.Int32"
    ),
    new ParameterDefinition(
      Position: 2,
      Name: "y", 
      TypeConstraint: "int",
      Description: null,
      IsOptional: false,
      IsCatchAll: false,
      ResolvedClrTypeName: "global::System.Int32"
    ),
  ],
  handler: HandlerDefinition.ForDelegate(
    parameters: [...],
    returnType: HandlerReturnType.Void,
    isAsync: false
  ),
  messageType: "Query",
  description: "Add two numbers"
);
```

### Design-Time Model Types (from #240)

Located in `source/timewarp-nuru-analyzers/models/`:
- `RouteDefinition` - main route container
- `LiteralDefinition` - literal segments like "add"
- `ParameterDefinition` - parameter segments like `{x:int}`
- `HandlerDefinition` - handler method info
- `ParameterBinding` - how params bind to handler args

### Success Criteria

A runfile that:
1. Manually constructs a valid `RouteDefinition` for "add {x:int} {y:int}"
2. Compiles without errors
3. Can be used as input to step-2 (runtime construction)
