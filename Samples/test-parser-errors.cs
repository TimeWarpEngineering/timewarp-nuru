#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing new route pattern parser:");
WriteLine();

// Test valid patterns
TestPattern("status");
TestPattern("git commit");
TestPattern("deploy {env}");
TestPattern("deploy {env} {tag?}");
TestPattern("delay {ms:int}");
TestPattern("docker {*args}");
TestPattern("build --verbose");
TestPattern("build --config {mode}");
// Test the hanging pattern
WriteLine("Testing pattern with description and option aliases...");
TestPattern("deploy {env|Environment} --dry-run,-d|Preview");
WriteLine("Done with complex pattern.");

WriteLine();
WriteLine("Testing invalid patterns:");
WriteLine();

// Test invalid patterns - the main one we're fixing
TestPattern("prompt <input>");
TestPattern("deploy {env");
TestPattern("build --config {");

void TestPattern(string pattern)
{
    try
    {
        var route = RoutePatternParser.Parse(pattern);
        WriteLine($"✓ '{pattern}' parsed successfully");
    }
    catch (ArgumentException ex)
    {
        WriteLine($"✗ '{pattern}' failed:");
        var lines = ex.Message.Split('\n');
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                WriteLine($"  {line}");
        }
    }
}