#!/usr/bin/dotnet --

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
var lexer = new Lexer(routePattern);
IReadOnlyList<Token> tokens = lexer.Tokenize();
WriteLine($"Tokens ({tokens.Count}):");
foreach (Token token in tokens)
{
  WriteLine($"  [{token.Type,-12}] '{token.Value}' at position {token.Position}");
}

WriteLine();

// Step 2: Test the Parser
WriteLine("Step 2: Parser Analysis");
WriteLine("-----------------------");
var parser = new Parser();
ParseResult<Syntax> parseResult = parser.Parse(routePattern);

if (!parseResult.Success)
{
  WriteLine("❌ Parse failed:");
  if (parseResult.ParseErrors is not null)
  {
    foreach (ParseError error in parseResult.ParseErrors)
    {
      WriteLine($" error at position {error.Position}");
    }
  }

  return 1;
}

WriteLine("✓ Parse succeeded");
Syntax? ast = parseResult.Value!;
WriteLine($"AST has {ast.Segments.Count} segments:");

int segmentIndex = 0;
foreach (SegmentSyntax segment in ast.Segments)
{
  Write($"  [{segmentIndex++}] ");
  switch (segment)
  {
    case LiteralSyntax lit:
      WriteLine($"Literal: '{lit.Value}'");
      break;

    case ParameterSyntax param:
      WriteLine($"Parameter: name='{param.Name}', type='{param.Type}', optional={param.IsOptional}");
      break;

    case OptionSyntax opt:
      WriteLine($"Option: longName='{opt.LongForm}', shortName='{opt.ShortForm}', hasParam={opt.Parameter is not null}");
      if (opt.Parameter is not null)
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
  CompiledRoute parsed = PatternParser.Parse(routePattern);

  WriteLine($"Positional segments ({parsed.PositionalMatchers.Count}):");
  foreach (RouteMatcher seg in parsed.PositionalMatchers)
  {
    WriteLine($"  {seg.GetType().Name}: '{seg.ToDisplayString()}'");
  }

  WriteLine($"\nOption segments ({parsed.OptionMatchers.Count}):");
  foreach (OptionMatcher option in parsed.OptionMatchers)
  {
    WriteLine($"    MatchPattern: '{option.MatchPattern}'");
    WriteLine($"    ExpectsValue: {option.ExpectsValue}");
    WriteLine($"    ParameterName: '{option.ParameterName}'");
    WriteLine($"    AlternateForm: '{option.AlternateForm}'");
  }

  WriteLine($"\nRequired options ({parsed.RequiredOptionPatterns.Count}):");
  foreach (string opt in parsed.RequiredOptionPatterns)
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

NuruApp app = builder.Build();

try
{
  WriteLine($"Running with args: [{string.Join(", ", testArgs.Select(a => $"'{a}'"))}]");
  int result = await app.RunAsync(testArgs);
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
string testAppPath = "kubectl-test-app.cs";
await File.WriteAllTextAsync(testAppPath, @"#!/usr/bin/dotnet --
NuruAppBuilder builder = new();
builder.AddRoute(""kubectl apply -f {file}"", (string file) =>
    Console.WriteLine($""deployment.apps/{file} configured""));

NuruApp app = builder.Build();
return await app.RunAsync(args);
");

// Make it executable
#pragma warning disable CA1849 // Call async methods when in an async method
Process.Start("chmod", ["+x", testAppPath]).WaitForExit();
#pragma warning restore CA1849 // Call async methods when in an async method

// Test with shell
var psi = new ProcessStartInfo
{
  FileName = "/bin/bash",
  Arguments = "-c \"./kubectl-test-app.cs kubectl apply -f deployment.yaml\"",
  RedirectStandardOutput = true,
  RedirectStandardError = true,
  UseShellExecute = false
};

var process = Process.Start(psi);
if (process is not null)
{
  string output = await process.StandardOutput.ReadToEndAsync();
  string error = await process.StandardError.ReadToEndAsync();
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