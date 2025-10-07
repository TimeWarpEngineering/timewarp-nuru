#!/usr/bin/dotnet --
// test-boolean-option.cs - Test boolean option binding

// Enable more logging
Environment.SetEnvironmentVariable("NURU_LOG_MATCHER", "trace");
Environment.SetEnvironmentVariable("NURU_LOG_BINDER", "trace");
Environment.SetEnvironmentVariable("NURU_LOG_PARSER", "debug");

// Test boolean option binding
NuruApp app = new NuruAppBuilder()
    .AddRoute("sync --all", (bool all) => WriteLine($"Sync with --all = {all}"))
    .AddRoute("sync", () => WriteLine("Sync without --all"))
    .Build();

// Test cases
string[][] testCases =
[
    ["sync", "--all"],  // Should match first route with all=true
    ["sync"]            // Should match second route
];

int failed = 0;

foreach (string[] testArgs in testCases)
{
    WriteLine($"Testing: {string.Join(" ", testArgs)}");

    try
    {
        int result = await app.RunAsync(testArgs);
        if (result != 0)
        {
            WriteLine($"  FAILED: Returned {result}");
            failed++;
        }
    }
    catch (Exception ex)
    {
        WriteLine($"  ERROR: {ex.Message}");
        failed++;
    }

    WriteLine();
}

return failed;