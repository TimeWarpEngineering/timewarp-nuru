#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru.Parsing;
using static System.Console;

WriteLine("Testing for hanging patterns...");
WriteLine("Each pattern has a 5-second timeout");
WriteLine();

// Test the pattern that seemed to hang
TestWithTimeout("build config}", "Pattern with closing brace but no opening");

// Test other potentially problematic patterns
TestWithTimeout("test {", "Pattern with opening brace but no closing");
TestWithTimeout("deploy }", "Pattern with only closing brace");
TestWithTimeout("{{test}}", "Pattern with double braces");
TestWithTimeout("{}", "Empty parameter");
TestWithTimeout("test {a{b}}", "Nested braces");
TestWithTimeout("build --config {", "Option with unclosed parameter");
TestWithTimeout("test {param", "Parameter without closing at end");

WriteLine("\nAll tests completed!");

static void TestWithTimeout(string pattern, string description)
{
    WriteLine($"\nTesting: '{pattern}' - {description}");
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