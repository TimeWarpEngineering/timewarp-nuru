# Support Alternative Option-Value Separators

## Goal
Extend TimeWarp.Nuru's option parsing to support alternative syntaxes commonly used by existing CLI applications, enabling better CLI interception capabilities for wrapping/extending third-party command-line tools.

## Background

TimeWarp.Nuru is designed to intercept and wrap existing CLI applications. To maximize compatibility, it needs to support the various option-value syntax patterns used across different ecosystems.

### Current Support
- ✅ **POSIX-style**: `-x value`, `--option value` (space-separated)
  - Example: `deploy --env prod` matches route `deploy --env {environment}`

### Missing Support
Different CLI ecosystems use alternative separators that Nuru cannot currently parse:

1. **Equals syntax** (npm, many modern tools):
   - `-x=value`
   - `--option=value`
   - Example: `npm install --registry=https://registry.npmjs.org`

2. **Colon syntax** (MSBuild, .NET tools):
   - `-p:Property=Value`
   - `/p:Configuration=Release`
   - Example: `dotnet build -p:Configuration=Release`

3. **Concatenated short options** (tar, curl, many Unix tools):
   - `-xvf` (multiple boolean flags)
   - `-xvalue` (short option with immediate value)
   - Example: `tar -xzf archive.tar.gz`

## Design Questions

### 1. Equals Separator (`=`)

**Should**: `deploy --env=prod` match route `deploy --env {environment}`?

**Parsing options**:
- Split on first `=`: `--env=prod` → option: `--env`, value: `prod`
- Support in both long and short forms: `--option=value` and `-x=value`

**Impact on filtering**:
- Current filtering: `--Section:Key=value` (config overrides)
- Would need to distinguish between option syntax and config override
- Possible rule: Filter only if pattern is `--*:*=*` (contains colon)

### 2. Colon Separator (`:`)

**Should**: `build -p:Configuration=Release` match route `build -p {property}`?

**Parsing complexity**:
- Single colon: `-p:value` → option: `-p`, value: `:value` OR value: `value`?
- MSBuild pattern: `-p:Key=Value` → How to extract? Full string or split?
- Nested structure: `-p:Logging:LogLevel=Debug` → option or config override?

**Conflict with config override filtering**:
- Currently filters `--Section:Key=value` (double-dash + colon)
- Should single-dash with colon be treated differently?
- Example: Should `-p:Config=Debug` be filtered or parsed?

### 3. Concatenated Short Options

**Should**: `tar -xzf archive.tar.gz` be supported?

**Parsing approaches**:
- Expand `-xzf` to `["-x", "-z", "-f"]` for boolean options
- If last option expects value, treat remaining as value: `-xvalue` → `-x` with value `value`

**Route pattern implications**:
- Would need to define: `tar -x -z -f {file}` but accept `tar -xzf {file}`
- Ambiguity: How to know if `-xvalue` is `-x value` vs `-x` + `-v` + `-a` + `-l` + `-u` + `-e`?

### 4. Windows-Style Options

**Should Windows-style** `/option:value` **be supported?**

**Considerations**:
- Used by MSBuild, Visual Studio tools, Windows batch tools
- Conflicts with file paths: `/path/to/file` vs `/option`
- Could be opt-in behavior

## Questions for Decision

### Core Design Philosophy

1. **Automatic support vs explicit opt-in?**
   - Should alternative separators work automatically for all routes?
   - Or require explicit syntax in route pattern: `build -p=|:{property}` to indicate accepted separators?

2. **Parsing precedence?**
   - If `--option=value:test` is provided, which separator takes priority?
   - Should it be `--option` with value `=value:test`, or split on `=`, or split on `:`?

3. **Backward compatibility?**
   - Will this break existing routes that expect literal values containing `=` or `:`?
   - Example: `echo {text}` with input `x=5` - should this split or stay as literal?

### Filtering Interaction

4. **How should filtering interact with separators?**
   - Current: Filter `--Section:Key=value` before route matching
   - If we parse `-p:Config=Debug` as option `-p` with value `:Config=Debug`, does filtering still apply?
   - Should filtering only apply to double-dash patterns?

5. **Should catch-all parameters capture unparsed strings?**
   - Example: `docker {*args}` with input `run --name=app -p:8080`
   - Should args be `["run", "--name", "app", "-p", "8080"]` (split) or `["run", "--name=app", "-p:8080"]` (original)?

### Implementation Strategy

6. **Where should parsing happen?**
   - In the lexer/tokenizer before route matching?
   - In the option matcher as a fallback when exact match fails?
   - As a preprocessing step in NuruApp.RunAsync()?

7. **Should there be an escape mechanism?**
   - How to pass literal `--option=value` without splitting?
   - Should end-of-options `--` disable alternative parsing?

## Real-World Use Cases

### MSBuild Wrapper
```csharp
// User runs: myapp build -p:Configuration=Release -p:Platform=x64
.AddRoute("build -p {properties}", (string[] properties) => ...)

// Should properties be:
// A) ["-p:Configuration=Release", "-p:Platform=x64"] (no split)
// B) ["Configuration=Release", "Platform=x64"] (split on -p:)
// C) Something else?
```

### NPM Wrapper
```csharp
// User runs: myapp install --registry=https://custom.org --save-dev
.AddRoute("install --registry {url} --save-dev", (string url, bool saveDev) => ...)

// Should this work, or require: install --registry https://custom.org --save-dev?
```

### Git-like Tool
```csharp
// User runs: myapp commit -m=message --author=name@example.com
.AddRoute("commit -m {message} --author? {author?}", (string message, string? author) => ...)

// Should this parse automatically?
```

## Success Criteria

- [ ] Decision made on which separators to support (=, :, concatenated, /)
- [ ] Decision made on parsing strategy (automatic, opt-in, or explicit syntax)
- [ ] Interaction with config override filtering clearly defined
- [ ] Backward compatibility impact assessed
- [ ] Implementation approach documented
- [ ] Test strategy defined for new syntax patterns
- [ ] Migration guide for existing routes (if breaking changes)

## Related Issues

- Issue #77: Arguments with colons should not be filtered (configuration overrides)
- Test: `routing-12-colon-filtering.cs` - validates current filtering behavior

## Notes

This task emerged from investigation of test `Should_not_filter_single_dash_with_colon`, which revealed that Nuru's CLI interception capabilities could be significantly enhanced by supporting alternative option syntax patterns commonly found in real-world CLI applications.

The decision here will impact:
- Route pattern syntax documentation
- Lexer/parser implementation
- Option matching logic
- Configuration override filtering
- Help text generation
- Shell completion suggestions
