# Specificity Algorithm

How the Resolver scores and matches routes to determine which handler should execute for a given input.

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

## Specificity with Repeated Options

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

## Related Documents

See [syntax-rules.md](../parser/syntax-rules.md) for route pattern syntax and validation rules.
See [parameter-optionality.md](../cross-cutting/parameter-optionality.md) for nullability-based optionality design.
