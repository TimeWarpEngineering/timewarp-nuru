#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

using System;
using TimeWarp.Amuru;

Console.WriteLine("=== Real App Test Runner ===\n");

string testFile = "test-real-app.cs";
string appFile = "real-app.cs";

// Step 1: Set up test environment
Console.WriteLine($"1. Setting NURU_TEST={testFile}");
Environment.SetEnvironmentVariable("NURU_TEST", testFile);

// Step 2: Clean to force rebuild with test harness
Console.WriteLine($"2. Cleaning {appFile} to force rebuild...");
CommandOutput cleanResult = await Shell.Builder("dotnet")
  .WithArguments("clean", appFile)
  .WithNoValidation()
  .CaptureAsync();

if (!cleanResult.Success)
{
  Console.WriteLine($"   Clean failed");
  Console.WriteLine(cleanResult.Stderr);
  return 1;
}
Console.WriteLine("   Clean succeeded");

// Step 3: Run tests
Console.WriteLine($"3. Running tests...\n");
CommandOutput testResult = await Shell.Builder("dotnet")
  .WithArguments("run", appFile)
  .WithNoValidation()
  .CaptureAsync();

Console.WriteLine(testResult.Stdout);
if (!string.IsNullOrEmpty(testResult.Stderr))
{
  Console.WriteLine(testResult.Stderr);
}

// Jaribu outputs "X/Y tests passed" where X == Y means all passed
bool testsPassed = testResult.Success && 
  (testResult.Stdout.Contains("0 failed") || 
   System.Text.RegularExpressions.Regex.IsMatch(testResult.Stdout, @"(\d+)/\1 tests passed"));

// Step 4: Clean up - remove env var and rebuild for production
Console.WriteLine("\n4. Cleaning up test environment...");
Environment.SetEnvironmentVariable("NURU_TEST", null);

Console.WriteLine($"5. Cleaning {appFile} to rebuild without test harness...");
CommandOutput cleanupResult = await Shell.Builder("dotnet")
  .WithArguments("clean", appFile)
  .WithNoValidation()
  .CaptureAsync();

if (!cleanupResult.Success)
{
  Console.WriteLine($"   Cleanup clean failed");
  Console.WriteLine(cleanupResult.Stderr);
}

// Step 5: Verify normal operation
Console.WriteLine($"6. Verifying normal operation...");
CommandOutput verifyResult = await Shell.Builder("dotnet")
  .WithArguments("run", appFile, "--", "greet", "TestRunner")
  .WithNoValidation()
  .CaptureAsync();

if (verifyResult.Success && verifyResult.Stdout.Contains("Hello, TestRunner!"))
{
  Console.WriteLine("   Normal operation verified");
}
else
{
  Console.WriteLine($"   Verification failed");
  Console.WriteLine(verifyResult.Stdout);
  Console.WriteLine(verifyResult.Stderr);
  return 1;
}

// Summary
Console.WriteLine("\n=== Summary ===");
if (testsPassed)
{
  Console.WriteLine("All tests passed and normal operation verified!");
  return 0;
}
else
{
  Console.WriteLine("Tests failed!");
  return 1;
}