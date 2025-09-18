# Parser Test Matrix for Optional and Repeated Syntax

## New Syntax Features to Test

### 1. Optional Flag Modifiers (`?`)

| Pattern | Description | Should Parse? | Expected Result |
|---------|------------|---------------|-----------------|
| `--flag?` | Optional flag without parameter | ✅ Yes | OptionMatcher with IsOptional=true |
| `--flag? {param}` | Optional flag with required parameter | ✅ Yes | OptionMatcher with IsOptional=true, parameter required |
| `--flag? {param?}` | Optional flag with optional parameter | ✅ Yes | Both flag and parameter optional |
| `--flag {param?}` | Required flag with optional parameter | ✅ Yes | Flag required, parameter optional |
| `-f?` | Short optional flag | ✅ Yes | OptionMatcher with IsOptional=true |
| `--flag?? ` | Double question mark | ❌ No | Parse error |
| `?--flag` | Question mark before flag | ❌ No | Parse error |

### 2. Repeated Option Modifiers (`*`)

| Pattern | Description | Should Parse? | Expected Result |
|---------|------------|---------------|-----------------|
| `--env {var}*` | Repeated option with parameter | ✅ Yes | OptionMatcher with IsRepeated=true |
| `--flag*` | Repeated boolean flag | ✅ Yes | OptionMatcher with IsRepeated=true |
| `--port {num:int}*` | Repeated typed parameter | ✅ Yes | Typed repeated option |
| `--env {var}**` | Double asterisk | ❌ No | Parse error |
| `--env* {var}` | Asterisk on flag not parameter | ❌ No | Parse error |
| `*--env` | Asterisk before flag | ❌ No | Parse error |

### 3. Combined Modifiers

| Pattern | Description | Should Parse? | Expected Result |
|---------|------------|---------------|-----------------|
| `--env? {var}*` | Optional repeated option | ✅ Yes | Optional and repeated |
| `--flag?*` | Optional repeated boolean | ✅ Yes | Optional and repeated |
| `--opt? {val?}*` | All modifiers combined | ✅ Yes | Everything optional and repeated |
| `--flag*?` | Wrong modifier order | ❌ No | Parse error |

### 4. Complex Patterns

| Pattern | Description | Should Parse? | Expected Result |
|---------|------------|---------------|-----------------|
| `deploy {env} --force? --dry-run?` | Multiple optional flags | ✅ Yes | Two optional flags |
| `docker run --env {var}* --volume {vol}*` | Multiple repeated options | ✅ Yes | Two repeated options |
| `build --config? {mode} --verbose?` | Mix of required and optional | ✅ Yes | Mixed options |
| `test {file} --repeat {n:int}* --verbose?` | Typed repeated with optional | ✅ Yes | Complex combination |

## Test Files to Create

1. **test-parser-optional-flags.cs** - Tests for `?` modifier on flags
2. **test-parser-repeated-options.cs** - Tests for `*` modifier on parameters
3. **test-parser-mixed-modifiers.cs** - Tests for combined `?` and `*`
4. **test-parser-error-cases.cs** - Invalid syntax that should fail

## Parser Components to Modify

1. **RouteParser** - Recognize `?` and `*` modifiers
2. **OptionSyntax** - Add IsOptional and IsRepeated properties
3. **ParameterSyntax** - Add IsRepeated property
4. **RouteCompiler** - Create appropriate matchers based on modifiers
5. **OptionMatcher** - Add IsOptional and IsRepeated properties

## Validation Rules

1. `?` can only appear once per flag/parameter
2. `*` can only appear once per parameter
3. `*` must be on the parameter, not the flag itself
4. Modifiers must be in correct position (end of flag/parameter)
5. Combined modifiers must be in order: `?` before `*`