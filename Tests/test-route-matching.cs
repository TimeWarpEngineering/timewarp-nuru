#!/usr/bin/dotnet --
#:project ../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using static System.Console;

WriteLine("Testing route matching:");
WriteLine();

var builder = new NuruAppBuilder();

// Add the problematic route
builder.AddRoute("git commit --amend --no-edit", () => WriteLine("✓ Amending without editing message"));

var app = builder.Build();

// Test the exact command
string[] testArgs = ["git", "commit", "--amend", "--no-edit"];
WriteLine($"Testing args: [{string.Join(", ", testArgs.Select(a => $"'{a}'"))}]");

try
{
    var result = await app.RunAsync(testArgs);
    WriteLine($"Result code: {result}");
}
catch (Exception ex)
{
    WriteLine($"ERROR: {ex.Message}");
    WriteLine($"Stack: {ex.StackTrace}");
}