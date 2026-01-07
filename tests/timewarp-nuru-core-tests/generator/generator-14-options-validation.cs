#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// TEST: IValidateOptions<T> automatic validation
// ═══════════════════════════════════════════════════════════════════════════════
//
// Verifies that:
// 1. Generator detects IValidateOptions<T> implementations
// 2. Validation runs automatically when IOptions<T> is bound
// 3. Valid options pass validation
// 4. Invalid options throw OptionsValidationException
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

int passed = 0;
int failed = 0;

// ═══════════════════════════════════════════════════════════════════════════════
// Test 1: Valid options pass validation
// ═══════════════════════════════════════════════════════════════════════════════
try
{
  // Default values in settings file are valid
  NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("test")
      .WithHandler(Handlers.TestHandler)
      .Done()
    .Build();

  await app.RunAsync(["test"]);
  WriteLine("Test 1 PASSED: Valid options pass validation");
  passed++;
}
catch (Exception ex)
{
  WriteLine($"Test 1 FAILED: {ex.Message}");
  failed++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Test 2: Invalid port throws validation exception
// ═══════════════════════════════════════════════════════════════════════════════
try
{
  // Pass invalid port via command line config - all args go to RunAsync
  string[] testArgs = ["test", "--Test:Port=0"];
  NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
    .Map("test")
      .WithHandler(Handlers.TestHandler)
      .Done()
    .Build();

  await app.RunAsync(testArgs);
  WriteLine("Test 2 FAILED: Expected OptionsValidationException");
  failed++;
}
catch (OptionsValidationException ex)
{
  if (ex.Message.Contains("Port must be between"))
  {
    WriteLine("Test 2 PASSED: Invalid port throws validation exception");
    passed++;
  }
  else
  {
    WriteLine($"Test 2 FAILED: Wrong message: {ex.Message}");
    failed++;
  }
}
catch (Exception ex)
{
  WriteLine($"Test 2 FAILED: Wrong exception type: {ex.GetType().Name}");
  failed++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Test 3: Empty required field throws validation exception
// ═══════════════════════════════════════════════════════════════════════════════
try
{
  // Pass empty name via command line config - all args go to RunAsync
  string[] testArgs = ["test", "--Test:Name="];
  NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
    .Map("test")
      .WithHandler(Handlers.TestHandler)
      .Done()
    .Build();

  await app.RunAsync(testArgs);
  WriteLine("Test 3 FAILED: Expected OptionsValidationException");
  failed++;
}
catch (OptionsValidationException ex)
{
  if (ex.Message.Contains("Name is required"))
  {
    WriteLine("Test 3 PASSED: Empty required field throws validation exception");
    passed++;
  }
  else
  {
    WriteLine($"Test 3 FAILED: Wrong message: {ex.Message}");
    failed++;
  }
}
catch (Exception ex)
{
  WriteLine($"Test 3 FAILED: Wrong exception type: {ex.GetType().Name}");
  failed++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Test 4: Multiple validation failures reported
// ═══════════════════════════════════════════════════════════════════════════════
try
{
  // Pass multiple invalid values - all args go to RunAsync
  string[] testArgs = ["test", "--Test:Name=", "--Test:Port=0"];
  NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
    .Map("test")
      .WithHandler(Handlers.TestHandler)
      .Done()
    .Build();

  await app.RunAsync(testArgs);
  WriteLine("Test 4 FAILED: Expected OptionsValidationException");
  failed++;
}
catch (OptionsValidationException ex)
{
  if (ex.Message.Contains("Name is required") && ex.Message.Contains("Port must be between"))
  {
    WriteLine("Test 4 PASSED: Multiple validation failures reported");
    passed++;
  }
  else
  {
    WriteLine($"Test 4 FAILED: Expected both errors, got: {ex.Message}");
    failed++;
  }
}
catch (Exception ex)
{
  WriteLine($"Test 4 FAILED: Wrong exception type: {ex.GetType().Name}");
  failed++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// Test 5: Options without validator work (no validation)
// ═══════════════════════════════════════════════════════════════════════════════
try
{
  NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("unvalidated")
      .WithHandler(Handlers.UnvalidatedHandler)
      .Done()
    .Build();

  await app.RunAsync(["unvalidated"]);
  WriteLine("Test 5 PASSED: Options without validator work");
  passed++;
}
catch (Exception ex)
{
  WriteLine($"Test 5 FAILED: {ex.Message}");
  failed++;
}

// Summary
WriteLine();
WriteLine($"Total: {passed + failed}");
WriteLine($"Passed: {passed}");
if (failed > 0)
  WriteLine($"Failed: {failed}");

return failed > 0 ? 1 : 0;

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  internal static void TestHandler(IOptions<TestOptions> options)
  {
    // Just access the options to trigger binding
    _ = options.Value;
  }

  internal static void UnvalidatedHandler(IOptions<UnvalidatedOptions> options)
  {
    // Just access the options to trigger binding
    _ = options.Value;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// OPTIONS CLASSES
// ═══════════════════════════════════════════════════════════════════════════════

[TimeWarp.Nuru.ConfigurationKey("Test")]
public class TestOptions
{
  public string Name { get; set; } = "default";
  public int Port { get; set; } = 8080;
}

// Options class without a validator
[TimeWarp.Nuru.ConfigurationKey("Unvalidated")]
public class UnvalidatedOptions
{
  public string Value { get; set; } = "anything";
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATORS
// ═══════════════════════════════════════════════════════════════════════════════

public class TestOptionsValidator : IValidateOptions<TestOptions>
{
  public ValidateOptionsResult Validate(string? name, TestOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    List<string> failures = [];

    if (string.IsNullOrWhiteSpace(options.Name))
      failures.Add("Name is required");

    if (options.Port < 1 || options.Port > 65535)
      failures.Add("Port must be between 1 and 65535");

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}
