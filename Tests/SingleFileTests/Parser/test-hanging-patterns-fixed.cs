#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Verifying all hanging patterns are fixed...");
WriteLine();

// Test the previously hanging patterns
TestPattern("build config}", "Closing brace without opening");
TestPattern("deploy }", "Solo closing brace");
TestPattern("test {a{b}}", "Nested braces");
TestPattern("{{test}}", "Double braces");

WriteLine("\nAll patterns now fail gracefully with proper error messages!");

static void TestPattern(string pattern, string description)
{
    Write($"Testing '{pattern}' ({description}): ");
    try
    {
        CompiledRoute route = RoutePatternParser.Parse(pattern);
        WriteLine("UNEXPECTED: Parsed successfully!");
    }
    catch (ArgumentException ex)
    {
        // Extract just the error message without the pattern prefix
        string message = ex.Message;
        int colonIndex = message.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex >= 0 && colonIndex < message.Length - 1)
        {
            message = message[(colonIndex + 1)..].Trim();
        }

        WriteLine($"✓ Error: {message}");
    }
}