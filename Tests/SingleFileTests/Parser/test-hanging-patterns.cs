#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

// Enable debug output to see where parser hangs
Environment.SetEnvironmentVariable("NURU_DEBUG", "true");

WriteLine("Testing for hanging patterns with NURU_DEBUG=true...");
WriteLine("Each pattern has a 5-second timeout");
WriteLine();

// Test each pattern individually to see debug output clearly
WriteLine("=== Testing patterns that HANG ===");
TestWithTimeout("build config}", "Pattern with closing brace but no opening");
TestWithTimeout("deploy }", "Pattern with only closing brace");
TestWithTimeout("test {a{b}}", "Nested braces");

WriteLine("\n=== Testing patterns that work correctly ===");
TestWithTimeout("test {", "Pattern with opening brace but no closing");
TestWithTimeout("{{test}}", "Pattern with double braces");
TestWithTimeout("{}", "Empty parameter");
TestWithTimeout("build --config {", "Option with unclosed parameter");
TestWithTimeout("test {param", "Parameter without closing at end");

WriteLine("\nAll tests completed!");

static void TestWithTimeout(string pattern, string description)
{
    WriteLine($"\n{new string('=', 60)}");
    WriteLine($"Testing: '{pattern}' - {description}");
    WriteLine($"{new string('=', 60)}");
    Write("  Result: ");

    using System.Threading.CancellationTokenSource cts = new();
    System.Threading.Tasks.Task<string> task = System.Threading.Tasks.Task.Run(() =>
    {
        try
        {
            CompiledRoute route = RoutePatternParser.Parse(pattern);
            return "UNEXPECTED: Parsed successfully!";
        }
        catch (ArgumentException ex)
        {
            string firstLine = ex.Message.Split('\n')[0];
            return $"Error: {firstLine}";
        }
    }, cts.Token);

    if (task.Wait(TimeSpan.FromSeconds(5)))
    {
        WriteLine(task.Result);
    }
    else
    {
        cts.Cancel();
        WriteLine("HANGS! (Timed out after 5 seconds)");
    }
}