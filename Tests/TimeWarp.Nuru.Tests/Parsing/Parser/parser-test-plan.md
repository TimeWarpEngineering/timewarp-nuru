# Parser Test Plan

> **See also**: [Test Plan Overview](../../test-plan-overview.md) for the three-layer testing architecture and shared philosophy.

This test plan covers **Layer 2: Parser (Compilation & Validation)** - building structured routes from token streams and validating semantic rules.

## Scope

The parser is responsible for:

1. **Syntax Validation** - Enforcing all parameter and option rules
2. **AST Construction** - Building abstract syntax tree from tokens
3. **Specificity Calculation** - Computing route specificity scores
4. **Semantic Validation** - Enforcing NURU_S001-S008 rules

Organized into 12 focused sections with 90+ individual test cases.

**Note**: Sections covering runtime behavior (option matching, route selection, parameter binding) have been moved to [Routing Test Plan](../../Routing/routing-test-plan.md) for proper layer separation.

## Design Document References

- `design/parser/syntax-rules.md` - Route pattern syntax and validation
- `design/resolver/specificity-algorithm.md` - Specificity calculation
- `design/cross-cutting/parameter-optionality.md` - Nullability-based optionality

---

## Section 1: Basic Parameter Parsing

**Purpose**: Verify parser correctly handles simple positional parameters.

### Test Cases

1. **Single Required Parameter**
   - Pattern: `greet {name}`
   - Expected: 1 positional parameter, required, untyped
   - Specificity: 10 (untyped parameter)

2. **Multiple Required Parameters**
   - Pattern: `copy {source} {dest}`
   - Expected: 2 positional parameters, both required, untyped
   - Specificity: 20 (2 × 10)

3. **Mixed Literals and Parameters**
   - Pattern: `deploy {env} to {region}`
   - Expected: 2 literals ("deploy", "to"), 2 parameters
   - Specificity: 220 (100 + 100 + 10 + 10)

4. **Parameter with Description**
   - Pattern: `greet {name|Person to greet}`
   - Expected: Parameter with description metadata
   - Verify: Description stored but not part of matching

---

## Section 2: Typed Parameters

**Purpose**: Verify type constraint parsing and specificity impact.

### Test Cases

1. **Integer Type Constraint**
   - Pattern: `delay {ms:int}`
   - Expected: 1 parameter, type=int, required
   - Specificity: 20 (typed parameter)

2. **Double Type Constraint**
   - Pattern: `scale {factor:double}`
   - Expected: 1 parameter, type=double, required
   - Specificity: 20

3. **DateTime Type Constraint**
   - Pattern: `schedule {when:datetime}`
   - Expected: 1 parameter, type=datetime, required
   - Specificity: 20

4. **Multiple Typed Parameters**
   - Pattern: `process {id:int} {priority:double}`
   - Expected: 2 parameters, both typed, both required
   - Specificity: 40 (20 + 20)

5. **Mixed Typed and Untyped**
   - Pattern: `run {script} {timeout:int}`
   - Expected: script=untyped(10), timeout=typed(20)
   - Specificity: 30 (10 + 20)

6. **Type with Description**
   - Pattern: `delay {ms:int|Milliseconds}`
   - Expected: Type and description both parsed
   - Specificity: 20 (description doesn't affect scoring)

---

## Section 3: Optional Parameters

**Purpose**: Verify optional parameter parsing and nullability.

### Test Cases

1. **Single Optional Parameter**
   - Pattern: `deploy {env?}`
   - Expected: 1 parameter, optional, untyped
   - Specificity: 5 (optional parameter)

2. **Typed Optional Parameter**
   - Pattern: `delay {ms:int?}`
   - Expected: 1 parameter, optional, typed
   - Specificity: 5 (optional trumps typed for scoring)

3. **Required Then Optional**
   - Pattern: `copy {source} {dest?}`
   - Expected: source=required(10), dest=optional(5)
   - Specificity: 15 (10 + 5)
   - Valid: ✅ Required before optional

4. **Optional with Description**
   - Pattern: `deploy {env?|Environment name}`
   - Expected: Optional parameter with description
   - Specificity: 5

---

## Section 4: Required-Before-Optional Rule (NURU_S006)

**Purpose**: Enforce that optional positional parameters must come after required ones.

### Test Cases

1. **Valid: Required Before Optional**
   - Pattern: `copy {source} {dest?}`
   - Expected: ✅ Parse succeeds
   - Reason: Required source comes before optional dest

2. **Invalid: Optional Before Required**
   - Pattern: `copy {source?} {dest}`
   - Expected: ❌ NURU_S006 error
   - Error: "Optional parameter 'source' cannot appear before required parameter 'dest'"
   - Reason: Ambiguous - input "copy file.txt" could be source or dest

3. **Valid: All Required**
   - Pattern: `copy {source} {dest}`
   - Expected: ✅ Parse succeeds
   - Reason: No optional parameters, rule doesn't apply

4. **Valid: All Optional (Single)**
   - Pattern: `deploy {env?}`
   - Expected: ✅ Parse succeeds
   - Reason: Only one parameter, can be optional

5. **Invalid: Multiple Optional After One Required**
   - Pattern: `run {script} {arg1?} {arg2?}`
   - Expected: ❌ NURU_S002 error (next section covers this)
   - Reason: Multiple consecutive optionals not allowed

---

## Section 5: Single Optional Positional Rule (NURU_S002)

**Purpose**: Enforce that only ONE optional positional parameter is allowed (no consecutive optionals).

### Test Cases

1. **Valid: Single Optional at End**
   - Pattern: `deploy {env} {tag?}`
   - Expected: ✅ Parse succeeds
   - Reason: Only one optional parameter

2. **Invalid: Two Consecutive Optionals**
   - Pattern: `deploy {env?} {version?}`
   - Expected: ❌ NURU_S002 error
   - Error: "Only one optional positional parameter allowed. Found: env, version"
   - Reason: Input "deploy v2.0" is ambiguous - is v2.0 the env or version?

3. **Invalid: Three Consecutive Optionals**
   - Pattern: `run {script?} {arg1?} {arg2?}`
   - Expected: ❌ NURU_S002 error
   - Reason: Extreme ambiguity with multiple optionals

4. **Valid: Required-Optional-Required Pattern Not Possible**
   - This pattern is already blocked by NURU_S006 (optional before required)
   - Pattern: `run {script} {arg?} {timeout}`
   - Expected: ❌ NURU_S006 error (optional before required)

5. **Edge Case: Single Optional Only**
   - Pattern: `status {format?}`
   - Expected: ✅ Parse succeeds
   - Reason: Only one parameter, can be optional

---

## Section 6: Catch-all Last Rule (NURU_S003)

**Purpose**: Enforce that catch-all parameters must be the last positional parameter.

### Test Cases

1. **Valid: Catch-all at End**
   - Pattern: `docker run {*args}`
   - Expected: ✅ Parse succeeds
   - Specificity: 1 (catch-all lowest priority)

2. **Valid: Parameters Then Catch-all**
   - Pattern: `execute {script} {*args}`
   - Expected: ✅ Parse succeeds
   - Specificity: 11 (10 + 1)

3. **Invalid: Catch-all Before Parameter**
   - Pattern: `run {*args} {script}`
   - Expected: ❌ NURU_S003 error
   - Error: "Catch-all parameter '*args' must be last positional parameter"

4. **Invalid: Catch-all in Middle**
   - Pattern: `run {script} {*args} {timeout}`
   - Expected: ❌ NURU_S003 error
   - Reason: Catch-all consumes all remaining args, nothing left for timeout

5. **Valid: Catch-all with Options**
   - Pattern: `docker run {*args} --verbose`
   - Expected: ✅ Parse succeeds
   - Reason: Options can appear after catch-all (parsed separately)

---

## Section 7: No Optional with Catch-all Rule (NURU_S004)

**Purpose**: Enforce that optional parameters cannot be mixed with catch-all parameters.

### Test Cases

1. **Invalid: Optional Before Catch-all**
   - Pattern: `run {script?} {*args}`
   - Expected: ❌ NURU_S004 error
   - Error: "Cannot mix optional parameters with catch-all parameter"
   - Reason: Ambiguous - where does optional end and catch-all begin?

2. **Invalid: Catch-all After Optional**
   - Same as above (order doesn't matter, both invalid)

3. **Valid: Required Before Catch-all**
   - Pattern: `execute {script} {*args}`
   - Expected: ✅ Parse succeeds
   - Reason: No optional parameters, rule doesn't apply

4. **Valid: Catch-all Only**
   - Pattern: `passthrough {*args}`
   - Expected: ✅ Parse succeeds
   - Reason: No optional parameters

5. **Invalid: Multiple Required, One Optional, Catch-all**
   - Pattern: `run {cmd} {arg?} {*rest}`
   - Expected: ❌ NURU_S004 error
   - Reason: Optional cannot appear with catch-all

---

## Section 8: Option Modifiers

**Purpose**: Verify all option modifier combinations are correctly parsed.

### Test Cases

1. **Required Flag Only (Boolean)**
   - Pattern: `build --verbose`
   - Expected: Boolean flag, always optional (booleans ignore required/optional)
   - Specificity: 50 (required option scoring, though semantically optional)

2. **Required Flag with Required Value**
   - Pattern: `build --config {mode}`
   - Expected: Option required, value required, non-nullable
   - Specificity: 50 (required option)

3. **Required Flag with Optional Value**
   - Pattern: `build --config {mode?}`
   - Expected: Option required (must be present), value optional (nullable)
   - Specificity: 50 (flag is required)

4. **Optional Flag with Optional Value**
   - Pattern: `build --config? {mode?}`
   - Expected: ✅ Both flag and value optional (per syntax-rules.md and parameter-optionality.md)
   - Specificity: 25 (optional option with optional value)
   - Use Case: Progressive override - route matches with no flag, flag only, or flag with value
   - Valid invocations: `build` (mode=null), `build --config` (mode=null), `build --config debug` (mode="debug")

5. **Optional Flag with Required Value**
   - Pattern: `--tag? {tag}`
   - Expected: ✅ Flag optional, but if present, value is required (per syntax-rules.md and parameter-optionality.md)
   - Specificity: 25 (optional option)
   - Use Case: Conditional enhancement - only intercept when flag provided WITH value
   - Valid invocations: `command` (tag=null), `command --tag Lexer` (tag="Lexer")
   - Invalid invocation: `command --tag` (flag present but missing required value)

6. **Short Option Flag**
   - Pattern: `build -v`
   - Expected: Boolean flag, short form
   - Specificity: 50

7. **Short Option with Value**
   - Pattern: `build -c {mode}`
   - Expected: Short option with required value
   - Specificity: 50

8. **Option with Alias**
   - Pattern: `build --verbose,-v`
   - Expected: Long and short forms parsed as aliases
   - Both match the same option

9. **Option Alias with Value**
   - Pattern: `build --config,-c {mode}`
   - Expected: Both forms take the same value parameter
   - Specificity: 50

> **Note**: Option modifier STRUCTURE (parsing, validation) is tested in Section 8 above and implemented in [parser-08-option-modifiers.cs](parser-08-option-modifiers.cs). Runtime BEHAVIOR (matching, binding, nullability) is tested in the [Routing Test Plan](../../Routing/routing-test-plan.md) Sections 5-6.

---

## Section 9: End-of-Options Separator (Parsing)

**Purpose**: Verify `--` end-of-options separator is correctly parsed and validated.

**Implementation**: [parser-11-end-of-options.cs](parser-11-end-of-options.cs)

### Parser-Level Tests

The parser verifies compile-time rules for the `--` separator:

1. **Valid: End-of-Options with Catch-all**
   - Pattern: `run -- {*args}`
   - Expected: ✅ Parses successfully
   - Validation: `--` token followed by catch-all parameter

2. **Invalid: End-of-Options Without Catch-all**
   - Pattern: `run --`
   - Expected: ❌ NURU_S007 semantic error
   - Reason: `--` must be followed by catch-all parameter

3. **Invalid: Options After End-of-Options**
   - Pattern: `run -- {*args} --verbose`
   - Expected: ❌ NURU_S008 semantic error
   - Reason: No options allowed after `--` separator

4. **Valid: Options Before End-of-Options**
   - Pattern: `docker run --detach -- {*cmd}`
   - Expected: ✅ Parses successfully
   - Validation: Options before `--` are allowed

> **Note**: Runtime behavior of `--` (literal capture, argument binding) is tested in [Routing Test Plan](../../Routing/routing-test-plan.md) Section 8.

---

## Section 10: Specificity Ranking (Relative Ordering)

**Purpose**: Verify that route specificity correctly determines priority when multiple routes could match.

**Approach**: Test RELATIVE ordering (route1 > route2), NOT exact point values.

### Test Cases from Design Document Examples

#### Git Commit Progressive Interception

Tests the git commit example from [specificity-algorithm.md](../../../documentation/developer/design/resolver/specificity-algorithm.md#example-1-git-commit-interception).

1. **Most Specific Wins**
   ```csharp
   var mostSpecific = PatternParser.Parse("git commit --message {msg} --amend");
   var lessSpecific = PatternParser.Parse("git commit --message {msg}");

   // Assertion: 2 options + param > 1 option + param
   mostSpecific.Specificity.ShouldBeGreaterThan(lessSpecific.Specificity);
   ```

2. **Options Beat Literals**
   ```csharp
   var withOption = PatternParser.Parse("git commit --amend");
   var literalOnly = PatternParser.Parse("git commit");

   // Assertion: Options increase specificity
   withOption.Specificity.ShouldBeGreaterThan(literalOnly.Specificity);
   ```

3. **Literals Beat Catch-all**
   ```csharp
   var specific = PatternParser.Parse("git commit");
   var catchAll = PatternParser.Parse("git {*args}");
   var universal = PatternParser.Parse("{*args}");

   // Assertion: Literals rank higher than catch-all
   specific.Specificity.ShouldBeGreaterThan(catchAll.Specificity);
   catchAll.Specificity.ShouldBeGreaterThan(universal.Specificity);
   ```

4. **Full Git Commit Hierarchy**
   ```csharp
   // Parse all 7 routes from the design doc example
   var r1 = PatternParser.Parse("git commit --message {msg} --amend");
   var r2 = PatternParser.Parse("git commit --message {msg}");
   var r3 = PatternParser.Parse("git commit --amend --no-edit");
   var r4 = PatternParser.Parse("git commit --amend");
   var r5 = PatternParser.Parse("git commit");
   var r6 = PatternParser.Parse("git {*args}");
   var r7 = PatternParser.Parse("{*args}");

   // Assertions verify complete ordering
   r1.Specificity.ShouldBeGreaterThan(r2.Specificity);  // More options wins
   r2.Specificity.ShouldBeGreaterThan(r4.Specificity);  // Param + option > option alone
   r4.Specificity.ShouldBeGreaterThan(r5.Specificity);  // Option > no option
   r5.Specificity.ShouldBeGreaterThan(r6.Specificity);  // Literals > catch-all
   r6.Specificity.ShouldBeGreaterThan(r7.Specificity);  // Literal + catch-all > catch-all alone
   ```

#### Deploy Command Evolution

Tests the deploy example showing progressive feature addition.

5. **Literal Parameter Beats Catch-all**
   ```csharp
   var specific = PatternParser.Parse("deploy production --force");
   var general = PatternParser.Parse("deploy {env}");
   var catchAll = PatternParser.Parse("deploy {env} {*flags}");

   // Assertions
   specific.Specificity.ShouldBeGreaterThan(general.Specificity);  // Literal value > parameter
   general.Specificity.ShouldBeGreaterThan(catchAll.Specificity);  // Parameter > catch-all
   ```

6. **Options Increase Specificity**
   ```csharp
   var withOptions = PatternParser.Parse("deploy {env} --dry-run");
   var withoutOptions = PatternParser.Parse("deploy {env}");

   // Assertion: Adding option increases ranking
   withOptions.Specificity.ShouldBeGreaterThan(withoutOptions.Specificity);
   ```

#### Docker Command Hierarchy

7. **Specific Beats General Beats Universal**
   ```csharp
   var specific = PatternParser.Parse("docker run {image} --detach");
   var general = PatternParser.Parse("docker {cmd} {*args}");
   var universal = PatternParser.Parse("{cmd} {*args}");

   // Assertions verify 3-level hierarchy
   specific.Specificity.ShouldBeGreaterThan(general.Specificity);
   general.Specificity.ShouldBeGreaterThan(universal.Specificity);
   ```

### Conceptual Validation

8. **Literal > Parameter > Catch-all**
   ```csharp
   var literal = PatternParser.Parse("status");
   var param = PatternParser.Parse("{command}");
   var catchAll = PatternParser.Parse("{*args}");

   // Fundamental hierarchy
   literal.Specificity.ShouldBeGreaterThan(param.Specificity);
   param.Specificity.ShouldBeGreaterThan(catchAll.Specificity);
   ```

9. **Multiple Elements Stack**
   ```csharp
   var twoLiterals = PatternParser.Parse("git status");
   var oneLiteral = PatternParser.Parse("status");

   // More specificity elements = higher rank
   twoLiterals.Specificity.ShouldBeGreaterThan(oneLiteral.Specificity);
   ```

10. **Options Contribute to Ranking**
    ```csharp
    var twoOptions = PatternParser.Parse("build --verbose --watch");
    var oneOption = PatternParser.Parse("build --verbose");
    var noOptions = PatternParser.Parse("build");

    // More options = more specific
    twoOptions.Specificity.ShouldBeGreaterThan(oneOption.Specificity);
    oneOption.Specificity.ShouldBeGreaterThan(noOptions.Specificity);
    ```

**Note**: These tests validate the DESIGN INTENT (routing priority), not implementation details (exact point values). If we change scoring constants, tests remain valid as long as relative ordering stays correct

> **Note**: Route selection (matching, choosing best route) is runtime behavior tested in [Routing Test Plan](../../Routing/routing-test-plan.md) Section 7.

---

## Section 11: Complex Pattern Integration

**Purpose**: Verify parser handles real-world complex CLI patterns correctly.

### Test Cases

1. **Docker-style Command**
   - Pattern: `docker run -it {image} --env {e}* -- {*cmd}`
   - Features: Short option, typed array, end-of-options, catch-all
   - Specificity: 100 + 50 + 10 + 50 + 1 = 211

2. **Git-style Command with Aliases**
   - Pattern: `git commit --message,-m {msg} --amend --no-verify`
   - Features: Option aliases, multiple flags
   - Specificity: 100 + 100 + 50 + 50 + 50 = 350

3. **Kubectl-style Imperative**
   - Pattern: `kubectl create {resource:string} {name} --namespace,-n {ns?}`
   - Features: Typed parameter, aliases, optional option
   - Specificity: 100 + 100 + 20 + 10 + 50 = 280

4. **Progressive Enhancement Pattern**
   - Pattern: `build {project?} --config {cfg?} --verbose --watch`
   - Features: Optional param, optional option value, boolean flags
   - Handles: `build`, `build myapp`, `build --verbose`, all combinations
   - Specificity: 100 + 5 + 50 + 50 + 50 = 255

5. **Passthrough with Selective Interception**
   - Pattern: `npm {*args}`
   - Purpose: Catch-all for gradual refinement
   - Later add: `npm install {pkg}` (higher specificity)
   - Specificity: catch-all=101, install=210

6. **Multi-valued Options with Types**
   - Pattern: `process --id {id:int}* --tag {t}* {script}`
   - Features: Two array options, trailing parameter
   - Specificity: 100 + 50 + 50 + 10 = 210

7. **Description-Rich Pattern**
   - Pattern: `deploy {env|Environment} --dry-run,-d|Preview mode --tag {t|Version}*`
   - Features: Descriptions throughout (don't affect matching)
   - Specificity: 100 + 10 + 50 + 50 = 210

---

## Section 12: Error Reporting & Edge Cases

**Purpose**: Verify comprehensive error reporting and edge case handling.

### Error Code Coverage

**Semantic Errors (NURU_S###):**

1. **NURU_S001: Duplicate Parameter Names**
   - Pattern: `run {arg} {arg}`
   - Error: "Duplicate parameter names in route"

2. **NURU_S002: Conflicting Optional Parameters**
   - Pattern: `run {arg1?} {arg2?}`
   - Error: "Only one optional positional parameter allowed"

3. **NURU_S003: Catch-all Not Last**
   - Pattern: `run {*args} {script}`
   - Error: "Catch-all parameter must be last positional parameter"

4. **NURU_S004: Mixed Catch-all with Optional**
   - Pattern: `run {script?} {*args}`
   - Error: "Cannot mix optional parameters with catch-all parameter"

5. **NURU_S005: Option with Duplicate Alias**
   - Pattern: `build --config,-c {m} --count,-c {n}`
   - Error: "Option with duplicate alias"

6. **NURU_S006: Optional Before Required**
   - Pattern: `run {arg?} {script}`
   - Error: "Optional parameter 'arg' cannot appear before required parameter 'script'"

7. **NURU_S007: Invalid End-of-Options Separator**
   - Pattern: `run --`
   - Error: "End-of-options separator '--' must be followed by catch-all parameter"

8. **NURU_S008: Options After End-of-Options Separator**
   - Pattern: `run -- {*args} --verbose`
   - Error: "Options cannot appear after end-of-options separator '--'"

**Parse Errors (NURU_P###):**

1. **NURU_P001: Invalid Parameter Syntax**
   - Pattern: `prompt <input>`
   - Error: "Invalid parameter syntax"

2. **NURU_P002: Unbalanced Braces**
   - Pattern: `deploy {env`
   - Error: "Expected '}'"

3. **NURU_P003: Invalid Option Format**
   - Pattern: `build ---config`
   - Error: "Invalid option format"

4. **NURU_P004: Invalid Type Constraint**
   - Pattern: `run {id:invalidtype}`
   - Error: "Invalid type constraint"

5. **NURU_P005: Invalid Character**
   - Pattern: `test @param`
   - Error: "Invalid character in route pattern"

6. **NURU_P006: Unexpected Token**
   - Pattern: `test }`
   - Error: "Unexpected token in route pattern"

7. **NURU_P007: Null Route Pattern**
   - Pattern: `null`
   - Error: "Null route pattern"

### Edge Cases

1. **Empty Pattern**
   - Pattern: ``
   - Expected: Valid (matches empty input)
   - Specificity: 0

2. **Whitespace-only Pattern**
   - Pattern: `   `
   - Expected: Same as empty after normalization

3. **Unicode in Identifiers**
   - Pattern: `привет {имя}`
   - Expected: Valid (Unicode literals and parameters)

4. **Very Long Pattern**
   - Pattern: 50+ segments with parameters and options
   - Expected: Parses correctly, no arbitrary limits

5. **Duplicate Parameter Names**
   - Pattern: `run {arg} {arg}`
   - Expected: ❌ NURU_S001 error (duplicate parameter 'arg')

6. **Duplicate Option Aliases**
   - Pattern: `build --config,-c {m} --count,-c {n}`
   - Expected: ❌ NURU_S005 error (duplicate alias '-c')

7. **Invalid Type Constraint**
   - Pattern: `run {id:invalidtype}`
   - Expected: ❌ NURU_P004 error (unknown type 'invalidtype')

8. **Option Without Value**
   - Pattern: `build --config`
   - Expected: Boolean flag (no value parameter)
   - Valid: ✅

9. **Malformed Option**
   - Pattern: `build --` (just separator, no catch-all)
   - Expected: ❌ NURU_S007 error

10. **Complex Validation Combo**
    - Pattern: `run {a?} {b} {*c}`
    - Errors: NURU_S006 (optional before required) AND NURU_S004 (optional with catch-all)
    - Expected: Report first error encountered (NURU_S006)

---

## Section 13: Syntax Validation Errors

**Purpose**: Verify parser correctly rejects invalid syntax patterns at the lexical/parsing level.

**Implementation**: [parser-13-syntax-errors.cs](parser-13-syntax-errors.cs)

### Test Cases

1. **Invalid Identifier Starting with Number**
   - Pattern: `build {123abc}`
   - Expected: ❌ Parse error
   - Reason: Identifiers must start with letter or underscore, not digit
   - **Status**: ❌ FAILING - Parser currently accepts this (BUG)

2. **Incomplete Parameter (Opening Brace Only)**
   - Pattern: `run {`
   - Expected: ❌ Parse error
   - Reason: Missing parameter name and closing brace
   - **Status**: ✅ PASSING

3. **Empty Parameter Braces**
   - Pattern: `test { }`
   - Expected: ❌ Parse error
   - Reason: Parameter name required between braces
   - **Status**: ✅ PASSING

4. **Incomplete Option Parameter**
   - Pattern: `build --config {`
   - Expected: ❌ Parse error
   - Reason: Option parameter started but not completed
   - **Status**: ✅ PASSING

5. **Unexpected Closing Brace**
   - Pattern: `test }`
   - Expected: ❌ Parse error
   - Reason: Closing brace without corresponding opening brace
   - **Status**: ✅ PASSING

**Results**: 4/5 tests passing. Test #1 identifies a critical parser bug where numeric-starting identifiers are incorrectly accepted.

---

## Section 14: Reserved for Future Use

---

## Section 15: Custom Type Constraints

**Purpose**: Verify parser accepts custom type constraints for use with `IRouteTypeConverter` implementations.

**Implementation**: [parser-15-custom-type-constraints.cs](parser-15-custom-type-constraints.cs)

**Context**: Fixes issue #62 - Parser now accepts any valid C# identifier as a type constraint, enabling users to register custom type converters with `AddTypeConverter()`.

### Test Cases

1. **Custom Type Constraint (Lowercase)**
   - Pattern: `process {file:fileinfo}`
   - Expected: ✅ Parses successfully
   - Reason: Custom types use valid identifier format
   - **Status**: ✅ PASSING

2. **Custom Type Constraint (PascalCase)**
   - Pattern: `load {data:MyCustomType}`
   - Expected: ✅ Parses successfully
   - Reason: PascalCase is valid identifier format
   - **Status**: ✅ PASSING

3. **Custom Type with Underscores**
   - Pattern: `parse {input:my_custom_type}`
   - Expected: ✅ Parses successfully
   - Reason: Underscores allowed in identifiers
   - **Status**: ✅ PASSING

4. **Custom Type Starting with Underscore**
   - Pattern: `handle {value:_internal}`
   - Expected: ✅ Parses successfully
   - Reason: Valid C# identifier (private convention)
   - **Status**: ✅ PASSING

5. **Custom Type with Numbers**
   - Pattern: `connect {addr:ipv4address}`
   - Expected: ✅ Parses successfully
   - Reason: Numbers allowed after first character
   - **Status**: ✅ PASSING

6. **Reject Type Starting with Number**
   - Pattern: `invalid {x:123type}`
   - Expected: ❌ Parse error (NURU_P004)
   - Reason: Identifiers cannot start with numbers
   - Error: "Invalid type constraint '123type'"
   - **Status**: ✅ PASSING

7. **Reject Type with Special Characters**
   - Pattern: `invalid {x:my-type}`
   - Expected: ❌ Parse error (NURU_P004)
   - Reason: Hyphens not allowed in identifiers
   - Error: "Invalid type constraint 'my-type'"
   - **Status**: ✅ PASSING

8. **Multiple Custom Type Constraints**
   - Pattern: `copy {source:fileinfo} {dest:directoryinfo}`
   - Expected: ✅ Parses successfully
   - Reason: Multiple custom types in same pattern
   - **Status**: ✅ PASSING

9. **Custom Type with Optional Modifier**
   - Pattern: `load {config:fileinfo?}`
   - Expected: ✅ Parses successfully
   - Reason: Custom types work with optional modifier
   - **Status**: ✅ PASSING

### Validation Rules

**Valid Custom Type Identifiers:**
- Must start with letter (a-z, A-Z) or underscore (_)
- Remaining characters can be letters, digits (0-9), or underscores
- Examples: `fileinfo`, `MyType`, `_internal`, `ipv4address`

**Invalid Custom Type Identifiers:**
- Cannot start with digit: `123type` ❌
- Cannot contain special characters: `my-type`, `my.type`, `my$type` ❌
- Cannot be empty or whitespace: ``, `  ` ❌

### Integration with Type Conversion System

Custom type constraints work with the `IRouteTypeConverter` system:

```csharp
// Register custom converter
app.AddTypeConverter(new FileInfoTypeConverter());

// Use in route pattern with type constraint
app.AddRoute("process {file:fileinfo}", (FileInfo file) => { ... });
```

The parser validates the **format** of the type constraint (valid identifier), while the runtime validates that a **converter is registered** for that type.

**Results**: 9/9 tests passing. Custom type constraint support fully functional.

---

## Implementation Strategy

### Phase 1: Core Parsing (Sections 1-3)
- ✅ parser-01: Basic parameter parsing
- ✅ parser-02: Type constraint support
- ✅ parser-03: Optional parameters

### Phase 2: Validation (Sections 4-7)
- ✅ parser-04: Duplicate parameter detection
- ✅ parser-05: Consecutive optional parameters
- ✅ parser-06: Catch-all position validation
- ✅ parser-07: Catch-all + optional conflict detection

### Phase 3: Options (Section 8)
- ✅ parser-08: Option modifiers (boolean flags, required/optional values, aliases)

### Phase 4: Advanced Routing (Sections 9-11)
- ✅ parser-09: End-of-options separator (`--`)
- ✅ parser-10: Specificity ranking algorithm
- ✅ parser-11: Complex real-world integration patterns

### Phase 5: Error Reporting (Sections 12-13)
- ✅ parser-12: Comprehensive error reporting (semantic + parse errors + modifier syntax validation)
- ✅ parser-13: Syntax validation errors (lexical/parsing edge cases)
- ⚠️ **Known Issue**: parser-13 identifies identifier validation bug (`{123abc}` incorrectly accepted)

### Phase 6: Extensibility (Section 15)
- ✅ parser-15: Custom type constraints for `IRouteTypeConverter` support (issue #62)
- ✅ **9/9 tests passing** - Custom type validation fully functional

---

## Testing Approach

### Progressive Test Structure

Tests build from simple compilation checks to complex behavioral validation:

**Sections 1-3 (parser-01 to parser-03): Compilation Verification**
- Focus: Do these patterns parse successfully?
- Assertions: Route not null, correct segment counts, basic structure
- NO specificity value testing (implementation detail)
- NO exact point calculations

**Sections 4-7 (parser-04 to parser-07): Semantic Validation**
- Focus: Do validation rules catch errors correctly?
- Assertions: PatternException thrown with correct NURU_S### error code
- Tests duplicate parameters, optional ordering, catch-all placement

**Section 8 (parser-08): Option Parsing**
- Focus: Boolean flags, required/optional values, aliases
- Tests all option modifier combinations

**Section 9 (parser-09): End-of-Options Separator**
- Focus: `--` separator parsing and validation
- Tests placement rules and semantic errors

**Section 10 (parser-10): Relative Specificity**
- Focus: Which route ranks higher when multiple routes could match?
- Uses real-world examples (git, deploy commands)
- Assertions: `route1.Specificity > route2.Specificity` (relative ordering)

**Section 11 (parser-11): Complex Integration**
- Focus: Real-world CLI patterns (docker, git, kubectl, npm)
- Tests multiple features working together

**Sections 12-13 (parser-12, parser-13): Error Reporting**
- parser-12: Comprehensive error coverage (semantic + parse + modifier syntax)
- parser-13: Syntax validation edge cases (incomplete patterns, invalid identifiers)
- All NURU_P### and NURU_S### error codes tested

### Why This Structure?

1. **Tests design intent, not implementation**: Relative ordering matters, exact point values don't
2. **Resilient to refactoring**: Can change scoring values without breaking tests
3. **Clear failure messages**: "route1 should rank higher than route2" vs "expected 47 but got 45"
4. **Matches documentation**: Uses same examples from specificity-algorithm.md
5. **Progressive complexity**: Simple checks first, complex behavior last

### Methodology

Following successful lexer testing approach:

1. **Single-file Scripts**: Use .NET 10 file-based apps with `#!/usr/bin/dotnet --`
2. **Systematic Coverage**: One section at a time, complete before moving on
3. **Clear Test Names**: Descriptive names indicating what is tested
4. **Both Positive and Negative**: Valid patterns AND error cases
5. **Real-world Examples**: Actual CLI patterns from popular tools
