#!/usr/bin/dotnet --

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

return await RunTests<ValidateOnStartTests>(clearCache: true);

[TestTag("Configuration")]
[ClearRunfileCache]
public class ValidateOnStartTests
{
  // Test that valid configuration passes startup validation
  public static async Task Should_pass_validation_with_valid_configuration()
  {
    // Arrange
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddOptions<ValidAppOptions>()
          .Configure(options =>
          {
            options.Name = "TestApp";
            options.Port = 8080;
            options.Timeout = 30;
          })
          .ValidateDataAnnotations()
          .ValidateOnStart();
      })
      .AddRoute("test", (IOptions<ValidAppOptions> options) =>
      {
        options.Value.Name.ShouldBe("TestApp");
        return 0;
      })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }

  // Test that invalid configuration fails during Build() (startup), not during RunAsync()
  public static async Task Should_throw_during_build_with_invalid_configuration()
  {
    // Arrange & Act
    OptionsValidationException? caughtException = null;
    try
    {
      NuruApp app = new NuruAppBuilder()
        .AddDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddOptions<ValidAppOptions>()
            .Configure(options =>
            {
              options.Name = ""; // Invalid - required field
              options.Port = 99999; // Invalid - out of range
              options.Timeout = -5; // Invalid - negative
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        })
        .AddRoute("test", (IOptions<ValidAppOptions> _) => 0)
        .Build(); // Should throw here, not during RunAsync

      await app.RunAsync(["test"]); // This line should never execute
    }
    catch (OptionsValidationException ex)
    {
      caughtException = ex;
    }

    // Assert
    caughtException.ShouldNotBeNull("Validation should have thrown during Build()");
    caughtException.Message.ShouldContain("ValidAppOptions");

    await Task.CompletedTask;
  }

  // Test that validation is optional (apps without ValidateOnStart work normally)
  public static async Task Should_work_without_validate_on_start()
  {
    // Arrange
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddOptions<ValidAppOptions>()
          .Configure(options =>
          {
            options.Name = ""; // Invalid, but no ValidateOnStart
            options.Port = 99999;
            options.Timeout = -5;
          })
          .ValidateDataAnnotations();
        // Note: NO .ValidateOnStart() - validation happens lazily
      })
      .AddRoute("test", () => 0) // Don't access options
      .Build(); // Should NOT throw

    // Act
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0, "App should run successfully when options are not accessed");

    await Task.CompletedTask;
  }

  // Test custom validation logic
  public static async Task Should_throw_for_custom_validation_failure()
  {
    // Arrange & Act
    OptionsValidationException? caughtException = null;
    try
    {
      NuruApp app = new NuruAppBuilder()
        .AddDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddOptions<CustomValidationOptions>()
            .Configure(options =>
            {
              options.StartDate = new DateTime(2025, 1, 1);
              options.EndDate = new DateTime(2024, 1, 1); // Before start date
            })
            .Validate(opts =>
            {
              return opts.EndDate > opts.StartDate; // Custom validation
            }, "EndDate must be after StartDate")
            .ValidateOnStart();
        })
        .AddRoute("test", () => 0)
        .Build(); // Should throw here

      await app.RunAsync(["test"]); // Never reached
    }
    catch (OptionsValidationException ex)
    {
      caughtException = ex;
    }

    // Assert
    caughtException.ShouldNotBeNull();
    caughtException.Message.ShouldContain("EndDate must be after StartDate");

    await Task.CompletedTask;
  }

  // Test that apps without DI are not affected (no ServiceProvider means no validators)
  public static async Task Should_work_without_dependency_injection()
  {
    // Arrange
    NuruApp app = new NuruAppBuilder()
      // No AddDependencyInjection() call
      .AddRoute("test", () => 0)
      .Build(); // Should not throw

    // Act
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }
}

// Options classes for testing

internal sealed class ValidAppOptions
{
  [Required(ErrorMessage = "Name is required")]
  [MinLength(1, ErrorMessage = "Name must not be empty")]
  public string Name { get; set; } = "";

  [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
  public int Port { get; set; }

  [Range(1, 3600, ErrorMessage = "Timeout must be between 1 and 3600 seconds")]
  public int Timeout { get; set; }
}

internal sealed class CustomValidationOptions
{
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
}
