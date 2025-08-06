#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru.Parsing;
using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing parser with single failing pattern:");
WriteLine();

// Test pattern that's failing: git commit --amend --no-edit
string pattern = "git commit --amend --no-edit";
WriteLine($"Pattern: '{pattern}'");
WriteLine();

// First, verify lexer output
WriteLine("Step 1: Lexer output");
var lexer = new RoutePatternLexer(pattern);
var tokens = lexer.Tokenize();
WriteLine($"Tokens ({tokens.Count}):");
foreach (var token in tokens)
{
    WriteLine($"  [{token.Type,-12}] '{token.Value}' at position {token.Position}");
}
WriteLine();

// Now test the parser
WriteLine("Step 2: Parser output");
var parser = new NewRoutePatternParser();
var parseResult = parser.Parse(pattern);

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
            WriteLine($"Option: longName='{opt.LongName}', shortName='{opt.ShortName}', desc='{opt.Description}', hasParam={opt.Parameter != null}");
            break;
            
        default:
            WriteLine($"Unknown segment type: {segment.GetType().Name}");
            break;
    }
}
WriteLine();

// Now test the full parsing to ParsedRoute
WriteLine("Step 3: ParsedRoute conversion");
try
{
    var parsed = RoutePatternParser.Parse(pattern);
    
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
        WriteLine($"    Description: '{opt.Description}'");
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
    WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}

WriteLine("\n" + new string('=', 50));
WriteLine("EXPECTED vs ACTUAL:");
WriteLine(new string('=', 50));
WriteLine("Expected AST segments:");
WriteLine("  [0] Literal: 'git'");
WriteLine("  [1] Literal: 'commit'");
WriteLine("  [2] Option: longName='amend'");
WriteLine("  [3] Option: longName='no-edit'");
WriteLine("\nExpected ParsedRoute:");
WriteLine("  2 positional segments (git, commit)");
WriteLine("  2 option segments (--amend, --no-edit)");
WriteLine("  2 required options (--amend, --no-edit)");

return 0;