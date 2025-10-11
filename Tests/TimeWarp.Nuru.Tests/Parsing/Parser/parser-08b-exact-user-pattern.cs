#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// Test user's EXACT pattern that fails
using TimeWarp.Nuru.Parsing;

Console.WriteLine("Testing user's EXACT failing pattern");
Console.WriteLine("=====================================\n");

string pattern =
  "{input|Text to generate avatar from (email, username, etc)} " +
  "--output,-o? {file?|Save SVG to file instead of stdout} " +
  "--no-env|Generate without environment circle " +
  "--output-hash|Display hash calculation details instead of SVG";

Console.WriteLine("Pattern:");
Console.WriteLine(pattern);
Console.WriteLine();

try
{
    CompiledRoute route = PatternParser.Parse(pattern);
    Console.WriteLine("✓ SUCCESS: Pattern parsed without error!");
    Console.WriteLine("  Options count: " + route.OptionMatchers.Count);
}
catch (Exception ex)
{
    Console.WriteLine("✗ FAILED: " + ex.Message);
    Console.WriteLine("  Type: " + ex.GetType().Name);
}
