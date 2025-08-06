#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Parsing;
using TimeWarp.Nuru.Parsing.Ast;
using static System.Console;
using System.Diagnostics;

WriteLine("Testing kubectl apply -f deployment.yaml");
WriteLine("==========================================");
WriteLine();

// First, let's define the route pattern and test command
string routePattern = "kubectl apply -f {file}";
string[] testArgs = ["kubectl", "apply", "-f", "deployment.yaml"];

WriteLine($"Route pattern: '{routePattern}'");
WriteLine($"Test args: [{string.Join(", ", testArgs.Select(a => $"'{a}'"))}]");
WriteLine();

// Step 1: Test the Lexer
WriteLine("Step 1: Lexer Analysis");
WriteLine("----------------------");
var lexer = new RoutePatternLexer(routePattern);
var tokens = lexer.Tokenize();
WriteLine($"Tokens ({tokens.Count}):");
foreach (var token in tokens)
{
    WriteLine($"  [{token.Type,-12}] '{token.Value}' at position {token.Position}");
}
WriteLine();

// Step 2: Test the Parser
WriteLine("Step 2: Parser Analysis");
WriteLine("-----------------------");
var parser = new NewRoutePatternParser();
var parseResult = parser.Parse(routePattern);

if (!parseResult.Success)
{
    WriteLine("❌ Parse failed:");
    foreach (var error in parseResult.Errors)
    {
        WriteLine($"  {error.Message} at position {error.Position}");
    }
    return 1;
}

WriteLine("✓ Parse succeeded");
var ast = parseResult.Value!;
WriteLine($"AST has {ast.Segments.Count} segments:");

int segmentIndex = 0;
foreach (var segment in ast.Segments)
{
    Write($"  [{segmentIndex++}] ");
    switch (segment)
    {
        case LiteralNode lit:
            WriteLine($"Literal: '{lit.Value}'");
            break;
            
        case ParameterNode param:
            WriteLine($"Parameter: name='{param.Name}', type='{param.Type}', optional={param.IsOptional}");
            break;
            
        case OptionNode opt:
            WriteLine($"Option: longName='{opt.LongName}', shortName='{opt.ShortName}', hasParam={opt.Parameter != null}");
            if (opt.Parameter != null)
            {
                WriteLine($"      Option parameter: name='{opt.Parameter.Name}', type='{opt.Parameter.Type}'");
            }
            break;
            
        default:
            WriteLine($"Unknown segment type: {segment.GetType().Name}");
            break;
    }
}
WriteLine();

// Step 3: Test ParsedRoute conversion
WriteLine("Step 3: ParsedRoute Analysis");
WriteLine("----------------------------");
try
{
    var parsed = RoutePatternParser.Parse(routePattern);
    
    WriteLine($"Positional segments ({parsed.PositionalTemplate.Count}):");
    foreach (var seg in parsed.PositionalTemplate)
    {
        WriteLine($"  {seg.GetType().Name}: '{seg.ToDisplayString()}'");
    }
    
    WriteLine($"\nOption segments ({parsed.OptionSegments.Count}):");
    foreach (var opt in parsed.OptionSegments)
    {
        WriteLine($"  Name: '{opt.Name}'");
        WriteLine($"    ExpectsValue: {opt.ExpectsValue}");
        WriteLine($"    ValueParameterName: '{opt.ValueParameterName}'");
        WriteLine($"    ShortAlias: '{opt.ShortAlias}'");
    }
    
    WriteLine($"\nRequired options ({parsed.RequiredOptions.Count}):");
    foreach (var opt in parsed.RequiredOptions)
    {
        WriteLine($"  '{opt}'");
    }
}
catch (Exception ex)
{
    WriteLine($"❌ Error: {ex.Message}");
    return 1;
}

// Step 4: Test actual route matching
WriteLine("\nStep 4: Route Matching Test");
WriteLine("---------------------------");
var builder = new NuruAppBuilder();
builder.AddRoute(routePattern, (string file) =>
    WriteLine($"✓ deployment.apps/{file} configured"));

var app = builder.Build();

try
{
    WriteLine($"Running with args: [{string.Join(", ", testArgs.Select(a => $"'{a}'"))}]");
    var result = await app.RunAsync(testArgs);
    WriteLine($"Result code: {result}");
}
catch (Exception ex)
{
    WriteLine($"❌ ERROR: {ex.Message}");
    WriteLine($"Stack: {ex.StackTrace}");
}

// Step 5: Test using actual shell execution
WriteLine("\nStep 5: Shell Execution Test");
WriteLine("----------------------------");
WriteLine("Building test app...");

// Create a simple test app
var testAppPath = "kubectl-test-app.cs";
await File.WriteAllTextAsync(testAppPath, @"#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;

var builder = new NuruAppBuilder();
builder.AddRoute(""kubectl apply -f {file}"", (string file) =>
    Console.WriteLine($""deployment.apps/{file} configured""));

var app = builder.Build();
return await app.RunAsync(args);
");

// Make it executable
Process.Start("chmod", ["+x", testAppPath]).WaitForExit();

// Test with shell
var psi = new ProcessStartInfo
{
    FileName = "/bin/bash",
    Arguments = $"-c \"./kubectl-test-app.cs kubectl apply -f deployment.yaml\"",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
};

var process = Process.Start(psi);
if (process != null)
{
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    
    WriteLine($"Exit code: {process.ExitCode}");
    if (!string.IsNullOrEmpty(output))
        WriteLine($"Output: {output.Trim()}");
    if (!string.IsNullOrEmpty(error))
        WriteLine($"Error: {error.Trim()}");
}

// Cleanup
File.Delete(testAppPath);

return 0;