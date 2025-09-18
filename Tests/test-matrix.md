# Test Matrix for Optional Options Implementation

Based on Documentation/Developer/Design/route-syntax-and-specificity.md

## Core Option Patterns

### 1. Required Flag with Required Value
**Pattern**: `--flag {value}`
**Current Behavior**: ✅ Works (flag must be present with value)
**Target Behavior**: ✅ Same (no change needed)
**Test File**: `test-required-flag-required-value.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Flag with value | `build --config debug` | Match, value="debug" | ✅ Works |
| Flag without value | `build --config` | No match (error) | ✅ Works |
| Missing flag | `build` | No match | ✅ Works |

### 2. Required Flag with Optional Value
**Pattern**: `--flag {value?}`
**Current Behavior**: ✅ Works when flag present, ❌ flag incorrectly required
**Target Behavior**: Flag required, value optional
**Test File**: `test-required-flag-optional-value.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Flag with value | `build --config debug` | Match, value="debug" | ✅ Works |
| Flag without value | `build --config` | Match, value=null | ✅ Works |
| Missing flag | `build` | No match | ✅ Correct |

### 3. Optional Flag with Required Value
**Pattern**: `--flag? {value}`
**Current Behavior**: ❌ Not supported
**Target Behavior**: Flag optional, but if present needs value
**Test File**: `test-optional-flag-required-value.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Flag with value | `build --config debug` | Match, value="debug" | ❌ Needs implementation |
| Flag without value | `build --config` | No match (error) | ❌ Needs implementation |
| Missing flag | `build` | Match, value=null | ❌ Currently fails |

### 4. Optional Flag with Optional Value
**Pattern**: `--flag? {value?}`
**Current Behavior**: ❌ Not supported
**Target Behavior**: Both flag and value optional
**Test File**: `test-optional-flag-optional-value.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Flag with value | `build --config debug` | Match, value="debug" | ❌ Needs implementation |
| Flag without value | `build --config` | Match, value=null | ❌ Needs implementation |
| Missing flag | `build` | Match, value=null | ❌ Currently fails |

### 5. Boolean Flags (Always Optional)
**Pattern**: `--flag`
**Current Behavior**: ✅ Works (optional as expected)
**Target Behavior**: ✅ Same
**Test File**: `test-boolean-flags.cs` (RENAME from test-truly-optional-options.cs)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Flag present | `test --verbose` | Match, verbose=true | ✅ Works |
| Flag absent | `test` | Match, verbose=false | ✅ Works |

## Array/Repeated Option Patterns

### 6. Repeated Options
**Pattern**: `--flag {value}*`
**Current Behavior**: ❌ Not supported
**Target Behavior**: Flag can appear multiple times
**Test File**: `test-repeated-required-options.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Single occurrence | `docker build --tag v1` | Match, tags=["v1"] | ❌ Needs implementation |
| Multiple occurrences | `docker build --tag v1 --tag v2` | Match, tags=["v1","v2"] | ❌ Needs implementation |
| No occurrences | `docker build` | No match (required) | ❌ Needs implementation |

### 7. Optional Repeated Options
**Pattern**: `--flag? {value}*`
**Current Behavior**: ❌ Not supported
**Target Behavior**: Flag optional, can repeat
**Test File**: `test-repeated-optional-options.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Multiple occurrences | `curl api.com --header H1 --header H2` | Match, headers=["H1","H2"] | ❌ Needs implementation |
| No occurrences | `curl api.com` | Match, headers=null/empty | ❌ Needs implementation |

## Positional Parameter Rules

### 8. Optional After Required
**Pattern**: `copy {source} {dest?}`
**Current Behavior**: ✅ Works
**Target Behavior**: ✅ Same
**Test File**: `test-positional-optional-after-required.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Both provided | `copy a.txt b.txt` | Match, source="a.txt", dest="b.txt" | ✅ Works |
| Optional omitted | `copy a.txt` | Match, source="a.txt", dest=null | ✅ Works |

### 9. Catch-All
**Pattern**: `git add {*files}`
**Current Behavior**: ✅ Works
**Target Behavior**: ✅ Same
**Test File**: `test-catch-all.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Multiple values | `git add a.txt b.txt c.txt` | Match, files=["a.txt","b.txt","c.txt"] | ✅ Works |
| Single value | `git add a.txt` | Match, files=["a.txt"] | ✅ Works |
| No values | `git add` | Match, files=[] | ✅ Works |

### 10. Catch-All with Options
**Pattern**: `kubectl get {*resources} --namespace? {ns?}`
**Current Behavior**: ⚠️ Partial - options must come before catch-all
**Target Behavior**: Options can come after catch-all
**Test File**: `test-catch-all-with-options.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| Resources only | `kubectl get pods svc` | Match, resources=["pods","svc"] | ✅ Works |
| With option after | `kubectl get pods --namespace prod` | Match, resources=["pods"], ns="prod" | ⚠️ Needs testing |

## Complex Patterns for Interception

### 11. Mixed Required and Optional Options
**Pattern**: `deploy --env {env} --version? {ver?} --force`
**Current Behavior**: ❌ All options required
**Target Behavior**: --env required, --version optional, --force optional
**Test File**: `test-mixed-required-optional.cs` (TO CREATE)

| Test Case | Input | Expected Result | Current Status |
|-----------|-------|-----------------|----------------|
| All options | `deploy --env prod --version v1 --force` | Match | ❌ Needs implementation |
| Required only | `deploy --env prod` | Match | ❌ Currently fails |
| Missing required | `deploy --version v1` | No match | ❌ Needs implementation |

## Implementation Priority

1. **HIGH**: Optional flag syntax (`--flag?`)
2. **HIGH**: Mixed required/optional options
3. **MEDIUM**: Repeated options (`{value}*`)
4. **LOW**: Complex edge cases

## Test Files Summary

### New Test Files to Create (11 files)
1. `test-required-flag-required-value.cs` - Basic required flag behavior
2. `test-required-flag-optional-value.cs` - Required flag, optional value
3. `test-optional-flag-required-value.cs` - Optional flag with `?` modifier
4. `test-optional-flag-optional-value.cs` - Both flag and value optional
5. `test-repeated-required-options.cs` - Repeated flags with `*` modifier
6. `test-repeated-optional-options.cs` - Optional repeated flags
7. `test-positional-optional-after-required.cs` - Positional parameter ordering
8. `test-catch-all.cs` - Basic catch-all behavior
9. `test-catch-all-with-options.cs` - Catch-all mixed with options
10. `test-mixed-required-optional.cs` - Complex mixed patterns
11. `test-interception-patterns.cs` - Real-world command examples (git, docker, kubectl)

### Existing Files to Rename
- `test-truly-optional-options.cs` → `test-boolean-flags.cs`

### Existing Files to Delete/Archive
- `test-optional-option-params.cs` - Confusing expectations, replace with specific tests
- `test-option-combinations.cs` - Wrong pattern, replace with new tests
- `test-four-optional-options.cs` - Needs complete rewrite