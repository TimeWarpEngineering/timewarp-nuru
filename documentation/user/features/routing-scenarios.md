# Routing scenarios

Longer route examples moved out of [Routing](routing.md) so that page stays within the line cap. The syntax rules stay there.

## Git-Style Sub-Commands

Build hierarchical command structures:

```csharp
// Repository commands
builder.Map("git init")
  .WithHandler(() => GitInit())
  .AsCommand()
  .Done();
builder.Map("git clone {url}")
  .WithHandler((string url) => GitClone(url))
  .AsCommand()
  .Done();
builder.Map("git status")
  .WithHandler(() => GitStatus())
  .AsCommand()
  .Done();

// Branch commands
builder.Map("git branch")
  .WithHandler(() => ListBranches())
  .AsCommand()
  .Done();
builder.Map("git branch {name}")
  .WithHandler((string name) => CreateBranch(name))
  .AsCommand()
  .Done();
builder.Map("git checkout {branch}")
  .WithHandler((string branch) => Checkout(branch))
  .AsCommand()
  .Done();

// Commit commands
builder.Map("git add {*files}")
  .WithHandler((string[] files) => GitAdd(files))
  .AsCommand()
  .Done();
builder.Map("git commit -m {message}")
  .WithHandler((string message) => GitCommit(message))
  .AsCommand()
  .Done();
builder.Map("git push")
  .WithHandler(() => GitPush())
  .AsCommand()
  .Done();
builder.Map("git push --force")
  .WithHandler(() => GitPushForce())
  .AsCommand()
  .Done();
```

## Docker-Style Options

Complex option combinations:

```csharp
builder.Map("run {image}")
  .WithHandler((string image) => Docker.Run(image))
  .AsCommand()
  .Done();

builder.Map("run {image} --port {port:int}")
  .WithHandler((string image, int port) => Docker.Run(image, port: port))
  .AsCommand()
  .Done();

builder.Map("run {image} --port {port:int} --detach")
  .WithHandler((string image, int port) => Docker.Run(image, port: port, detached: true))
  .AsCommand()
  .Done();

builder.Map("run {image} --env {*vars}")
  .WithHandler((string image, string[] vars) => Docker.Run(image, envVars: vars))
  .AsCommand()
  .Done();
```

## Conditional Routing Based on Options

Different handlers for different option combinations:

```csharp
// Dry run
builder.Map("deploy {app} --env {environment} --dry-run")
  .WithHandler((string app, string environment) => DeployDryRun(app, environment))
  .AsCommand()
  .Done();

// Actual deployment
builder.Map("deploy {app} --env {environment}")
  .WithHandler((string app, string environment) => DeployReal(app, environment))
  .AsCommand()
  .Done();

// Force deployment
builder.Map("deploy {app} --env {environment} --force")
  .WithHandler((string app, string environment) => DeployForce(app, environment))
  .AsCommand()
  .Done();
```
