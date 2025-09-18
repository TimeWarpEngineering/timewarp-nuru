#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing Route Pattern Analyzer Scenarios");
WriteLine("========================================");
WriteLine();
WriteLine("Each test shows an invalid pattern and its corrected version.");
WriteLine("These correspond to analyzer diagnostics NURU001-NURU009.");
WriteLine();

// NURU001: Invalid parameter syntax (using angle brackets instead of curly braces)
TestPair("NURU001", "Invalid parameter syntax",
    "deploy <env>",
    "deploy {env}",
    "Use curly braces {} for parameters, not angle brackets <>");

TestPair("NURU001", "Multiple invalid parameters",
    "copy <source> <dest>",
    "copy {source} {dest}",
    "All parameters must use curly braces");

// NURU002: Unbalanced braces in route pattern
TestPair("NURU002", "Missing closing brace",
    "deploy {env",
    "deploy {env}",
    "Close all parameter braces");

TestPair("NURU002", "Missing opening brace",
    "build config}",
    "build {config}",
    "Open all parameter braces");

TestPair("NURU002", "Nested unbalanced braces",
    "test {param{nested}",
    "test {param}",
    "Parameters cannot be nested");

// NURU003: Invalid option format
TestPair("NURU003", "Single dash with word",
    "test -verbose",
    "test --verbose",
    "Long options need double dash (--verbose) or use short form (-v)");

TestPair("NURU003", "Missing dashes",
    "build config release",
    "build --config release",
    "Options must start with dashes");

// NURU004: Invalid type constraint
TestPair("NURU004", "Unknown type",
    "get {id:invalid}",
    "get {id:int}",
    "Valid types: string, int, double, bool, DateTime, Guid, long, float, decimal");

TestPair("NURU004", "Misspelled type",
    "wait {seconds:integer}",
    "wait {seconds:int}",
    "Use 'int' not 'integer'");

TestPair("NURU004", "Case sensitive type",
    "parse {date:datetime}",
    "parse {date:DateTime}",
    "Type names are case-sensitive (DateTime not datetime)");

// NURU005: Catch-all parameter not at end of route
TestPair("NURU005", "Catch-all in middle",
    "docker {*args} {command}",
    "docker {command} {*args}",
    "Catch-all parameter must be last");

TestPair("NURU005", "Multiple parameters after catch-all",
    "exec {*args} {env} {user}",
    "exec {env} {user} {*args}",
    "Nothing can come after catch-all parameter");

// NURU006: Duplicate parameter names in route
TestPair("NURU006", "Same parameter twice",
    "copy {file} {file}",
    "copy {source} {destination}",
    "Each parameter must have a unique name");

TestPair("NURU006", "Duplicate with different positions",
    "move {path} to {path}",
    "move {source} to {destination}",
    "Parameter names must be unique across the entire route");

// NURU007: Conflicting optional parameters
TestPair("NURU007", "Multiple optional in sequence",
    "backup {src?} {dst?}",
    "backup {src} {dst?}",
    "Only the last parameter in a sequence can be optional");

TestPair("NURU007", "Optional before required",
    "deploy {env?} {version}",
    "deploy {env} {version?}",
    "Optional parameters must come after required ones");

// NURU008: Mixed catch-all with optional parameters
TestPair("NURU008", "Optional before catch-all",
    "exec {cmd?} {*args}",
    "exec {cmd} {*args}",
    "Cannot mix optional parameters with catch-all");

TestPair("NURU008", "Multiple optional with catch-all",
    "run {script?} {env?} {*args}",
    "run {script} {env} {*args}",
    "Remove optional markers when using catch-all");

// NURU009: Option with same short and long form
TestPair("NURU009", "Duplicate short form",
    "test --verbose,-v,-v",
    "test --verbose,-v",
    "Don't duplicate short option forms");

TestPair("NURU009", "Multiple same aliases",
    "build --output,-o,-o,-o {path}",
    "build --output,-o {path}",
    "Each short form should appear only once");

WriteLine();
WriteLine("Summary: Tested all 9 analyzer diagnostic scenarios");
WriteLine("These patterns help ensure the analyzer catches common mistakes");

static void TestPair(string diagnostic, string scenario, string invalid, string valid, string explanation)
{
    WriteLine($"\n{diagnostic}: {scenario}");
    WriteLine($"  {explanation}");

    Write($"  ✗ Invalid: '{invalid}' - ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(invalid);
        WriteLine("UNEXPECTED: Pattern parsed successfully!");
    }
    catch (ArgumentException ex)
    {
        string firstLine = ex.Message.Split('\n')[0];
        WriteLine($"Error: {firstLine}");
    }

    Write($"  ✓ Valid:   '{valid}' - ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(valid);
        WriteLine("Parses correctly");
    }
    catch (ArgumentException ex)
    {
        WriteLine($"UNEXPECTED ERROR: {ex.Message.Split('\n')[0]}");
    }
}