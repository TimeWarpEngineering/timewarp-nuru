#!/usr/bin/dotnet --
#:project timewarp-nuru-sample/timewarp-nuru-sample.csproj

// ============================================================================
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

NuruAppBuilder builder = NuruApp.CreateBuilder([]);

#region MCP:literals
// Literal segments are plain text that must match exactly
builder.Map("status").WithHandler(() => Console.WriteLine("OK")).AsQuery().Done();
builder.Map("git commit").WithHandler(() => Console.WriteLine("Committing...")).AsCommand().Done();
builder.Map("version").WithHandler(() => Console.WriteLine("1.0.0")).AsQuery().Done();
#endregion

#region MCP:parameters
// Parameters are defined using curly braces {} and capture values from the command line
builder.Map("greet {name}").WithHandler((string name) => Console.WriteLine($"Hello {name}")).AsCommand().Done();
builder.Map("copy {source} {destination}")
    .WithHandler((string source, string dest) => Console.WriteLine($"Copying {source} to {dest}"))
    .AsCommand()
    .Done();
#endregion

#region MCP:types
// Parameters can have type constraints using a colon : followed by the type
builder.Map("delay {ms:int}")
    .WithHandler((int milliseconds) => Console.WriteLine($"Delaying {milliseconds}ms"))
    .AsCommand().Done();
builder.Map("price {amount:double}")
    .WithHandler((double amount) => Console.WriteLine($"Price: ${amount:F2}"))
    .AsQuery().Done();
builder.Map("enabled {flag:bool}")
    .WithHandler((bool flag) => Console.WriteLine($"Enabled: {flag}"))
    .AsIdempotentCommand().Done();
builder.Map("schedule {date:DateTime}")
    .WithHandler((DateTime date) => Console.WriteLine($"Scheduled for {date}"))
    .AsCommand().Done();

// New built-in types for common CLI scenarios:
builder.Map("fetch {url:Uri}")
    .WithHandler((Uri url) => Console.WriteLine($"Fetching {url}"))
    .AsQuery().Done();
builder.Map("read {file:FileInfo}")
    .WithHandler((FileInfo file) => Console.WriteLine($"Reading {file.FullName}"))
    .AsQuery().Done();
builder.Map("list {dir:DirectoryInfo}")
    .WithHandler((DirectoryInfo dir) => Console.WriteLine($"Listing {dir.FullName}"))
    .AsQuery().Done();
builder.Map("connect {addr:IPAddress}")
    .WithHandler((IPAddress addr) => Console.WriteLine($"Connecting to {addr}"))
    .AsCommand().Done();
builder.Map("report {date:DateOnly}")
    .WithHandler((DateOnly date) => Console.WriteLine($"Report for {date}"))
    .AsQuery().Done();
builder.Map("alarm {time:TimeOnly}")
    .WithHandler((TimeOnly time) => Console.WriteLine($"Alarm set for {time}"))
    .AsCommand().Done();

// Supported types: string, int, long, double, decimal, bool, DateTime, Guid, TimeSpan,
// Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
// You can also register custom types via IRouteTypeConverter
#endregion

#region MCP:optional
// Parameters can be made optional by adding ? after the name
// The handler parameter type must be nullable
builder.Map("deploy {env} {tag?}")
    .WithHandler((string env, string? tag) =>
        Console.WriteLine($"Deploying to {env}" + (tag != null ? $" with tag {tag}" : "")))
    .AsCommand().Done();

// Optional parameters can also have type constraints with nullable types
builder.Map("wait {seconds:int?}")
    .WithHandler((int? seconds) => Console.WriteLine($"Waiting {seconds?.ToString() ?? "default"} seconds"))
    .AsCommand().Done();
builder.Map("backup {source} {destination?}")
    .WithHandler((string source, string? destination) => Console.WriteLine($"Backing up {source}"))
    .AsCommand().Done();
#endregion

#region MCP:catchall
// Use * prefix for catch-all parameters that capture all remaining arguments into an array
builder.Map("docker {*args}")
    .WithHandler((string[] args) => Console.WriteLine($"Docker args: {string.Join(" ", args)}"))
    .AsCommand().Done();
builder.Map("run {script} {*params}")
    .WithHandler((string script, string[] parameters) =>
        Console.WriteLine($"Running {script} with {parameters.Length} parameters"))
    .AsCommand().Done();

// Catch-all parameters must be the last parameter in the route pattern
#endregion

#region MCP:options
// Options start with -- (long form) or - (short form)

// Boolean option (flag)
builder.Map("build --verbose")
    .WithHandler((bool verbose) => Console.WriteLine($"Building (verbose: {verbose})"))
    .AsCommand().Done();

// Option with value
builder.Map("build --config {mode}")
    .WithHandler((string mode) => Console.WriteLine($"Building in {mode} mode"))
    .AsCommand().Done();

// Short form option
builder.Map("build -c {mode}")
    .WithHandler((string mode) => Console.WriteLine($"Building in {mode} mode"))
    .AsCommand().Done();

// Option with alias
builder.Map("build --config,-c {mode}")
    .WithHandler((string mode) => Console.WriteLine($"Building in {mode} mode"))
    .AsCommand().Done();

// Multiple options
builder.Map("deploy {env} --dry-run --force")
    .WithHandler((string env, bool dryRun, bool force) =>
        Console.WriteLine($"Deploy to {env} (dry-run: {dryRun}, force: {force})"))
    .AsCommand().Done();

// Optional options (can be omitted)
builder.Map("build --verbose? --debug?")
    .WithHandler((bool verbose, bool debug) => Console.WriteLine($"Build (verbose: {verbose}, debug: {debug})"))
    .AsCommand().Done();

// Repeated options (collect multiple values into array)
builder.Map("docker --env {var}*")
    .WithHandler((string[] var) => Console.WriteLine($"Environment variables: {string.Join(", ", var)}"))
    .AsCommand().Done();
#endregion

#region MCP:descriptions
// Use | to add descriptions for parameters and options (shown in auto-generated help)
builder.Map("deploy {env|Target environment} --dry-run|Preview changes without applying")
    .WithHandler((string env, bool dryRun) =>
        Console.WriteLine($"Deploying to {env} (dry-run: {dryRun})"))
    .AsCommand().Done();

builder.Map("backup {source|Source directory} {dest?|Destination path}")
    .WithHandler((string source, string? dest) =>
        Console.WriteLine($"Backup {source} to {dest ?? "default location"}"))
    .AsCommand().Done();
#endregion

#region MCP:complex
// Real-world examples combining multiple features

// Git-style command with multiple options and aliases
builder.Map("git commit --message,-m {msg} --amend? --no-verify?")
    .WithHandler((string msg, bool amend, bool noVerify) =>
        Console.WriteLine($"Commit: {msg} (amend: {amend}, no-verify: {noVerify})"))
    .AsCommand().Done();

// Docker-style with repeated options and catch-all
builder.Map("docker run --env {e}* -p {port:int}* {image} {*cmd}")
    .WithHandler((string[] e, int[] port, string image, string[] cmd) =>
        Console.WriteLine($"Running {image} with {e.Length} env vars, {port.Length} ports"))
    .AsCommand().Done();

// Kubectl-style with optional typed parameter and option with value
builder.Map("kubectl get {resource} --namespace,-n {ns?} --output,-o {format?}")
    .WithHandler((string resource, string? ns, string? format) =>
        Console.WriteLine($"Get {resource} in namespace {ns ?? "default"}"))
    .AsQuery().Done();
#endregion

NuruCoreApp app = builder.Build();

Console.WriteLine("✅ TimeWarp.Nuru Syntax Examples - All patterns compiled successfully!");
Console.WriteLine();
Console.WriteLine("This file validates that all syntax examples used in MCP documentation");
Console.WriteLine("are correct and compile successfully.");
Console.WriteLine();
Console.WriteLine("Run with '--help' to see auto-generated help from descriptions:");
Console.WriteLine("  ./SyntaxExamples.cs --help");

return 0;
