#!/usr/bin/dotnet --

using System.ComponentModel;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.EnumSource
{

[TestTag("Completion")]
public class EnumSourceTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EnumSourceTests>();

  public static async Task Should_extract_all_enum_values()
  {
    // Arrange
    EnumCompletionSource<TestEnvironment> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(4);
    completions.Any(c => c.Value == "Development").ShouldBeTrue();
    completions.Any(c => c.Value == "Staging").ShouldBeTrue();
    completions.Any(c => c.Value == "Production").ShouldBeTrue();
    completions.Any(c => c.Value == "Testing").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_descriptions_from_attribute()
  {
    // Arrange
    EnumCompletionSource<DeploymentMode> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    CompletionCandidate fast = completions.First(c => c.Value == "Fast");
    fast.Description.ShouldBe("Fast deployment without health checks");

    CompletionCandidate standard = completions.First(c => c.Value == "Standard");
    standard.Description.ShouldBe("Standard deployment with rolling updates");

    CompletionCandidate blueGreen = completions.First(c => c.Value == "BlueGreen");
    blueGreen.Description.ShouldBe("Blue-green deployment with zero downtime");

    CompletionCandidate canary = completions.First(c => c.Value == "Canary");
    canary.Description.ShouldBe("Canary deployment with gradual rollout");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_without_description_attribute()
  {
    // Arrange
    EnumCompletionSource<TestEnvironment> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert - Without DescriptionAttribute, fallback shows "Value: {numeric_value}"
    completions.All(c => c.Description!.StartsWith("Value:", StringComparison.Ordinal)).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_mixed_descriptions()
  {
    // Arrange
    EnumCompletionSource<LogLevel> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    CompletionCandidate debug = completions.First(c => c.Value == "Debug");
    debug.Description.ShouldBe("Detailed debug information");

    CompletionCandidate info = completions.First(c => c.Value == "Info");
    info.Description.ShouldBe("Informational messages");

    // Warning has no description - fallback to "Value: {numeric_value}"
    CompletionCandidate warning = completions.First(c => c.Value == "Warning");
    warning.Description.ShouldStartWith("Value:");

    // Error has description
    CompletionCandidate error = completions.First(c => c.Value == "Error");
    error.Description.ShouldBe("Error conditions");

    await Task.CompletedTask;
  }

  public static async Task Should_return_candidates_in_alphabetical_order()
  {
    // Arrange
    EnumCompletionSource<DeploymentMode> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert - EnumCompletionSource sorts alphabetically (StringComparer.Ordinal)
    completions[0].Value.ShouldBe("BlueGreen");
    completions[1].Value.ShouldBe("Canary");
    completions[2].Value.ShouldBe("Fast");
    completions[3].Value.ShouldBe("Standard");

    await Task.CompletedTask;
  }

  public static async Task Should_set_correct_completion_type()
  {
    // Arrange
    EnumCompletionSource<TestEnvironment> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.All(c => c.Type == CompletionType.Parameter).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_single_value()
  {
    // Arrange
    EnumCompletionSource<SingleValueEnum> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(1);
    completions[0].Value.ShouldBe("OnlyValue");

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_pascal_case_in_enum_value_names()
  {
    // Arrange
    EnumCompletionSource<DeploymentMode> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Any(c => c.Value == "BlueGreen").ShouldBeTrue(); // Not "bluegreen" or "blue-green"

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_explicit_numeric_values()
  {
    // Arrange
    EnumCompletionSource<ExplicitValuesEnum> source = new();
    CompletionContext context = CreateContext();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(3);
    completions.Any(c => c.Value == "First").ShouldBeTrue();
    completions.Any(c => c.Value == "Second").ShouldBeTrue();
    completions.Any(c => c.Value == "Third").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_ignore_context_parameter()
  {
    // Arrange
    EnumCompletionSource<TestEnvironment> source = new();
    CompletionContext context1 = CreateContext("arg1", "arg2");
    CompletionContext context2 = CreateContext("different", "args");

    // Act
    List<CompletionCandidate> completions1 = [.. source.GetCompletions(context1)];
    List<CompletionCandidate> completions2 = [.. source.GetCompletions(context2)];

    // Assert - Context doesn't affect enum extraction
    completions1.Count.ShouldBe(completions2.Count);
    completions1.Select(c => c.Value).ShouldBe(completions2.Select(c => c.Value));

    await Task.CompletedTask;
  }

  private static CompletionContext CreateContext(params string[] args)
  {
    NuruAppBuilder builder = new();
    return new CompletionContext(
      Args: args.Length > 0 ? args : ["app"],
      CursorPosition: args.Length > 0 ? args.Length : 1,
      Endpoints: builder.EndpointCollection
    );
  }
}

// =============================================================================
// Test Enums
// =============================================================================

enum TestEnvironment
{
  Development,
  Staging,
  Production,
  Testing
}

enum DeploymentMode
{
  [Description("Fast deployment without health checks")]
  Fast,

  [Description("Standard deployment with rolling updates")]
  Standard,

  [Description("Blue-green deployment with zero downtime")]
  BlueGreen,

  [Description("Canary deployment with gradual rollout")]
  Canary
}

enum LogLevel
{
  [Description("Detailed debug information")]
  Debug,

  [Description("Informational messages")]
  Info,

  Warning,

  [Description("Error conditions")]
  Error
}

enum SingleValueEnum
{
  OnlyValue
}

enum ExplicitValuesEnum
{
  First = 10,
  Second = 20,
  Third = 30
}

} // namespace TimeWarp.Nuru.Tests.Completion.EnumSource
