#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ============================================================================
// TIMEWARP.NURU - SYNTAX EXAMPLES (ENDPOINT DSL + FLUENT DSL)
// ============================================================================
// This file provides syntax examples for both DSLs used by the MCP Server.
//
// PRIORITY: Endpoint DSL (Recommended for agents - scales better, easier to reason about)
// ALTERNATIVE: Fluent DSL (Minimal API style - good for simple cases)
//
// IMPORTANT: This file is used by the TimeWarp.Nuru MCP Server
// ============================================================================
// The MCP server extracts code snippets from the #region blocks below to
// provide syntax documentation to AI assistants and IDE integrations.
//
// DO NOT:
// - Delete or rename the #region blocks
// - Remove this file without updating Source/TimeWarp.Nuru.Mcp/Tools/GetSyntaxTool.cs
// - Add code that doesn't compile
//
// This file MUST compile successfully - it serves as both documentation
// and validation that all syntax examples are correct and working.
// ============================================================================

using System.Net;
using TimeWarp.Nuru;

// ============================================================================
// ENDPOINT DSL EXAMPLES (RECOMMENDED - Priority 1)
// ============================================================================
// The Endpoint DSL uses classes with [NuruRoute] attributes.
// Benefits: Single responsibility, easier to test, scales better, saves context.
// Use this for: Production apps, complex scenarios, agent-friendly codebases.
// ============================================================================

#region MCP:endpoint-literals
// Literal segments are plain text that must match exactly
// File: endpoints/status-query.cs
[NuruRoute("status")]
public class StatusQuery : INuruQuery
{
  public string Handle() => "OK";
}

// File: endpoints/version-query.cs
[NuruRoute("version")]
public class VersionQuery : INuruQuery
{
  public string Handle() => "1.0.0";
}

// Multi-word literals (hyphenated)
// File: endpoints/git-commit-command.cs
[NuruRoute("git-commit")]  // Call with: app git-commit
public class GitCommitCommand : INuruCommand
{
  public void Handle() => Console.WriteLine("Committing...");
}
#endregion

#region MCP:endpoint-parameters
// Parameters are defined using curly braces {} and capture values from the command line
// File: endpoints/greet-query.cs
[NuruRoute("greet {name}")]
public class GreetQuery : INuruQuery
{
  public string Handle(string name) => $"Hello {name}";
}

// File: endpoints/copy-command.cs
[NuruRoute("copy {source} {destination}")]
public class CopyCommand : INuruCommand
{
  public void Handle(string source, string destination) =>
    Console.WriteLine($"Copying {source} to {destination}");
}
#endregion

#region MCP:endpoint-types
// Parameters can have type constraints using a colon : followed by the type
// File: endpoints/delay-command.cs
[NuruRoute("delay {milliseconds:int}")]
public class DelayCommand : INuruCommand
{
  public void Handle(int milliseconds) =>
    Console.WriteLine($"Delaying {milliseconds}ms");
}

// File: endpoints/price-query.cs
[NuruRoute("price {amount:double}")]
public class PriceQuery : INuruQuery
{
  public string Handle(double amount) => $"Price: ${amount:F2}";
}

// File: endpoints/schedule-command.cs
[NuruRoute("schedule {date:DateTime}")]
public class ScheduleCommand : INuruCommand
{
  public void Handle(DateTime date) =>
    Console.WriteLine($"Scheduled for {date}");
}

// File: endpoints/fetch-query.cs (Uri type)
[NuruRoute("fetch {url:Uri}")]
public class FetchQuery : INuruQuery
{
  public string Handle(Uri url) => $"Fetching {url}";
}

// File: endpoints/read-file-query.cs (FileInfo type)
[NuruRoute("read-file {file:FileInfo}")]
public class ReadFileQuery : INuruQuery
{
  public string Handle(FileInfo file) => $"Reading {file.FullName}";
}

// Supported types: string, int, long, double, decimal, bool, DateTime, Guid, TimeSpan,
// Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
// Custom types via IRouteTypeConverter also supported
#endregion

#region MCP:endpoint-optional
// Parameters can be made optional by adding ? after the name
// The handler parameter type must be nullable
// File: endpoints/deploy-command.cs
[NuruRoute("deploy {environment} {tag?}")]
public class DeployCommand : INuruCommand
{
  public void Handle(string environment, string? tag) =>
    Console.WriteLine($"Deploying to {environment}" + (tag != null ? $" with tag {tag}" : ""));
}

// File: endpoints/wait-command.cs
[NuruRoute("wait {seconds:int?}")]
public class WaitCommand : INuruCommand
{
  public void Handle(int? seconds) =>
    Console.WriteLine($"Waiting {seconds?.ToString() ?? "default"} seconds");
}
#endregion

#region MCP:endpoint-catchall
// Use * prefix for catch-all parameters that capture all remaining arguments
// File: endpoints/docker-command.cs
[NuruRoute("docker {*args}")]
public class DockerCommand : INuruCommand
{
  public void Handle(string[] args) =>
    Console.WriteLine($"Docker args: {string.Join(" ", args)}");
}

// File: endpoints/run-command.cs
[NuruRoute("run {script} {*parameters}")]
public class RunCommand : INuruCommand
{
  public void Handle(string script, string[] parameters) =>
    Console.WriteLine($"Running {script} with {parameters.Length} parameters");
}

// Catch-all parameters must be the last parameter in the route pattern
#endregion

#region MCP:endpoint-options
// Options are parameters that start with -- (long form) or - (short form)
// File: endpoints/build-command.cs
[NuruRoute("build --verbose")]
public class BuildCommand : INuruCommand
{
  public void Handle(bool verbose) =>
    Console.WriteLine($"Building (verbose: {verbose})");
}

// File: endpoints/build-with-config-command.cs
[NuruRoute("build-with-config --config {mode}")]
public class BuildWithConfigCommand : INuruCommand
{
  public void Handle(string mode) =>
    Console.WriteLine($"Building in {mode} mode");
}

// Option with alias
// File: endpoints/build-with-alias-command.cs
[NuruRoute("build-with-alias --config,-c {mode}")]
public class BuildWithAliasCommand : INuruCommand
{
  public void Handle(string mode) =>
    Console.WriteLine($"Building in {mode} mode");
}

// Multiple options
// File: endpoints/deploy-with-options-command.cs
[NuruRoute("deploy-with-options {environment} --dry-run --force")]
public class DeployWithOptionsCommand : INuruCommand
{
  public void Handle(string environment, bool dryRun, bool force) =>
    Console.WriteLine($"Deploy to {environment} (dry-run: {dryRun}, force: {force})");
}

// Optional options (can be omitted)
// File: endpoints/build-with-optional-command.cs
[NuruRoute("build-with-optional --verbose? --debug?")]
public class BuildWithOptionalCommand : INuruCommand
{
  public void Handle(bool verbose, bool debug) =>
    Console.WriteLine($"Build (verbose: {verbose}, debug: {debug})");
}

// Repeated options (collect multiple values into array)
// File: endpoints/docker-with-env-command.cs
[NuruRoute("docker-with-env --env {variables}*")]
public class DockerWithEnvCommand : INuruCommand
{
  public void Handle(string[] variables) =>
    Console.WriteLine($"Environment variables: {string.Join(", ", variables)}");
}
#endregion

#region MCP:endpoint-descriptions
// Use | to add descriptions for parameters and options (shown in auto-generated help)
// File: endpoints/deploy-with-desc-command.cs
[NuruRoute("deploy-with-desc {environment|Target environment} --dry-run|Preview changes without applying")]
public class DeployWithDescCommand : INuruCommand
{
  public void Handle(string environment, bool dryRun) =>
    Console.WriteLine($"Deploying to {environment} (dry-run: {dryRun})");
}

// File: endpoints/backup-command.cs
[NuruRoute("backup {source|Source directory} {destination?|Destination path}")]
public class BackupCommand : INuruCommand
{
  public void Handle(string source, string? destination) =>
    Console.WriteLine($"Backup {source} to {destination ?? "default location"}");
}
#endregion

#region MCP:endpoint-complex
// Real-world examples combining multiple features
// Git-style command with multiple options and aliases
// File: endpoints/git-commit-full-command.cs
[NuruRoute("git-commit-full --message,-m {message} --amend? --no-verify?")]
public class GitCommitFullCommand : INuruCommand
{
  public void Handle(string message, bool amend, bool noVerify) =>
    Console.WriteLine($"Commit: {message} (amend: {amend}, no-verify: {noVerify})");
}

// Docker-style with repeated options and catch-all
// File: endpoints/docker-run-command.cs
[NuruRoute("docker-run --env {env}* -p {ports:int}* {image} {*command}")]
public class DockerRunCommand : INuruCommand
{
  public void Handle(string[] env, int[] ports, string image, string[] command) =>
    Console.WriteLine($"Running {image} with {env.Length} env vars, {ports.Length} ports");
}

// Kubectl-style with optional typed parameter and option with value
// File: endpoints/kubectl-get-query.cs
[NuruRoute("kubectl-get {resource} --namespace,-n {namespace?} --output,-o {format?}")]
public class KubectlGetQuery : INuruQuery
{
  public string Handle(string resource, string? namespace, string? format) =>
    $"Get {resource} in namespace {namespace ?? "default"}";
}
#endregion

// ============================================================================
// FLUENT DSL EXAMPLES (ALTERNATIVE - Priority 2)
// ============================================================================
// The Fluent DSL uses builder.Map().WithHandler().AsCommand().Done()
// Benefits: Minimal API style, good for simple cases and quick prototyping.
// Use this for: Simple scripts, prototyping, migration from minimal APIs.
// ============================================================================

#region MCP:fluent-literals
// Literal segments are plain text that must match exactly
builder.Map("fluent-status").WithHandler(() => Console.WriteLine("OK")).AsQuery().Done();
builder.Map("fluent-version").WithHandler(() => Console.WriteLine("1.0.0")).AsQuery().Done();
// Note: Multi-word in Fluent uses builder pattern directly
#endregion

#region MCP:fluent-parameters
// Parameters are defined using curly braces {} and capture values from the command line
builder.Map("fluent-greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello {name}"))
    .AsCommand()
    .Done();
builder.Map("fluent-copy {source} {destination}")
    .WithHandler((string source, string dest) => Console.WriteLine($"Copying {source} to {dest}"))
    .AsCommand()
    .Done();
#endregion

#region MCP:fluent-types
// Parameters can have type constraints using a colon : followed by the type
builder.Map("fluent-delay {ms:int}")
    .WithHandler((int milliseconds) => Console.WriteLine($"Delaying {milliseconds}ms"))
    .AsCommand().Done();
builder.Map("fluent-price {amount:double}")
    .WithHandler((double amount) => Console.WriteLine($"Price: ${amount:F2}"))
    .AsQuery().Done();
builder.Map("fluent-enabled {flag:bool}")
    .WithHandler((bool flag) => Console.WriteLine($"Enabled: {flag}"))
    .AsIdempotentCommand().Done();
builder.Map("fluent-schedule {date:DateTime}")
    .WithHandler((DateTime date) => Console.WriteLine($"Scheduled for {date}"))
    .AsCommand().Done();
builder.Map("fluent-fetch {url:Uri}")
    .WithHandler((Uri url) => Console.WriteLine($"Fetching {url}"))
    .AsQuery().Done();
builder.Map("fluent-read {file:FileInfo}")
    .WithHandler((FileInfo file) => Console.WriteLine($"Reading {file.FullName}"))
    .AsQuery().Done();
#endregion

#region MCP:fluent-optional
// Parameters can be made optional by adding ? after the name
// The handler parameter type must be nullable
builder.Map("fluent-deploy {env} {tag?}")
    .WithHandler((string env, string? tag) =>
        Console.WriteLine($"Deploying to {env}" + (tag != null ? $" with tag {tag}" : "")))
    .AsCommand().Done();
builder.Map("fluent-wait {seconds:int?}")
    .WithHandler((int? seconds) => Console.WriteLine($"Waiting {seconds?.ToString() ?? "default"} seconds"))
    .AsCommand().Done();
#endregion

#region MCP:fluent-catchall
// Use * prefix for catch-all parameters that capture all remaining arguments
builder.Map("fluent-docker {*args}")
    .WithHandler((string[] args) => Console.WriteLine($"Docker args: {string.Join(" ", args)}"))
    .AsCommand().Done();
builder.Map("fluent-run {script} {*params}")
    .WithHandler((string script, string[] parameters) =>
        Console.WriteLine($"Running {script} with {parameters.Length} parameters"))
    .AsCommand().Done();
#endregion

#region MCP:fluent-options
// Options start with -- (long form) or - (short form)
builder.Map("fluent-build --verbose")
    .WithHandler((bool verbose) => Console.WriteLine($"Building (verbose: {verbose})"))
    .AsCommand().Done();
builder.Map("fluent-build-config --config {mode}")
    .WithHandler((string mode) => Console.WriteLine($"Building in {mode} mode"))
    .AsCommand().Done();
builder.Map("fluent-build-alias --config,-c {mode}")
    .WithHandler((string mode) => Console.WriteLine($"Building in {mode} mode"))
    .AsCommand().Done();
builder.Map("fluent-deploy-options {env} --dry-run --force")
    .WithHandler((string env, bool dryRun, bool force) =>
        Console.WriteLine($"Deploy to {env} (dry-run: {dryRun}, force: {force})"))
    .AsCommand().Done();
builder.Map("fluent-build-optional --verbose? --debug?")
    .WithHandler((bool verbose, bool debug) => Console.WriteLine($"Build (verbose: {verbose}, debug: {debug})"))
    .AsCommand().Done();
builder.Map("fluent-docker-env --env {var}*")
    .WithHandler((string[] var) => Console.WriteLine($"Environment variables: {string.Join(", ", var)}"))
    .AsCommand().Done();
#endregion

#region MCP:fluent-descriptions
// Use | to add descriptions for parameters and options
builder.Map("fluent-deploy-desc {env|Target environment} --dry-run|Preview changes without applying")
    .WithHandler((string env, bool dryRun) =>
        Console.WriteLine($"Deploying to {env} (dry-run: {dryRun})"))
    .AsCommand().Done();
builder.Map("fluent-backup {source|Source directory} {dest?|Destination path}")
    .WithHandler((string source, string? dest) =>
        Console.WriteLine($"Backup {source} to {dest ?? "default location"}"))
    .AsCommand().Done();
#endregion

#region MCP:fluent-complex
// Real-world examples combining multiple features
builder.Map("fluent-git-commit --message,-m {msg} --amend? --no-verify?")
    .WithHandler((string msg, bool amend, bool noVerify) =>
        Console.WriteLine($"Commit: {msg} (amend: {amend}, no-verify: {noVerify})"))
    .AsCommand().Done();
builder.Map("fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}")
    .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        Console.WriteLine($"Running {image} with {e.Length} env vars, {port.Length} ports"))
    .AsCommand().Done();
builder.Map("fluent-kubectl-get {resource} --namespace,-n {ns?} --output,-o {format?}")
    .WithHandler((string resource, string? ns, string? format) =>
        Console.WriteLine($"Get {resource} in namespace {ns ?? "default"}"))
    .AsQuery().Done();
#endregion

// ============================================================================
// BUILD AND RUN
// ============================================================================

NuruApp app = builder.Build();

Console.WriteLine("✅ TimeWarp.Nuru Syntax Examples - All patterns compiled successfully!");
Console.WriteLine();
Console.WriteLine("This file validates that all syntax examples used in MCP documentation");
Console.WriteLine("are correct and compile successfully.");
Console.WriteLine();
Console.WriteLine("DSL PRIORITY:");
Console.WriteLine("  1. Endpoint DSL (RECOMMENDED) - Scalable, single responsibility, agent-friendly");
Console.WriteLine("  2. Fluent DSL (Alternative) - Minimal API style, good for simple cases");
Console.WriteLine();
Console.WriteLine("Run with '--help' to see auto-generated help from descriptions:");
Console.WriteLine("  ./syntax-examples.cs --help");
