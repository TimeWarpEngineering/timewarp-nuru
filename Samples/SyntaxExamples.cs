#!/usr/bin/dotnet --
#:project TimeWarp.Nuru.Sample/TimeWarp.Nuru.Sample.csproj

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

using TimeWarp.Nuru;

NuruAppBuilder builder = new();

#region MCP:literals
// Literal segments are plain text that must match exactly
builder.AddRoute("status", () => Console.WriteLine("OK"));
builder.AddRoute("git commit", () => Console.WriteLine("Committing..."));
builder.AddRoute("version", () => Console.WriteLine("1.0.0"));
#endregion

#region MCP:parameters
// Parameters are defined using curly braces {} and capture values from the command line
builder.AddRoute("greet {name}", (string name) => Console.WriteLine($"Hello {name}"));
builder.AddRoute("copy {source} {destination}", (string source, string dest) =>
    Console.WriteLine($"Copying {source} to {dest}"));
#endregion

#region MCP:types
// Parameters can have type constraints using a colon : followed by the type
builder.AddRoute("delay {ms:int}", (int milliseconds) =>
    Console.WriteLine($"Delaying {milliseconds}ms"));
builder.AddRoute("price {amount:double}", (double amount) =>
    Console.WriteLine($"Price: ${amount:F2}"));
builder.AddRoute("enabled {flag:bool}", (bool flag) =>
    Console.WriteLine($"Enabled: {flag}"));
builder.AddRoute("schedule {date:DateTime}", (DateTime date) =>
    Console.WriteLine($"Scheduled for {date}"));

// New built-in types for common CLI scenarios:
builder.AddRoute("fetch {url:Uri}", (Uri url) =>
    Console.WriteLine($"Fetching {url}"));
builder.AddRoute("read {file:FileInfo}", (FileInfo file) =>
    Console.WriteLine($"Reading {file.FullName}"));
builder.AddRoute("list {dir:DirectoryInfo}", (DirectoryInfo dir) =>
    Console.WriteLine($"Listing {dir.FullName}"));
builder.AddRoute("connect {addr:IPAddress}", (IPAddress addr) =>
    Console.WriteLine($"Connecting to {addr}"));
builder.AddRoute("report {date:DateOnly}", (DateOnly date) =>
    Console.WriteLine($"Report for {date}"));
builder.AddRoute("alarm {time:TimeOnly}", (TimeOnly time) =>
    Console.WriteLine($"Alarm set for {time}"));

// Supported types: string, int, long, double, decimal, bool, DateTime, Guid, TimeSpan,
// Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
// You can also register custom types via IRouteTypeConverter
#endregion

#region MCP:optional
// Parameters can be made optional by adding ? after the name
// The handler parameter type must be nullable
builder.AddRoute("deploy {env} {tag?}", (string env, string? tag) =>
    Console.WriteLine($"Deploying to {env}" + (tag != null ? $" with tag {tag}" : "")));

// Optional parameters can also have type constraints with nullable types
builder.AddRoute("wait {seconds:int?}", (int? seconds) =>
    Console.WriteLine($"Waiting {seconds?.ToString() ?? "default"} seconds"));
builder.AddRoute("backup {source} {destination?}", (string source, string? destination) =>
    Console.WriteLine($"Backing up {source}"));
#endregion

#region MCP:catchall
// Use * prefix for catch-all parameters that capture all remaining arguments into an array
builder.AddRoute("docker {*args}", (string[] args) =>
    Console.WriteLine($"Docker args: {string.Join(" ", args)}"));
builder.AddRoute("run {script} {*params}", (string script, string[] parameters) =>
    Console.WriteLine($"Running {script} with {parameters.Length} parameters"));

// Catch-all parameters must be the last parameter in the route pattern
#endregion

#region MCP:options
// Options start with -- (long form) or - (short form)

// Boolean option (flag)
builder.AddRoute("build --verbose", (bool verbose) =>
    Console.WriteLine($"Building (verbose: {verbose})"));

// Option with value
builder.AddRoute("build --config {mode}", (string mode) =>
    Console.WriteLine($"Building in {mode} mode"));

// Short form option
builder.AddRoute("build -c {mode}", (string mode) =>
    Console.WriteLine($"Building in {mode} mode"));

// Option with alias
builder.AddRoute("build --config,-c {mode}", (string mode) =>
    Console.WriteLine($"Building in {mode} mode"));

// Multiple options
builder.AddRoute("deploy {env} --dry-run --force", (string env, bool dryRun, bool force) =>
    Console.WriteLine($"Deploy to {env} (dry-run: {dryRun}, force: {force})"));

// Optional options (can be omitted)
builder.AddRoute("build --verbose? --debug?", (bool verbose, bool debug) =>
    Console.WriteLine($"Build (verbose: {verbose}, debug: {debug})"));

// Repeated options (collect multiple values into array)
builder.AddRoute("docker --env {var}*", (string[] var) =>
    Console.WriteLine($"Environment variables: {string.Join(", ", var)}"));
#endregion

#region MCP:descriptions
// Use | to add descriptions for parameters and options (shown in auto-generated help)
builder.AddRoute(
    "deploy {env|Target environment} --dry-run|Preview changes without applying",
    (string env, bool dryRun) =>
        Console.WriteLine($"Deploying to {env} (dry-run: {dryRun})")
);

builder.AddRoute(
    "backup {source|Source directory} {dest?|Destination path}",
    (string source, string? dest) =>
        Console.WriteLine($"Backup {source} to {dest ?? "default location"}")
);
#endregion

#region MCP:complex
// Real-world examples combining multiple features

// Git-style command with multiple options and aliases
builder.AddRoute(
    "git commit --message,-m {msg} --amend? --no-verify?",
    (string msg, bool amend, bool noVerify) =>
        Console.WriteLine($"Commit: {msg} (amend: {amend}, no-verify: {noVerify})")
);

// Docker-style with repeated options and catch-all
builder.AddRoute(
    "docker run --env {e}* -p {port:int}* {image} {*cmd}",
    (string[] e, int[] port, string image, string[] cmd) =>
        Console.WriteLine($"Running {image} with {e.Length} env vars, {port.Length} ports")
);

// Kubectl-style with optional typed parameter and option with value
builder.AddRoute(
    "kubectl get {resource} --namespace,-n {ns?} --output,-o {format?}",
    (string resource, string? ns, string? format) =>
        Console.WriteLine($"Get {resource} in namespace {ns ?? "default"}")
);
#endregion

NuruApp app = builder.Build();

Console.WriteLine("✅ TimeWarp.Nuru Syntax Examples - All patterns compiled successfully!");
Console.WriteLine();
Console.WriteLine("This file validates that all syntax examples used in MCP documentation");
Console.WriteLine("are correct and compile successfully.");
Console.WriteLine();
Console.WriteLine("Run with '--help' to see auto-generated help from descriptions:");
Console.WriteLine("  ./SyntaxExamples.cs --help");

return 0;
