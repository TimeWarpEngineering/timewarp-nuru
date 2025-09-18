#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

#pragma warning disable CA1031 // Do not catch general exception types - OK for tests

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing lexer with hanging pattern:");

string pattern = "deploy {env|Environment} --dry-run,-d|Preview";
WriteLine($"Pattern: {pattern}");

// Test simpler patterns first
TestLexer("deploy {env|Environment}");
TestLexer("--dry-run,-d|Preview");
TestLexer("deploy {env|Environment} --dry-run,-d|Preview");

void TestLexer(string testPattern)
{
    try
    {
        WriteLine($"\nTesting: {testPattern}");
        var lexer = new RoutePatternLexer(testPattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();
        WriteLine($"Found {tokens.Count} tokens:");
        foreach (Token token in tokens)
        {
            WriteLine($"  {token.Type}: '{token.Value}' at position {token.Position}");
        }
    }
    catch (Exception ex)
    {
        WriteLine($"Error: {ex.Message}");
    }
}