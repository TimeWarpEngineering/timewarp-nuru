# Routing Test Plan

> **See also**: [Test Plan Overview](../test-plan-overview.md) for the three-layer testing architecture and shared philosophy.

This test plan covers **Layer 3: Routing (Matching & Binding)** - matching input arguments against compiled routes and binding values to handler parameters.

## Scope

The routing layer is responsible for:

1. **Route Matching** - Finding routes that match input arguments
2. **Route Selection** - Choosing the best match when multiple routes match (specificity-based)
3. **Parameter Binding** - Extracting values and converting types
4. **Nullability Validation** - Enforcing required vs optional at runtime
5. **Error Handling** - Type conversion failures, missing required values

Tests use numbered files (routing-01, routing-02, etc.) for systematic coverage, with 11 sections covering ~50 test scenarios.

---

## Section 1: Basic Route Matching

**Purpose**: Verify fundamental route matching without parameters.

### Test Cases

1. **Exact Literal Match**
   - Route: `status`
   - Input: `status`
   - Expected: ✅ Match
   - Input: `version`
   - Expected: ❌ No match

2. **Multi-Literal Match**
   - Route: `git status`
   - Input: `git status`
   - Expected: ✅ Match
   - Input: `git commit`
   - Expected: ❌ No match

3. **Case Sensitivity**
   - Route: `status`
   - Input: `STATUS`
   - Expected: ❌ No match (case-sensitive by default)

4. **Argument Count Mismatch**
   - Route: `git status`
   - Input: `git`
   - Expected: ❌ No match (too few arguments)
   - Input: `git status --verbose`
   - Expected: ❌ No match (extra arguments, unless catch-all)

5. **Empty Input**
   - Route: `` (empty pattern)
   - Input: ``
   - Expected: ✅ Match
   - Input: `anything`
   - Expected: ❌ No match

---

## Section 2: Parameter Binding

**Purpose**: Verify parameter extraction and type conversion.

### Test Cases

1. **String Parameter Binding**
   - Route: `greet {name}`
   - Handler: `(string name) => ...`
   - Input: `greet Alice`
   - Expected: ✅ Match, `name = "Alice"`

2. **Integer Parameter Binding**
   - Route: `delay {ms:int}`
   - Handler: `(int ms) => ...`
   - Input: `delay 500`
   - Expected: ✅ Match, `ms = 500`
   - Input: `delay abc`
   - Expected: ❌ No match (type conversion fails)

3. **Double Parameter Binding**
   - Route: `calculate {value:double}`
   - Handler: `(double value) => ...`
   - Input: `calculate 3.14`
   - Expected: ✅ Match, `value = 3.14`

4. **Bool Parameter Binding**
   - Route: `set {flag:bool}`
   - Handler: `(bool flag) => ...`
   - Input: `set true`
   - Expected: ✅ Match, `flag = true`
   - Input: `set false`
   - Expected: ✅ Match, `flag = false`

5. **Multiple Parameters**
   - Route: `connect {host} {port:int}`
   - Handler: `(string host, int port) => ...`
   - Input: `connect localhost 8080`
   - Expected: ✅ Match, `host = "localhost"`, `port = 8080`

6. **Parameter Type Mismatch**
   - Route: `age {years:int}`
   - Handler: `(int years) => ...`
   - Input: `age twenty`
   - Expected: ❌ No match (int conversion fails)

---

## Section 3: Optional Parameters (Nullability)

**Purpose**: Verify optional parameter behavior based on nullability.

### Test Cases

1. **Required String Parameter**
   - Route: `deploy {env}`
   - Handler: `(string env) => ...` (non-nullable)
   - Input: `deploy prod`
   - Expected: ✅ Match, `env = "prod"`
   - Input: `deploy`
   - Expected: ❌ No match (missing required parameter)

2. **Optional String Parameter**
   - Route: `deploy {env?}`
   - Handler: `(string? env) => ...` (nullable)
   - Input: `deploy prod`
   - Expected: ✅ Match, `env = "prod"`
   - Input: `deploy`
   - Expected: ✅ Match, `env = null`

3. **Optional Integer Parameter**
   - Route: `list {count:int?}`
   - Handler: `(int? count) => ...`
   - Input: `list 10`
   - Expected: ✅ Match, `count = 10`
   - Input: `list`
   - Expected: ✅ Match, `count = null`

4. **Mixed Required and Optional**
   - Route: `deploy {env} {tag?}`
   - Handler: `(string env, string? tag) => ...`
   - Input: `deploy prod v1.0`
   - Expected: ✅ Match, `env = "prod"`, `tag = "v1.0"`
   - Input: `deploy prod`
   - Expected: ✅ Match, `env = "prod"`, `tag = null`
   - Input: `deploy`
   - Expected: ❌ No match (missing required `env`)

---

## Section 4: Catch-All Parameters

**Purpose**: Verify catch-all parameter capturing remaining arguments.

### Test Cases

1. **Basic Catch-All**
   - Route: `run {*args}`
   - Handler: `(string[] args) => ...`
   - Input: `run one two three`
   - Expected: ✅ Match, `args = ["one", "two", "three"]`

2. **Empty Catch-All**
   - Route: `passthrough {*args}`
   - Handler: `(string[] args) => ...`
   - Input: `passthrough`
   - Expected: ✅ Match, `args = []` (empty array)

3. **Catch-All After Literals**
   - Route: `docker run {*cmd}`
   - Handler: `(string[] cmd) => ...`
   - Input: `docker run nginx --port 8080`
   - Expected: ✅ Match, `cmd = ["nginx", "--port", "8080"]`

4. **Catch-All After Parameters**
   - Route: `execute {script} {*args}`
   - Handler: `(string script, string[] args) => ...`
   - Input: `execute test.sh --verbose --output log.txt`
   - Expected: ✅ Match, `script = "test.sh"`, `args = ["--verbose", "--output", "log.txt"]`

5. **Catch-All Preserves Options**
   - Route: `npm {*args}`
   - Handler: `(string[] args) => ...`
   - Input: `npm install --save-dev typescript`
   - Expected: ✅ Match, `args = ["install", "--save-dev", "typescript"]`
   - Note: Catch-all captures everything as-is, no option parsing

---

## Section 5: Option Matching (Required vs Optional)

**Purpose**: Verify option matching based on parameter nullability.

### Test Cases

1. **Required Option with Non-Nullable Parameter**
   - Route: `build --config {mode}`
   - Handler: `(string mode) => ...` (non-nullable)
   - Input: `build --config debug`
   - Expected: ✅ Match, `mode = "debug"`
   - Input: `build`
   - Expected: ❌ No match (missing required option)

2. **Optional Option with Nullable Parameter**
   - Route: `build --config {mode?}`
   - Handler: `(string? mode) => ...` (nullable)
   - Input: `build --config release`
   - Expected: ✅ Match, `mode = "release"`
   - Input: `build`
   - Expected: ✅ Match, `mode = null` (option omitted)

3. **Boolean Flag (Always Optional)**
   - Route: `build --verbose`
   - Handler: `(bool verbose) => ...`
   - Input: `build --verbose`
   - Expected: ✅ Match, `verbose = true`
   - Input: `build`
   - Expected: ✅ Match, `verbose = false` (flag omitted)

4. **Mixed Required and Optional Options**
   - Route: `deploy --env {e} --tag {t?} --verbose`
   - Handler: `(string e, string? t, bool verbose) => ...`
   - Input: `deploy --env prod --tag v1.0 --verbose`
   - Expected: ✅ Match, `e = "prod"`, `t = "v1.0"`, `verbose = true`
   - Input: `deploy --env prod`
   - Expected: ✅ Match, `e = "prod"`, `t = null`, `verbose = false`
   - Input: `deploy --tag v1.0`
   - Expected: ❌ No match (missing required `--env`)

5. **Option with Typed Parameter**
   - Route: `server --port {num:int}`
   - Handler: `(int num) => ...`
   - Input: `server --port 8080`
   - Expected: ✅ Match, `num = 8080`
   - Input: `server --port abc`
   - Expected: ❌ No match (type conversion fails)

6. **Option Alias Matching**
   - Route: `build --verbose,-v`
   - Handler: `(bool verbose) => ...`
   - Input: `build --verbose`
   - Expected: ✅ Match, `verbose = true`
   - Input: `build -v`
   - Expected: ✅ Match, `verbose = true` (alias works)

---

## Section 6: Repeated Options (Arrays)

**Purpose**: Verify repeated option syntax for collecting multiple values.

### Test Cases

1. **Basic Repeated Option**
   - Route: `docker run --env {e}*`
   - Handler: `(string[] e) => ...`
   - Input: `docker run --env A --env B --env C`
   - Expected: ✅ Match, `e = ["A", "B", "C"]`

2. **Empty Repeated Option**
   - Route: `docker run --env {e}*`
   - Handler: `(string[] e) => ...`
   - Input: `docker run`
   - Expected: ✅ Match, `e = []` (empty array, repeated options implicitly optional)

3. **Typed Repeated Option**
   - Route: `process --id {id:int}*`
   - Handler: `(int[] id) => ...`
   - Input: `process --id 1 --id 2 --id 3`
   - Expected: ✅ Match, `id = [1, 2, 3]`

4. **Repeated Option with Alias**
   - Route: `docker run --env,-e {e}*`
   - Handler: `(string[] e) => ...`
   - Input: `docker run --env A -e B --env C`
   - Expected: ✅ Match, `e = ["A", "B", "C"]` (both forms collected)

5. **Mixed Repeated and Single Options**
   - Route: `deploy --env {e} --tag {t}* --verbose`
   - Handler: `(string e, string[] t, bool verbose) => ...`
   - Input: `deploy --env prod --tag v1 --tag v2 --verbose`
   - Expected: ✅ Match, `e = "prod"`, `t = ["v1", "v2"]`, `verbose = true`

6. **Single Value for Repeated Option**
   - Route: `run --flag {f}*`
   - Handler: `(string[] f) => ...`
   - Input: `run --flag X`
   - Expected: ✅ Match, `f = ["X"]` (single-item array)

---

## Section 7: Route Selection (Specificity)

**Purpose**: Verify specificity-based route selection when multiple routes match.

### Test Cases

1. **Literal Beats Parameter**
   - Routes: `git status` (more specific), `git {command}` (less specific)
   - Input: `git status`
   - Expected: Selects `git status`

2. **Typed Parameter Beats Untyped**
   - Routes: `delay {ms:int}` (more specific), `delay {duration}` (less specific)
   - Input: `delay 500`
   - Expected: Selects `delay {ms:int}`

3. **Required Beats Optional**
   - Routes: `deploy {env}` (more specific), `deploy {env?}` (less specific)
   - Input: `deploy prod`
   - Expected: Selects `deploy {env}` (required is more specific)

4. **More Options Beat Fewer**
   - Routes: `build --verbose --watch` (more specific), `build --verbose` (less specific)
   - Input: `build --verbose --watch`
   - Expected: Selects `build --verbose --watch`

5. **Required Option Doesn't Match Without Option**
   - Routes: `build --config {m}` (requires option), `build` (no options)
   - Input: `build`
   - Expected: Selects `build` (first route requires `--config`, doesn't match)

6. **Catch-All as Fallback**
   - Routes: `git status`, `git commit`, `git {*args}`
   - Input: `git push`
   - Expected: Selects `git {*args}` (only matching route)

7. **First Registered Wins on Equal Specificity**
   - Routes: `greet {name}` (registered first), `hello {person}` (registered second)
   - Input: `greet Alice`
   - Expected: Selects first route (only one matches literal "greet")

8. **Progressive Specificity Ranking**
   - Routes:
     - `deploy {env} --tag {t} --verbose` (most specific)
     - `deploy {env} --tag {t}` (medium)
     - `deploy {env}` (less specific)
     - `deploy` (least specific)
   - Input: `deploy prod --tag v1.0`
   - Expected: Selects `deploy {env} --tag {t}` (highest matching specificity)

---

## Section 8: End-of-Options Separator

**Purpose**: Verify `--` separator runtime behavior.

### Test Cases

1. **End-of-Options with Catch-All**
   - Route: `run -- {*args}`
   - Handler: `(string[] args) => ...`
   - Input: `run -- --not-a-flag file.txt`
   - Expected: ✅ Match, `args = ["--not-a-flag", "file.txt"]`
   - Reason: Everything after `--` treated as literals

2. **Parameter Before End-of-Options**
   - Route: `execute {script} -- {*args}`
   - Handler: `(string script, string[] args) => ...`
   - Input: `execute run.sh -- --verbose file.txt`
   - Expected: ✅ Match, `script = "run.sh"`, `args = ["--verbose", "file.txt"]`

3. **Empty Args After Separator**
   - Route: `run -- {*args}`
   - Handler: `(string[] args) => ...`
   - Input: `run --`
   - Expected: ✅ Match, `args = []` (separator present, no following args)

4. **Options Before Separator Parsed Normally**
   - Route: `docker run --detach -- {*cmd}`
   - Handler: `(bool detach, string[] cmd) => ...`
   - Input: `docker run --detach -- nginx --port 80`
   - Expected: ✅ Match, `detach = true`, `cmd = ["nginx", "--port", "80"]`
   - Note: `--detach` parsed as option, `--port` captured as literal in catch-all

---

## Section 9: Complex Integration Scenarios

**Purpose**: Verify realistic multi-feature routing patterns.

### Test Cases

1. **Docker-Style Command**
   - Route: `docker run -i -t --env {e}* -- {*cmd}`
   - Handler: `(bool i, bool t, string[] e, string[] cmd) => ...`
   - Input: `docker run -i -t --env A=1 --env B=2 -- nginx --port 80`
   - Expected: ✅ Match
     - `i = true`, `t = true`
     - `e = ["A=1", "B=2"]`
     - `cmd = ["nginx", "--port", "80"]`

2. **Git Commit with Aliases**
   - Route: `git commit --message,-m {msg} --amend --no-verify`
   - Handler: `(string msg, bool amend, bool noVerify) => ...`
   - Input: `git commit -m "fix bug" --amend`
   - Expected: ✅ Match, `msg = "fix bug"`, `amend = true`, `noVerify = false`

3. **Progressive Enhancement**
   - Route: `build {project?} --config {cfg?} --verbose --watch`
   - Handler: `(string? project, string? cfg, bool verbose, bool watch) => ...`
   - Input: `build --verbose`
   - Expected: ✅ Match, `project = null`, `cfg = null`, `verbose = true`, `watch = false`

4. **Multi-Valued with Types**
   - Route: `process --id {id:int}* --tag {t}* {script}`
   - Handler: `(int[] id, string[] t, string script) => ...`
   - Input: `process --id 1 --id 2 --tag A run.sh`
   - Expected: ✅ Match, `id = [1, 2]`, `t = ["A"]`, `script = "run.sh"`

---

## Section 10: Error Cases & Edge Conditions

**Purpose**: Verify proper handling of invalid input and edge cases.

### Test Cases

1. **No Matching Route**
   - Routes: `status`, `version`
   - Input: `unknown`
   - Expected: ❌ No route matches
   - Application should show error/help message

2. **Type Conversion Failure**
   - Route: `delay {ms:int}`
   - Input: `delay abc`
   - Expected: ❌ Route doesn't match (type constraint fails)

3. **Missing Required Option Value**
   - Route: `build --config {mode}`
   - Input: `build --config`
   - Expected: ❌ Option value missing (ambiguous - could be flag or missing value)

4. **Duplicate Options (Non-Repeated)**
   - Route: `build --verbose`
   - Input: `build --verbose --verbose`
   - Expected: ❌ Duplicate option (or second ignored, implementation-defined)

5. **Unknown Option**
   - Route: `build --verbose`
   - Input: `build --verbose --unknown`
   - Expected: ❌ Unknown option `--unknown`

6. **Mixed Positionals with Options**
   - Route: `deploy {env} --tag {t}`
   - Input: `deploy prod --tag v1.0`
   - Expected: ✅ Match (positionals before options)
   - Input: `deploy --tag v1.0 prod`
   - Expected: Implementation-defined (may require strict ordering)

---

## Section 11: Delegate vs Mediator Consistency

**Purpose**: Verify both routing approaches produce identical results.

### Test Cases

For each test case in Sections 1-10, verify:

1. **Same Route Registration**
   - Delegate: `routes.Add("pattern", handler)`
   - Mediator: `routes.Add<HandlerRequest>("pattern")`
   - Both should parse identically

2. **Same Matching Behavior**
   - Given identical input, both should:
     - Match the same route
     - Extract the same parameter values
     - Invoke handlers with identical arguments

3. **Same Error Behavior**
   - Invalid input should fail identically:
     - Type conversion errors
     - Missing required parameters
     - Unknown options

4. **Performance Characteristics**
   - Delegate approach: Minimal allocations (~4KB)
   - Mediator approach: Additional DI container overhead
   - Both: Comparable matching performance

---

## Implementation Notes

### Test Organization

- Number files sequentially: `routing-01-basic-matching.cs`, `routing-02-parameter-binding.cs`, etc.
- Each file contains 5-10 focused test methods
- Use Kijaribu test framework for consistency with parser tests
- Clear cache between runs: `[ClearRunfileCache]`

### Test Structure

```csharp
[TestTag("Routing")]
[ClearRunfileCache]
public class BasicMatchingTests
{
  public static async Task Should_match_exact_literal()
  {
    // Arrange - Build routes
    var routes = new RouteCollection();
    bool executed = false;
    routes.Add("status", () => { executed = true; });

    // Act - Match input
    var match = routes.Match(["status"]);

    // Assert
    match.ShouldNotBeNull();
    match.Execute();
    executed.ShouldBeTrue();

    await Task.CompletedTask;
  }
}
```

### Coverage Goals

- ✅ All parameter types (string, int, double, bool, arrays)
- ✅ All optionality scenarios (required, optional, boolean)
- ✅ All specificity rules (literals > params > catch-all)
- ✅ Options (required, optional, repeated, aliases)
- ✅ End-of-options separator
- ✅ Complex real-world patterns
- ✅ Error handling
- ✅ Delegate/Mediator parity

### Success Criteria

- All routing tests pass for both Delegate and Mediator implementations
- Test output clearly shows which route was selected and why
- Parameter binding values match expectations
- No regressions when adding new routing features
