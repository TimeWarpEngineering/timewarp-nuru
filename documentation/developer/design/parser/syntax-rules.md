# Syntax Rules

Route pattern syntax rules enforced by the Parser. These rules prevent ambiguous or malformed patterns.

## Error Categories

Nuru validates route patterns in two phases:

1. **Parse Errors (NURU_P###)**: Syntax errors detected during parsing - malformed syntax that cannot be parsed into valid AST
2. **Semantic Errors (NURU_S###)**: Validation errors detected after parsing - logically invalid patterns that create ambiguity

---

## Parse Errors (NURU_P###)

Parse errors are detected during lexical analysis and parsing, before semantic validation runs. These represent malformed syntax that cannot be parsed into a valid Abstract Syntax Tree (AST).

### NURU_P001: Invalid Parameter Syntax

Parameters must use curly braces `{}`, not angle brackets `<>`.

```csharp
// ❌ Error: Invalid parameter syntax
.AddRoute("deploy <env>", handler);

// ✅ Correct: Use curly braces
.AddRoute("deploy {env}", handler);
```

**Why:** Curly braces are the standard syntax for parameters in route patterns. Angle brackets are reserved for other uses.

### NURU_P002: Unbalanced Braces

All opening braces `{` must have matching closing braces `}`.

```csharp
// ❌ Error: Missing closing brace
.AddRoute("deploy {env", handler);

// ❌ Error: Missing opening brace
.AddRoute("deploy env}", handler);

// ✅ Correct: Balanced braces
.AddRoute("deploy {env}", handler);
```

**Why:** Unbalanced braces create ambiguity about where parameters begin and end.

### NURU_P003: Invalid Option Format

Multi-character options must use double dash `--`. Single dash `-` is only for single-character options.

```csharp
// ❌ Error: Multi-character with single dash
.AddRoute("build -verbose", handler);

// ✅ Correct: Double dash for multi-character
.AddRoute("build --verbose", handler);

// ✅ Correct: Single dash for single character
.AddRoute("build -v", handler);
```

**Why:** This follows POSIX conventions where `-v` and `--verbose` are distinct but related options.

### NURU_P004: Invalid Type Constraint

Type constraints must be one of the supported types.

**Supported types:** `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`

```csharp
// ❌ Error: Unsupported type 'integer'
.AddRoute("process {id:integer}", handler);

// ❌ Error: Unsupported type 'float'
.AddRoute("calc {value:float}", handler);

// ✅ Correct: Use 'int'
.AddRoute("process {id:int}", handler);

// ✅ Correct: Use 'double' for floating point
.AddRoute("calc {value:double}", handler);
```

**Why:** Only explicitly supported types ensure proper parameter binding and type conversion.

### NURU_P005: Invalid Character

Route patterns must contain only valid characters. Unsupported special characters are rejected.

```csharp
// ❌ Error: Invalid character '@'
.AddRoute("test @param", handler);

// ❌ Error: Invalid character '#'
.AddRoute("build #target", handler);

// ✅ Correct: Use parameters for dynamic values
.AddRoute("test {param}", handler);
```

**Why:** Restricts syntax to prevent ambiguity and ensure patterns are parseable.

### NURU_P006: Unexpected Token

Parser encountered unexpected syntax during pattern parsing.

```csharp
// ❌ Error: Unexpected '}'
.AddRoute("test }", handler);

// ❌ Error: Expected parameter name
.AddRoute("build --config {}", handler);

// ✅ Correct: Complete parameter syntax
.AddRoute("build --config {mode}", handler);
```

**Why:** Indicates incomplete or malformed syntax that doesn't match expected grammar.

### NURU_P007: Null Route Pattern

Route pattern string cannot be null.

```csharp
// ❌ Error: Null pattern
string? pattern = null;
builder.AddRoute(pattern!, handler);

// ✅ Correct: Valid pattern string
builder.AddRoute("valid-pattern", handler);
```

**Why:** Null patterns have no semantic meaning and indicate a programming error.

---

## Semantic Errors (NURU_S###)

Semantic errors are detected after successful parsing, during validation of the route's logical structure. These patterns are syntactically valid but create ambiguity or conflicts.

## Core Syntax Rules

### Parameter Modifiers

| Syntax | Meaning | Example |
|--------|---------|---------|
| `{param}` | Required positional parameter | `deploy {env}` - env must be provided |
| `{param?}` | Optional positional parameter | `deploy {env?}` - env can be omitted |
| `{param:type}` | Typed parameter | `wait {seconds:int}` - must be integer |
| `{param:type?}` | Optional typed parameter | `wait {seconds:int?}` - optional integer |
| `{*param}` | Catch-all (remaining args) | `exec {cmd} {*args}` - captures rest |

### Positional Parameter Rules

#### NURU_S006: Optional Parameters Must Come After Required

Optional positional parameters must appear after all required parameters.

```csharp
// ✅ Valid: Required before optional
.AddRoute("copy {source} {dest?}", (string source, string? dest) => ...)

// ❌ Error NURU_S006: Optional before required
.AddRoute("copy {source?} {dest}", (string? source, string dest) => ...)
```

**Why:** Ambiguity - input `"copy file.txt"` could be source (with dest omitted) or dest (with source omitted).

#### NURU_S002: Only ONE Optional Positional Parameter Allowed

Having multiple consecutive optional parameters creates parsing ambiguity.

```csharp
// ✅ Valid: Single optional at end
.AddRoute("deploy {env} {version?}", (string env, string? version) => ...)

// ❌ Error NURU_S002: Multiple consecutive optionals
.AddRoute("deploy {env?} {version?}", (string? env, string? version) => ...)
```

**Why:** Input `"deploy v2.0"` is ambiguous - is `v2.0` the env (with version omitted) or version (with env omitted)?

#### NURU_S003: Catch-all Must Be Last

Catch-all parameters must appear as the last positional parameter in the route.

```csharp
// ✅ Valid: Catch-all at end
.AddRoute("exec {cmd} {*args}", (string cmd, string[] args) => ...)

// ❌ Error NURU_S003: Catch-all not at end
.AddRoute("exec {*args} {cmd}", (string[] args, string cmd) => ...)
```

**Why:** Catch-all consumes all remaining arguments, leaving nothing for subsequent parameters.

#### NURU_S004: Cannot Mix Optional Parameters with Catch-all

Routes cannot contain both optional parameters and catch-all parameters.

```csharp
// ❌ Error NURU_S004: Optional with catch-all
.AddRoute("run {script?} {*args}", (string? script, string[] args) => ...)

// ✅ Valid: Required with catch-all
.AddRoute("run {script} {*args}", (string script, string[] args) => ...)

// ✅ Valid: Just optional (no catch-all)
.AddRoute("run {script?}", (string? script) => ...)
```

**Why:** Ambiguity - where does the optional parameter end and the catch-all begin?

#### NURU_S001: Duplicate Parameter Names

Each parameter name must be unique within a route pattern.

```csharp
// ❌ Error NURU_S001: Duplicate parameter 'arg'
.AddRoute("run {arg} {arg}", handler);

// ✅ Valid: Unique parameter names
.AddRoute("run {source} {dest}", handler);
```

**Why:** Parameters must have unique names for proper binding to handler arguments.

### Why These Rules Exist

**Ambiguity Prevention**: Consider `deploy {env?} {version?}`:
- Input: `deploy v2.0`
- Is `v2.0` the env with no version?
- Or is env omitted and `v2.0` is the version?
- **Undecidable!**

**Solution Patterns**:
```csharp
// Use options for multiple optional values
.AddRoute("deploy {env} --version? {ver?} --tag? {tag?}",
    (string env, string? ver, string? tag) => ...)
// Clear: positional is env, options are version and tag

// Or use subcommands for different patterns
.AddRoute("deploy latest {env}", (string env) => DeployLatest(env))
.AddRoute("deploy version {env} {ver}", (string env, string ver) => DeployVersion(env, ver))
```

### Arrays and Variable Arguments

Nuru handles arrays through the **catch-all** parameter syntax `{*param}`:

```csharp
// Git-style: Multiple files
.AddRoute("git add {*files}", (string[] files) => Git.Add(files))
// Matches: git add file1.txt file2.txt file3.txt

// npm-style: Multiple packages
.AddRoute("install {*packages}", (string[] packages) => Npm.Install(packages))
// Matches: install express mongoose dotenv

// Docker-style: Command with arguments
.AddRoute("docker run {image} {*args}", (string image, string[] args) =>
    Docker.Run(image, args))
// Matches: docker run ubuntu -it bash -c "echo hello"

// Calculator: Variable number of values
.AddRoute("stats {*values}", (string[] values) => {
    var numbers = values.Select(double.Parse).ToArray();
    return CalculateStats(numbers);
})
// Matches: stats 10 20 30 40 50
```

**Real-World Examples**:
- `git add file1.txt file2.txt file3.txt`
- `npm install express mongoose dotenv`
- `kubectl delete pods pod1 pod2 pod3`
- `rm *.txt *.log *.tmp`

**Typed Arrays (Future Enhancement)**:
```csharp
// Currently: string[] only
.AddRoute("sum {*numbers}", (string[] numbers) => {
    var nums = numbers.Select(int.Parse);  // Manual parsing
    return nums.Sum();
})

// Potential future: Typed catch-all
.AddRoute("sum {*numbers:int}", (int[] numbers) => numbers.Sum())
// Not currently supported but tracked in ToDo item 007
```

**Catch-All Rules**:
1. Must be the **last** parameter in the route
2. Captures **all remaining** arguments
3. Cannot be optional (always gets at least empty array)
4. Cannot mix with optional positional parameters
5. Currently only supports `string[]` type

### End-of-Options Separator (--)

The double dash `--` serves as an end-of-options marker (POSIX standard) that signals all following arguments should be treated as positional parameters, even if they start with `-` or `--`.

```csharp
// Pass remaining args to command (prevents option interpretation)
.AddRoute("exec {cmd} -- {*args}",
    (string cmd, string[] args) => Shell.Run(cmd, args))
// Matches: exec npm -- run build --watch
// cmd = "npm", args = ["run", "build", "--watch"]

// Git-style file disambiguation
.AddRoute("git log -- {*files}",
    (string[] files) => Git.Log(files))
// Matches: git log -- -README.md --version.txt
// files = ["-README.md", "--version.txt"] (dashes preserved as literals)

// Docker exec pattern
.AddRoute("docker exec {container} -- {*cmd}",
    (string container, string[] cmd) => Docker.Exec(container, cmd))
// Matches: docker exec web -- npm test --coverage
// container = "web", cmd = ["npm", "test", "--coverage"]

// Combined with options
.AddRoute("exec --env {e}* -- {*cmd}",
    (string[] e, string[] cmd) => ...)
// Matches: exec --env PATH=/bin --env USER=root -- ls -la
// e = ["PATH=/bin", "USER=root"], cmd = ["ls", "-la"]
```

**Semantic Validation Rules for `--` Separator**:

#### NURU_S007: Invalid End-of-Options Separator

The `--` separator must be followed by a catch-all parameter.

```csharp
// ❌ Error NURU_S007: No catch-all after --
.AddRoute("run --", handler);

// ✅ Valid: -- followed by catch-all
.AddRoute("run -- {*args}", handler);
```

**Why:** The `--` separator's purpose is to pass remaining arguments to the catch-all parameter.

#### NURU_S008: Options After End-of-Options Separator

Options cannot appear after the `--` separator.

```csharp
// ❌ Error NURU_S008: Option after --
.AddRoute("run -- {*args} --verbose", handler);

// ✅ Valid: Options before --
.AddRoute("run --verbose -- {*args}", handler);
```

**Why:** The `--` separator marks the end of option processing; everything after is treated as literal arguments.

**Additional Rules**:
1. Everything after `--` is treated as positional arguments
2. The `--` itself is not included in the captured arguments
3. Arguments after `--` preserve their literal form (dashes are not interpreted)

**Implementation Details**:
- Lexer tokenizes standalone `--` as `TokenType.EndOfOptions`
- Parser validates that only catch-all parameters follow `--` (NURU_S007)
- Parser validates that no options appear after `--` (NURU_S008)
- Route matching stops option processing after encountering `--`

### Arrays in Options - Design for Command Interception

Since Nuru's goal includes intercepting existing CLI commands, we need to handle common real-world patterns:

#### Pattern 1: Repeated Options (IMPLEMENTED)

Most common pattern in real CLIs. Same flag appears multiple times:

```csharp
// Syntax: {param}* indicates array from repeated flags
.AddRoute("docker build --build-arg {args}* {path}",
    (string[] args, string path) => ...)
// Intercepts: docker build --build-arg KEY1=val1 --build-arg KEY2=val2 .
// args = ["KEY1=val1", "KEY2=val2"]

.AddRoute("curl {url} --header {headers}*",
    (string url, string[] headers) => ...)
// Intercepts: curl api.com --header "Accept: json" --header "Auth: token"
// headers = ["Accept: json", "Auth: token"]

// Optional repeated flag
.AddRoute("git commit --message {msg} --author? {authors}*",
    (string msg, string[]? authors) => ...)
// Intercepts: git commit --message "fix"
// Intercepts: git commit --message "fix" --author "John" --author "Jane"
```

**Real commands that use this pattern**:
- `docker build --build-arg VAR1=val1 --build-arg VAR2=val2`
- `curl -H "Header1: value1" -H "Header2: value2"`
- `git -c config.key1=val1 -c config.key2=val2`
- `maven -D prop1=val1 -D prop2=val2`

#### Pattern 2: Multiple Positional Values (WORKS WITH CATCH-ALL)

Many CLIs accept multiple positional arguments, which catch-all handles well:

```csharp
// ✅ WORKS: Catch-all for multiple resources
.AddRoute("kubectl get {*resources} --namespace? {ns?} --output? {format?}",
    (string[] resources, string? ns, string? format) => ...)
// Intercepts: kubectl get pods svc deployments
// Intercepts: kubectl get pods --namespace prod --output json
// resources = ["pods", "svc", "deployments"], options parsed correctly

// ✅ WORKS: Multiple files with options
.AddRoute("git add {*files} --force --dry-run",
    (string[] files, bool force, bool dryRun) => ...)
// Intercepts: git add src/*.cs tests/*.cs --force
// files = ["src/file1.cs", "src/file2.cs", "tests/test1.cs"]

// ✅ WORKS: Command with args and options
.AddRoute("docker run {image} {*cmd} --detach --name? {name?}",
    (string image, string[] cmd, bool detach, string? name) => ...)
// Intercepts: docker run ubuntu bash -c "echo hello" --detach
// Note: Options can come after catch-all since they have '--' prefix
```

**Why this works**: Options with `-` or `--` prefixes are distinguishable from positional arguments, so the parser can correctly separate them.

#### Pattern 3: Delimiter-Separated Values (PARTIAL SUPPORT)

Single option value containing delimited list:

```csharp
// User handles splitting in their code
.AddRoute("javac --classpath {cp} {file}",
    (string cp, string file) => {
        var paths = cp.Split(':');  // or ';' on Windows
        JavaCompile(paths, file);
    })
// Intercepts: javac --classpath lib1.jar:lib2.jar Main.java
```

**Support level**: Pass as single string, let user split. This works today.

#### Pattern 4: Complex/Unusual Syntaxes (USE CREATIVE PATTERNS)

Some CLIs have unusual syntaxes, but we can still intercept them with creative route patterns:

```csharp
// tar with combined flags
.AddRoute("tar {flags} {file} {*files}",
    (string flags, string file, string[] files) => {
        if (flags.Contains('x')) Extract(file);
        if (flags.Contains('c')) Create(file, files);
    })
// Intercepts: tar xzvf archive.tar.gz
// Intercepts: tar czf archive.tar.gz file1 file2

// ps with flags that don't use dashes
.AddRoute("ps {flags?}",
    (string? flags) => {
        var showAll = flags?.Contains('a') ?? false;
        var showUser = flags?.Contains('u') ?? false;
        ProcessList(showAll, showUser);
    })
// Intercepts: ps aux
// Intercepts: ps

// dd with key=value pairs
.AddRoute("dd {*args}",
    (string[] args) => {
        var params = args.Select(a => a.Split('=')).ToDictionary(p => p[0], p => p[1]);
        DiskDump(params["if"], params["of"], params.GetValueOrDefault("bs"));
    })
// Intercepts: dd if=/dev/zero of=/dev/null bs=1M count=100

// find with complex boolean logic - capture it all, parse in handler
.AddRoute("find {path} {*conditions}",
    (string path, string[] conditions) => {
        // Parse -type, -name, -o (or), -a (and) in handler
        var query = ParseFindConditions(conditions);
        FindFiles(path, query);
    })
// Intercepts: find . -type f -name "*.txt" -o -name "*.md"
```

**Key Insight**: Nuru just needs to MATCH and CAPTURE. The handler can parse complex logic. This allows intercepting virtually any CLI command.

### Design Decision Summary

| Pattern | Support | Syntax | Example |
|---------|---------|--------|---------|
| Repeated options | ✅ FULL | `--flag {param}*` | `--tag latest --tag v1.0` |
| Optional repeated | ✅ FULL | `--flag? {param}*` | Can omit or repeat |
| Multiple positionals | ✅ FULL | `{*args}` | `kubectl get pods svc` |
| Delimited values | ✅ MANUAL | `--flag {param}` | `--cp lib1.jar:lib2.jar` (split in handler) |
| Complex/unusual syntax | ✅ CREATIVE | Various patterns | `tar xzvf`, `ps aux`, `dd if=...` |

**Bottom Line**: Between repeated options (`{param}*`), catch-all (`{*args}`), and creative patterns, Nuru can intercept virtually any CLI command. The handler does the parsing work when needed.

### Option Modifiers

| Syntax | Meaning | Example | Valid | Invalid |
|--------|---------|---------|-------|---------|
| `--flag` | Boolean flag (always optional) | `build --verbose` | `build`<br>`build --verbose` | - |
| `--flag {value}` | Required flag with required value | `build --config {mode}` | `build --config debug` | `build`<br>`build --config` |
| `--flag {value?}` | Required flag with optional value | `build --config {mode?}` | `build --config debug`<br>`build --config` | `build` |
| `--flag? {value}` | Optional flag with required value (if present) | `build --config? {mode}` | `build`<br>`build --config debug` | `build --config` |
| `--flag? {value?}` | Optional flag with optional value | `build --config? {mode?}` | `build`<br>`build --config`<br>`build --config debug` | - |

#### NURU_S005: Option with Duplicate Alias

Options cannot have the same short form (alias) specified multiple times.

```csharp
// ❌ Error NURU_S005: Duplicate alias '-c'
.AddRoute("build --config,-c {m} --count,-c {n}", handler);

// ✅ Valid: Unique aliases
.AddRoute("build --config,-c {m} --count,-n {n}", handler);

// ✅ Valid: Option aliases allow both forms
.AddRoute("build --verbose,-v", handler);
// Matches: build --verbose
// Matches: build -v
```

**Why:** Short form aliases must be unique to avoid ambiguity when parsing command-line arguments.

## Required vs Optional Options - Explicit Syntax

### Self-Contained Route Patterns

```csharp
// Required flag: No ? on flag
.AddRoute("deploy --env {env}", (string env) => ...)
// Must provide: deploy --env production
// Won't match: deploy

// Optional flag: ? on flag
.AddRoute("deploy --env? {env}", (string? env) => ...)
// Can provide: deploy --env production (env = "production")
// Can omit: deploy (env = null)

// Optional flag with optional value
.AddRoute("deploy --env? {env?}", (string? env) => ...)
// Can omit flag: deploy (env = null)
// Can provide flag only: deploy --env (env = null)
// Can provide both: deploy --env production (env = "production")

// Mixed requirement levels
.AddRoute("deploy --env {env} --config? {cfg?} --force",
    (string env, string? cfg, bool force) => ...)
// Required: --env with value (no ? on flag)
// Optional: --config flag and value (? on both)
// Optional: --force (booleans always optional)
```

### Why This Works for Interception

```csharp
// Specific handler for production deploys (higher specificity)
.AddRoute("deploy --env production --force", (bool force) => {
    // Intercept production deploys
    if (!force && !Confirm("Deploy to prod?")) return;
    ProductionDeploy();
})

// General handler for other environments (lower specificity)
.AddRoute("deploy --env {env}", (string env) => {
    // Standard deploy path
    StandardDeploy(env);
})

// Fallback for deploy without options
.AddRoute("deploy {env?}", (string? env) => {
    // Legacy positional parameter support
    var target = env ?? "staging";
    StandardDeploy(target);
})
```

## Related Documents

See [specificity-algorithm.md](../resolver/specificity-algorithm.md) for how route patterns are scored and matched.
See [parameter-optionality.md](../cross-cutting/parameter-optionality.md) for nullability-based optionality design.
