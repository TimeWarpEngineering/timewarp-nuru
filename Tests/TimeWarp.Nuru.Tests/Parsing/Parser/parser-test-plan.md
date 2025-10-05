# Parser Test Plan

## Overview

This test plan ensures comprehensive coverage of the Nuru route pattern parser. The parser is responsible for:

1. **Syntax Validation** - Enforcing all parameter and option rules
2. **AST Construction** - Building abstract syntax tree from tokens
3. **Specificity Calculation** - Computing route specificity scores
4. **Route Matching** - Selecting the best matching route

The plan follows the systematic approach used in lexer testing, organized into 15 focused sections with an estimated 120-150 individual test cases.

## Design Document References

- `design/parser/syntax-rules.md` - Route pattern syntax rules and validation (Parser)
- `design/resolver/specificity-algorithm.md` - Route matching and scoring algorithm (Resolver)
- `design/cross-cutting/parameter-optionality.md` - Nullability-based optional/required design

See also: `guides/building-new-cli-apps.md` - Best practices for new CLI applications

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
   - Expected: ❌ NOT SUPPORTED per parameter-optionality.md
   - Reason: Can't mark flag as optional in pattern - use nullable param instead
   - Alternative: `build --config {mode?}` (flag required, value optional)

5. **Short Option Flag**
   - Pattern: `build -v`
   - Expected: Boolean flag, short form
   - Specificity: 50

6. **Short Option with Value**
   - Pattern: `build -c {mode}`
   - Expected: Short option with required value
   - Specificity: 50

7. **Option with Alias**
   - Pattern: `build --verbose,-v`
   - Expected: Long and short forms parsed as aliases
   - Both match the same option

8. **Option Alias with Value**
   - Pattern: `build --config,-c {mode}`
   - Expected: Both forms take the same value parameter
   - Specificity: 50

---

## Section 9: Required vs Optional Options

**Purpose**: Verify nullability-based required/optional option logic.

### Test Cases

1. **Non-nullable Parameter = Required Option**
   - Pattern: `build --config {mode}`
   - Handler: `(string mode) => ...`
   - Input `build` (missing --config): ❌ Route doesn't match
   - Input `build --config debug`: ✅ Matches, mode="debug"

2. **Nullable Parameter = Optional Option**
   - Pattern: `build --config {mode?}`
   - Handler: `(string? mode) => ...`
   - Input `build`: ✅ Matches, mode=null
   - Input `build --config debug`: ✅ Matches, mode="debug"

3. **Boolean Always Optional**
   - Pattern: `build --verbose`
   - Handler: `(bool verbose) => ...`
   - Input `build`: ✅ Matches, verbose=false
   - Input `build --verbose`: ✅ Matches, verbose=true

4. **Mixed Required and Optional Options**
   - Pattern: `deploy --env {e} --tag {t?} --verbose`
   - Handler: `(string e, string? t, bool verbose) => ...`
   - Input `deploy --env prod`: ✅ Matches (tag=null, verbose=false)
   - Input `deploy`: ❌ Doesn't match (missing required --env)

5. **Required Option with Optional Value Edge Case**
   - Pattern: `log --level {lvl?}`
   - Handler: `(string? lvl) => ...`
   - Input `log --level`: ✅ Matches, lvl=null (flag present, value omitted)
   - Input `log --level debug`: ✅ Matches, lvl="debug"
   - Input `log`: ❌ Doesn't match (flag itself is required)

---

## Section 10: Repeated Options (Arrays)

**Purpose**: Verify repeated option syntax for collecting multiple values.

### Test Cases

1. **Basic Repeated Option**
   - Pattern: `docker run --env {e}*`
   - Expected: Array parameter, can appear multiple times
   - Input: `docker run --env A --env B --env C`
   - Bound to: `string[] e = ["A", "B", "C"]`

2. **Typed Repeated Option**
   - Pattern: `process --id {id:int}*`
   - Expected: Typed array parameter
   - Input: `process --id 1 --id 2 --id 3`
   - Bound to: `int[] id = [1, 2, 3]`

3. **Repeated Option with Alias**
   - Pattern: `docker run --env,-e {e}*`
   - Expected: Both forms contribute to same array
   - Input: `docker run --env A -e B --env C`
   - Bound to: `string[] e = ["A", "B", "C"]`

4. **Zero Occurrences of Repeated Option**
   - Pattern: `docker run --env {e}*`
   - Input: `docker run` (no --env)
   - Bound to: `string[] e = []` (empty array)
   - Route matches: ✅ (repeated options implicitly optional)

5. **Mixed Repeated and Single Options**
   - Pattern: `deploy --env {e} --tag {t}* --verbose`
   - Input: `deploy --env prod --tag v1 --tag v2 --verbose`
   - Bound to: `(string e, string[] t, bool verbose)`

6. **Repeated Option Specificity**
   - Pattern: `run --flag {f}*`
   - Specificity: 50 (required option score, repetition doesn't change)

---

## Section 11: End-of-Options Separator

**Purpose**: Verify `--` end-of-options separator handling.

### Test Cases

1. **End-of-Options with Catch-all**
   - Pattern: `run -- {*args}`
   - Expected: `--` token followed by catch-all parameter
   - Input: `run -- --not-a-flag file.txt`
   - Bound to: `string[] args = ["--not-a-flag", "file.txt"]`
   - Reason: Everything after `--` treated as literals, not options

2. **Required Parameter Then End-of-Options**
   - Pattern: `execute {script} -- {*args}`
   - Input: `execute run.sh -- --verbose file.txt`
   - Bound to: `(string script, string[] args)` where script="run.sh", args=["--verbose", "file.txt"]

3. **Invalid: End-of-Options Without Catch-all**
   - Pattern: `run --`
   - Expected: ❌ Error (likely NURU_S007)
   - Reason: `--` must be followed by catch-all parameter

4. **Invalid: Options After End-of-Options**
   - Pattern: `run -- {*args} --verbose`
   - Expected: ❌ Error (likely NURU_S008)
   - Reason: No options allowed after `--` separator

5. **Edge Case: Empty Args After Separator**
   - Pattern: `run -- {*args}`
   - Input: `run --`
   - Bound to: `string[] args = []` (empty array)
   - Valid: ✅ Separator present, no args following

---

## Section 12: Specificity Calculation

**Purpose**: Verify the 7-tier specificity scoring system is correctly calculated.

### Specificity Score Table (from design docs)

| Element | Score | Example |
|---------|-------|---------|
| Literal | 100 | `deploy`, `status` |
| Required Option | 50 | `--config {mode}`, `--verbose` |
| Optional Option | 25 | `--config? {mode?}` (NOT SUPPORTED) |
| Typed Parameter | 20 | `{id:int}`, `{when:datetime}` |
| Untyped Parameter | 10 | `{name}`, `{value}` |
| Optional Parameter | 5 | `{tag?}`, `{id:int?}` |
| Catch-all | 1 | `{*args}`, `{*rest}` |

### Test Cases

1. **Pure Literal Route**
   - Pattern: `git status`
   - Score: 200 (100 + 100)

2. **Literal + Required Parameter**
   - Pattern: `greet {name}`
   - Score: 110 (100 + 10)

3. **Literal + Typed Parameter**
   - Pattern: `delay {ms:int}`
   - Score: 120 (100 + 20)

4. **Literal + Optional Parameter**
   - Pattern: `deploy {env?}`
   - Score: 105 (100 + 5)

5. **Literal + Catch-all**
   - Pattern: `docker {*args}`
   - Score: 101 (100 + 1)

6. **Required Options**
   - Pattern: `build --config {mode} --verbose`
   - Score: 100 (50 + 50)

7. **Mixed: Literal + Typed + Required Option**
   - Pattern: `deploy {env} --tag {t} --verbose`
   - Score: 160 (100 + 10 + 50)
   - Breakdown: "deploy"(100) + {env}(10) + --tag(50) + --verbose(50) = 210
   - Correction: Score: 210

8. **Complex: Multiple Literals and Parameters**
   - Pattern: `git commit -m {msg} --amend`
   - Score: 310 (100 + 100 + 50 + 50 + 10)
   - Breakdown: "git"(100) + "commit"(100) + -m(50) + --amend(50) + {msg}(10)

9. **Optional Parameter Scoring**
   - Pattern: `copy {source} {dest?}`
   - Score: 15 (10 + 5)
   - Note: Optional scores LOWER than typed (5 vs 20)

10. **Optional Typed Parameter**
    - Pattern: `delay {ms:int?}`
    - Score: 5 (optional trumps typed)
    - Note: Optional status determines score, not type

---

## Section 13: Route Matching & Selection

**Purpose**: Verify route matching logic and specificity-based selection.

### Test Cases

1. **Exact Match vs Catch-all**
   - Routes: `status` (200), `{*args}` (1)
   - Input: `status`
   - Selected: `status` (higher specificity)

2. **Typed vs Untyped Parameter**
   - Routes: `delay {ms:int}` (120), `delay {duration}` (110)
   - Input: `delay 500`
   - Selected: `delay {ms:int}` (120 > 110)

3. **Required vs Optional Parameter**
   - Routes: `deploy {env}` (110), `deploy {env?}` (105)
   - Input: `deploy prod`
   - Selected: `deploy {env}` (110 > 105)

4. **Literal Beats Parameter**
   - Routes: `git status` (200), `git {command}` (110)
   - Input: `git status`
   - Selected: `git status` (200 > 110)

5. **Equal Specificity: First Registered Wins**
   - Routes: `greet {name}` (110), `hello {person}` (110)
   - Input: `greet Alice`
   - Selected: First matching route (if both match, first wins)
   - Note: Different literals, so only first matches input "greet"

6. **Options Increase Specificity**
   - Routes: `build` (100), `build --verbose` (150)
   - Input: `build --verbose`
   - Selected: `build --verbose` (150 > 100)

7. **Required Option Doesn't Match Without Option**
   - Routes: `build --config {m}` (150), `build` (100)
   - Input: `build`
   - Selected: `build` (100) - first route requires --config, doesn't match

8. **Partial Option Match**
   - Routes: `deploy --env {e} --tag {t}` (200), `deploy --env {e}` (150)
   - Input: `deploy --env prod`
   - Selected: `deploy --env {e}` (150) - first route requires both options

9. **Catch-all as Fallback**
   - Routes: `git status` (200), `git commit` (200), `git {*args}` (101)
   - Input: `git push`
   - Selected: `git {*args}` (only matching route)

10. **Complex Multi-Route Selection**
    - Routes:
      - `deploy {env} --tag {t} --verbose` (210)
      - `deploy {env} --tag {t}` (160)
      - `deploy {env}` (110)
      - `deploy` (100)
    - Input: `deploy prod --tag v1.0`
    - Selected: `deploy {env} --tag {t}` (160, highest matching specificity)

---

## Section 14: Complex Pattern Integration

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

## Section 15: Error Reporting & Edge Cases

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

## Implementation Strategy

### Phase 1: Core Parsing (Sections 1-3)
- Implement basic parameter parsing
- Add type constraint support
- Handle optional parameters

### Phase 2: Validation (Sections 4-7)
- Implement all 4 validation rules
- Comprehensive error reporting
- NURU error codes

### Phase 3: Options (Sections 8-10)
- Option modifier parsing
- Required/optional option logic
- Repeated options (arrays)

### Phase 4: Advanced (Sections 11-13)
- End-of-options separator
- Specificity calculation
- Route matching and selection

### Phase 5: Integration (Sections 14-15)
- Complex real-world patterns
- Edge cases and error scenarios
- Full error code coverage

---

## Success Criteria

- [ ] All 15 sections implemented
- [ ] 120-150 individual test cases passing
- [ ] 100% coverage of validation rules (NURU001-NURU008+)
- [ ] Complete specificity scoring verification
- [ ] All option modifier combinations tested
- [ ] Real-world CLI pattern validation
- [ ] Comprehensive error reporting verified

---

## Testing Approach

Following the successful lexer testing methodology:

1. **Test-Driven Development**: Write tests before implementation
2. **Single-file Scripts**: Use .NET 10 file-based apps with `#!/usr/bin/dotnet --`
3. **Systematic Coverage**: One section at a time, complete before moving on
4. **Clear Test Names**: Descriptive names indicating what is tested
5. **Both Positive and Negative**: Valid patterns AND error cases
6. **Real-world Examples**: Use actual CLI patterns from popular tools

---

## Estimated Timeline

- **Section 1-3** (Core): ~3-4 hours
- **Section 4-7** (Validation): ~4-5 hours
- **Section 8-10** (Options): ~4-5 hours
- **Section 11-13** (Advanced): ~3-4 hours
- **Section 14-15** (Integration): ~3-4 hours

**Total**: ~17-22 hours for complete parser test coverage

---

## Notes

- Lexer tests achieved 100% coverage with 14 sections and ~100 tests
- Parser is more complex: 15 sections, 120-150 tests estimated
- Focus on validation rules - these are the most error-prone areas
- Specificity calculation is critical for route selection
- Real-world patterns validate the entire system works together
