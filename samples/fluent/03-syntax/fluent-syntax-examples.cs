#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ============================================================================
// TIMEWARP.NURU - FLUENT DSL SYNTAX EXAMPLES
// ============================================================================
// This file provides Fluent DSL syntax examples for the MCP Server.
//
// The Fluent DSL uses builder.Map().WithHandler().AsCommand().Done()
// Benefits: Minimal API style, good for simple cases and quick prototyping.
// Use this for: Simple scripts, prototyping, migration from minimal APIs.
//
// For Endpoint DSL examples, see: samples/endpoints/03-syntax/
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

var builder = NuruApp.CreateBuilder();

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

Console.WriteLine("✅ TimeWarp.Nuru Fluent DSL Syntax Examples - All patterns compiled successfully!");
Console.WriteLine();
Console.WriteLine("This file validates that all Fluent DSL syntax examples used in MCP documentation");
Console.WriteLine("are correct and compile successfully.");
Console.WriteLine();
Console.WriteLine("For Endpoint DSL examples, see: samples/endpoints/03-syntax/");
Console.WriteLine();
Console.WriteLine("Run with '--help' to see auto-generated help from descriptions:");
Console.WriteLine("  ./fluent-syntax-examples.cs --help");

await app.RunAsync(args);
