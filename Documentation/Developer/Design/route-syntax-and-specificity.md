# Route Syntax and Specificity Design

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

1. **Optional parameters must come after all required parameters**
   ```csharp
   // ✅ Valid: Required before optional
   .AddRoute("copy {source} {dest?}", (string source, string? dest) => ...)

   // ❌ Invalid: Optional before required
   .AddRoute("copy {source?} {dest}", (string? source, string dest) => ...)
   ```

2. **Only ONE optional positional parameter allowed (no consecutive optionals)**
   ```csharp
   // ✅ Valid: Single optional at end
   .AddRoute("deploy {env} {version?}", (string env, string? version) => ...)

   // ❌ Invalid: Multiple consecutive optionals (ambiguous)
   .AddRoute("deploy {env?} {version?}", (string? env, string? version) => ...)
   // Which arg is env vs version? "deploy v2.0" is ambiguous
   ```

3. **Catch-all must be last**
   ```csharp
   // ✅ Valid: Catch-all at end
   .AddRoute("exec {cmd} {*args}", (string cmd, string[] args) => ...)

   // ❌ Invalid: Catch-all not at end
   .AddRoute("exec {*args} {cmd}", (string[] args, string cmd) => ...)
   ```

4. **Cannot mix optional parameters with catch-all**
   ```csharp
   // ❌ Invalid: Optional param with catch-all (NURU008)
   .AddRoute("run {script?} {*args}", (string? script, string[] args) => ...)
   // Use one or the other, not both
   ```

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

**Rules for `--` Separator**:
1. `--` must be followed by a catch-all parameter (`{*param}`)
2. No options can appear after `--`
3. Everything after `--` is treated as positional arguments
4. The `--` itself is not included in the captured arguments
5. Arguments after `--` preserve their literal form (dashes are not interpreted)

**Implementation Details**:
- Lexer tokenizes standalone `--` as `TokenType.EndOfOptions`
- Parser validates that only catch-all parameters follow `--`
- Route matching stops option processing after encountering `--`

### Arrays in Options - Design for Command Interception

Since Nuru's goal includes intercepting existing CLI commands, we need to handle common real-world patterns:

#### Pattern 1: Repeated Options (SHOULD SUPPORT)

Most common pattern in real CLIs. Same flag appears multiple times:

```csharp
// Proposed syntax: {param}* indicates array from repeated flags
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

### Specificity with Repeated Options

Repeated options affect route specificity:

```csharp
// Higher specificity: Specific repeated option
.AddRoute("docker build --build-arg {args}* --tag {tags}* {path}",
    (string[] args, string[] tags, string path) => ...)
// Score: 100 (docker) + 100 (build) + 50 (--build-arg) + 50 (--tag) + 10 (path) = 310

// Lower specificity: Generic catch-all
.AddRoute("docker build {*args}", (string[] args) => ...)
// Score: 100 (docker) + 100 (build) + 1 (catch-all) = 201
```

This allows progressive interception - start with catch-all, add specific repeated option handling as needed.

### Option Modifiers

| Syntax | Meaning | Example | Valid | Invalid |
|--------|---------|---------|-------|---------|
| `--flag` | Boolean flag (always optional) | `build --verbose` | `build`<br>`build --verbose` | - |
| `--flag {value}` | Required flag with required value | `build --config {mode}` | `build --config debug` | `build`<br>`build --config` |
| `--flag {value?}` | Required flag with optional value | `build --config {mode?}` | `build --config debug`<br>`build --config` | `build` |
| `--flag? {value}` | Optional flag with required value (if present) | `build --config? {mode}` | `build`<br>`build --config debug` | `build --config` |
| `--flag? {value?}` | Optional flag with optional value | `build --config? {mode?}` | `build`<br>`build --config`<br>`build --config debug` | - |

## Route Specificity Rules

Routes are matched by specificity score (higher wins):

1. **Literal segments** = 100 points each
2. **Required options** (`--flag`) = 50 points each
3. **Optional options** (`--flag?`) = 25 points each
4. **Typed parameters** = 20 points each
5. **Untyped parameters** = 10 points each
6. **Optional parameters** (`{param?}`) = 5 points each
7. **Catch-all** (`{*args}`) = 1 point

Note: Optional flags score lower than required flags, allowing required flag routes to take precedence for more specific matching.

## Progressive Interception Patterns

### Example 1: Git Commit Interception

```csharp
// Specificity: 250 (2 literals + 2 options + 1 param)
.AddRoute("git commit --message {msg} --amend", (string msg, bool amend) => {
    // HIGHEST: Intercept amend with message
    ValidateCommitMessage(msg);
    GitService.CommitAmend(msg);
})

// Specificity: 200 (2 literals + 1 option + 1 param)
.AddRoute("git commit --message {msg}", (string msg) => {
    // HIGH: Intercept standard commit with message
    ValidateCommitMessage(msg);
    Shell.Run("git", "commit", "-m", msg);
})

// Specificity: 200 (2 literals + 2 options)
.AddRoute("git commit --amend --no-edit", (bool amend, bool noEdit) => {
    // HIGH: Intercept quick amend
    GitService.QuickAmend();
})

// Specificity: 150 (2 literals + 1 option)
.AddRoute("git commit --amend", (bool amend) => {
    // MEDIUM: Intercept amend, open editor
    OpenCommitEditor(amend: true);
})

// Specificity: 200 (2 literals)
.AddRoute("git commit", () => {
    // MEDIUM: Intercept basic commit
    OpenCommitEditor(amend: false);
})

// Specificity: 101 (1 literal + 1 catch-all)
.AddRoute("git {*args}", (string[] args) => {
    // LOW: Pass through other git commands
    Shell.Run("git", args);
})

// Specificity: 1 (1 catch-all)
.AddRoute("{*args}", (string[] args) => {
    // LOWEST: Pass through everything else
    Shell.Run(args[0], args[1..]);
})
```

### Example 2: Deploy Command Evolution

```csharp
// Stage 1: Basic passthrough with logging
.AddRoute("deploy {env}", (string env) => {
    Log($"Deploy to {env}");
    Shell.Run("deploy", env);
})

// Stage 2: Add validation for production
.AddRoute("deploy production --force", (bool force) => {
    if (!force) return Error("Production requires --force");
    Shell.Run("deploy", "production", "--force");
})

// Stage 3: Intercept dry-run for all environments
.AddRoute("deploy {env} --dry-run", (string env, bool dryRun) => {
    SimulateDeploy(env);  // Never calls shell
})

// Stage 4: Full interception for config-based deploys
.AddRoute("deploy {env} --config {cfg} --version? {ver?}",
    (string env, string cfg, string? ver) => {
    // --config is required (no ? on flag)
    // --version is optional (? on flag)
    var config = LoadConfig(cfg);
    var version = ver ?? config.DefaultVersion;
    DeployService.Execute(env, config, version);
})

// Fallback for unmatched patterns
.AddRoute("deploy {env} {*flags}", (string env, string[] flags) => {
    Shell.Run("deploy", env, flags);
})
```

## Specificity Resolution Examples

### Scenario A: `git commit --message "hello" --amend`

Matching routes by specificity:
1. ✅ **250 pts**: `git commit --message {msg} --amend` - WINS
2. ❌ **200 pts**: `git commit --message {msg}` - would match but lower
3. ❌ **150 pts**: `git commit --amend` - missing --message
4. ❌ **200 pts**: `git commit` - missing options
5. ❌ **101 pts**: `git {*args}` - would match but much lower

### Scenario B: `git commit --amend`

Matching routes by specificity:
1. ❌ **250 pts**: Requires --message, doesn't match
2. ❌ **200 pts**: Requires --message, doesn't match
3. ❌ **200 pts**: Requires --no-edit, doesn't match
4. ✅ **150 pts**: `git commit --amend` - WINS
5. ❌ **200 pts**: `git commit` - missing --amend
6. ❌ **101 pts**: `git {*args}` - would match but lower

### Scenario C: `git status`

Matching routes by specificity:
1. ❌ All specific git commit routes don't match
2. ✅ **101 pts**: `git {*args}` - WINS
3. ❌ **1 pt**: `{*args}` - would match but lower

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

## Route Registration Order

When routes have **equal specificity**, first registered wins:

```csharp
// These have equal specificity (200 points each)
.AddRoute("process --mode {mode}", (string mode) => HandleA(mode))  // Wins
.AddRoute("process --mode {type}", (string type) => HandleB(type))  // Never reached

// Better: Use different patterns or combine
.AddRoute("process --mode debug", () => HandleDebug())     // Specific (250 pts)
.AddRoute("process --mode {mode}", (string mode) => HandleGeneric(mode))  // General (200 pts)
```

## Boolean Flag Specificity

Boolean flags are scored but always optional:

```csharp
// More specific: Multiple flags
.AddRoute("test --verbose --coverage --watch", (bool v, bool c, bool w) => {
    // Specificity: 150 (3 flags × 50)
    TestRunner.RunFull(v, c, w);
})

// Less specific: Single flag
.AddRoute("test --verbose", (bool verbose) => {
    // Specificity: 50 (1 flag × 50)
    TestRunner.RunBasic(verbose);
})

// Least specific: No flags
.AddRoute("test", () => {
    // Specificity: 0
    TestRunner.RunDefault();
})

// Input: "test --verbose --coverage"
// Matches #1 with verbose=true, coverage=true, watch=false
```

## Catch-All and Wildcard Patterns

```csharp
// Specific command interception
.AddRoute("docker run {image} --detach", (string image, bool detach) => {
    // Intercept detached runs
    DockerService.RunDetached(image);
})

// General docker command with args
.AddRoute("docker {cmd} {*args}", (string cmd, string[] args) => {
    // Log all docker commands
    LogDockerCommand(cmd, args);
    Shell.Run("docker", cmd, args);
})

// Universal fallback
.AddRoute("{cmd} {*args}", (string cmd, string[] args) => {
    // Everything else goes to shell
    Shell.Run(cmd, args);
})
```

## Key Design Principles

1. **Specificity Wins**: More specific routes always match before general ones
2. **Nullable = Optional**: Use nullable parameters to make options optional
3. **Progressive Enhancement**: Add specific intercepts without breaking general patterns
4. **Fallback Safety**: Always have a catch-all for unhandled cases
5. **Order Matters**: When specificity is equal, first registered wins

## Common Patterns

### Gradual Feature Addition
```csharp
// Start: Simple passthrough
.AddRoute("feature {*args}", (args) => Shell.Run("feature", args))

// Add: Validate dangerous operations
.AddRoute("feature delete {id} --force", (string id, bool force) => {
    if (!force) return Error("Requires --force");
    Shell.Run("feature", "delete", id, "--force");
})

// Add: New native functionality
.AddRoute("feature list --format {fmt?}", (string? fmt) => {
    var format = fmt ?? "table";
    return FeatureService.List(format);  // Native implementation
})
```

### Environment-Specific Handling
```csharp
// Production gets special treatment (most specific)
.AddRoute("deploy production --config {cfg}", (string cfg) =>
    ProductionDeploy(cfg))

// Staging has relaxed rules (medium specific)
.AddRoute("deploy staging {*opts}", (string[] opts) =>
    StagingDeploy(opts))

// Other environments (least specific)
.AddRoute("deploy {env} {*opts}", (string env, string[] opts) =>
    StandardDeploy(env, opts))
```