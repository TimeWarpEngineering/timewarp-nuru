#!/usr/bin/dotnet --
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

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.OptionsValidation
{
  [TestTag("Generator")]
  [TestTag("IOptions")]
  [TestTag("Validation")]
  public class OptionsValidationTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OptionsValidationTests>();

    /// <summary>
    /// Valid options should pass validation without throwing.
    /// </summary>
    public static async Task Should_pass_validation_with_valid_options()
    {
      // Arrange - Default values in TestOptions are valid
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("test")
          .WithHandler(Handlers.TestHandler)
          .Done()
        .Build();

      // Act & Assert - Should not throw
      int exitCode = await app.RunAsync(["test"]);
      exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Invalid port (0) should throw OptionsValidationException.
    /// </summary>
    public static async Task Should_throw_when_port_invalid()
    {
      // Arrange - Pass invalid port via command line config
      string[] testArgs = ["test", "--Test:Port=0"];
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .Map("test")
          .WithHandler(Handlers.TestHandler)
          .Done()
        .Build();

      // Act & Assert
      try
      {
        await app.RunAsync(testArgs);
        throw new ShouldAssertException("Expected OptionsValidationException but none was thrown");
      }
      catch (OptionsValidationException ex)
      {
        ex.Message.ShouldContain("Port must be between");
      }
    }

    /// <summary>
    /// Empty required field should throw OptionsValidationException.
    /// </summary>
    public static async Task Should_throw_when_required_field_empty()
    {
      // Arrange - Pass empty name via command line config
      string[] testArgs = ["test", "--Test:Name="];
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .Map("test")
          .WithHandler(Handlers.TestHandler)
          .Done()
        .Build();

      // Act & Assert
      // Note: Using try-catch instead of Should.ThrowAsync because interceptors
      // cannot intercept calls inside lambdas (see #367)
      try
      {
        await app.RunAsync(testArgs);
        throw new ShouldAssertException("Expected OptionsValidationException but none was thrown");
      }
      catch (OptionsValidationException ex)
      {
        ex.Message.ShouldContain("Name is required");
      }
    }

    /// <summary>
    /// Multiple validation failures should all be reported.
    /// </summary>
    public static async Task Should_report_multiple_validation_failures()
    {
      // Arrange - Pass multiple invalid values
      string[] testArgs = ["test", "--Test:Name=", "--Test:Port=0"];
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .Map("test")
          .WithHandler(Handlers.TestHandler)
          .Done()
        .Build();

      // Act & Assert
      // Note: Using try-catch instead of Should.ThrowAsync because interceptors
      // cannot intercept calls inside lambdas (see #367)
      try
      {
        await app.RunAsync(testArgs);
        throw new ShouldAssertException("Expected OptionsValidationException but none was thrown");
      }
      catch (OptionsValidationException ex)
      {
        ex.Message.ShouldContain("Name is required");
        ex.Message.ShouldContain("Port must be between");
      }
    }

    /// <summary>
    /// Options without a validator should work normally (no validation).
    /// </summary>
    public static async Task Should_work_without_validator()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("unvalidated")
          .WithHandler(Handlers.UnvalidatedHandler)
          .Done()
        .Build();

      // Act & Assert - Should not throw
      int exitCode = await app.RunAsync(["unvalidated"]);
      exitCode.ShouldBe(0);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // HANDLERS
  // ═══════════════════════════════════════════════════════════════════════════════

  internal static class Handlers
  {
    internal static void TestHandler(IOptions<TestOptions> options)
    {
      // Just access the options to trigger binding and validation
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

  [ConfigurationKey("Test")]
  public class TestOptions
  {
    public string Name { get; set; } = "default";
    public int Port { get; set; } = 8080;
  }

  /// <summary>
  /// Options class without a validator - should work without validation.
  /// </summary>
  [ConfigurationKey("Unvalidated")]
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
}
