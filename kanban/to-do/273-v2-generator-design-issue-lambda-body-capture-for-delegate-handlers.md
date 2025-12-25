# V2 Generator Design Issue: Lambda Body Capture for Delegate Handlers

## Description

The V2 generator has a **fundamental design issue**: the current implementation attempts to invoke handlers using a `Handler(args)` pattern, but the generated code doesn't have access to the actual lambda/delegate code at runtime.

**The Problem:**
When a user writes:
```csharp
.Map("ping")
  .WithHandler(() => "pong")
  .AsQuery()
  .Done()
```

The generator sees the lambda `() => "pong"` in the source code during compilation. However, the current `HandlerInvokerEmitter` generates code like:
```csharp
string result = Handler(args);  // ERROR: "Handler" is not defined
```

There is no `Handler` symbol available in the generated code - the lambda exists only in the original source.

## Parent

#265 Epic: V2 Source Generator Implementation

## Blocked

#272 V2 Generator Phase 6: Testing - Cannot complete testing until this is resolved

## Impact

- **All delegate-based route handlers are broken**
- The generated code will not compile
- This is a fundamental architecture issue, not a bug

## Root Cause Analysis

The current emission architecture assumes handlers can be "invoked" but doesn't define how the handler code is made available in the generated file.

**What the generator extracts:**
- Handler parameter types
- Handler return type
- Handler async status

**What the generator does NOT extract:**
- The actual lambda body source code
- A reference to the lambda that can be invoked

## Possible Solutions

### Option A: Capture Lambda Body Source (Workaround Attempted)
Capture the lambda body as a string during extraction and inline it into generated code.

**Pros:** 
- Keeps the interception model
- Generated code is self-contained

**Cons:**
- Complex string manipulation
- May break with complex lambdas
- Closure variables won't work
- Type inference issues

### Option B: Store Handler Delegates at Build Time
Instead of inlining, have `Build()` store the delegates in a registry that the generated interceptor can access.

```csharp
// During Build():
HandlerRegistry.Register("ping", () => "pong");

// Generated code:
var handler = HandlerRegistry.Get<Func<string>>("ping");
string result = handler();
```

**Pros:**
- Handlers work correctly
- Closures preserved

**Cons:**
- Requires runtime state
- May not be AOT-friendly
- Defeats some of the "pure compile-time" goal

### Option C: Generate Partial Methods
Generate partial method stubs that the user's code implements.

**Cons:**
- Changes the API significantly
- Not backwards compatible

### Option D: Re-evaluate the Interception Model
The current model intercepts `RunAsync()` and tries to replace all routing logic. Perhaps the generator should instead:
1. Generate a route table/metadata
2. Generate invoker methods for type-safe dispatch
3. Keep the runtime routing but optimize it with generated data

**Pros:**
- More achievable
- Still provides compile-time optimization
- Closures and runtime state work naturally

**Cons:**
- Less "pure" than full interception
- Some runtime overhead remains

## Recommended Action

**STOP** attempting workarounds. This is an **architectural decision** that needs discussion before proceeding.

The copilot attempted to add `LambdaBodySource` capture and inline the lambda body as strings - this was reverted as it's a workaround that doesn't address the fundamental issue.

## Questions to Answer

1. What is the actual goal of the V2 generator?
   - Zero runtime reflection? 
   - Full AOT support?
   - Performance optimization?
   - Type safety?

2. Is intercepting `RunAsync()` the right approach, or should we generate helpers that the runtime uses?

3. How do we handle closures in lambda handlers?

4. What happens with handlers that reference local variables, services, etc.?

## Checklist

- [ ] Discuss and decide on architecture approach
- [ ] Document the chosen solution
- [ ] Update affected tasks (270, 271, 272) with new design
- [ ] Implement the solution

## Notes

### Discovery Context
This issue was discovered while implementing Phase 6 Testing (Task #272). The minimal test case:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Build();

int exitCode = await app.RunAsync(["ping"]);
```

The generated interceptor code has no way to invoke `() => "pong"` because that lambda only exists as syntax during compilation, not as a callable symbol in the generated code.

### Files Involved
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - Emits handler invocation code
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs` - Extracts handler info
- `source/timewarp-nuru-analyzers/generators/models/handler-definition.cs` - Handler IR model

### Reverted Changes
The following changes were attempted as a workaround and reverted:
- Adding `LambdaBodySource` property to `HandlerDefinition`
- Capturing lambda body text in `HandlerExtractor`
- Inlining lambda body strings in `HandlerInvokerEmitter`
- Adding `InterceptsLocationAttribute` polyfill to generated code
