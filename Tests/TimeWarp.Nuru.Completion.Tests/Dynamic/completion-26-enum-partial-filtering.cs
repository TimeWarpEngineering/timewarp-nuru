#!/usr/bin/dotnet --

using System.Collections.ObjectModel;

// Test for enum completion filtering by partial input
// This tests the bug where typing "deploy p<tab>" should complete to "deploy prod"
// but the completion system returns all enum values without filtering

return await RunTests<EnumPartialFilteringTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class EnumPartialFilteringTests
{
  public enum Environment
  {
    Dev,
    Staging,
    Prod
  }

  public static async Task Should_filter_enum_completions_by_partial_input()
  {
    // Arrange - Register enum converter and create route
    NuruAppBuilder builder = new();
    builder.AddTypeConverter(new EnumTypeConverter<Environment>());
    builder.Map("deploy {env:environment} {tag?}", (Environment env, string? tag) => 0);

    NuruApp app = builder.Build();
    CompletionProvider provider = new(app.TypeConverterRegistry);

    // Simulate: user typed "deploy p" and pressed tab
    // Args: ["deploy", "p"]
    // HasTrailingSpace: false (cursor is right after 'p')
    CompletionContext context = new(
      Args: ["deploy", "p"],
      CursorPosition: 2,
      Endpoints: app.Endpoints,
      HasTrailingSpace: false
    );

    // Act
    ReadOnlyCollection<CompletionCandidate> completions = provider.GetCompletions(context, app.Endpoints);

    // Assert - Should only return "Prod" (matches "p" prefix)
    completions.Count.ShouldBe(1, "Expected only 'Prod' to match partial input 'p'");
    completions[0].Value.ShouldBe("Prod");

    await Task.CompletedTask;
  }

  public static async Task Should_return_all_enum_values_when_no_partial_input()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddTypeConverter(new EnumTypeConverter<Environment>());
    builder.Map("deploy {env:environment}", (Environment env) => 0);

    NuruApp app = builder.Build();
    CompletionProvider provider = new(app.TypeConverterRegistry);

    // Simulate: user typed "deploy " and pressed tab (trailing space = completing next word)
    CompletionContext context = new(
      Args: ["deploy"],
      CursorPosition: 1,
      Endpoints: app.Endpoints,
      HasTrailingSpace: true
    );

    // Act
    ReadOnlyCollection<CompletionCandidate> completions = provider.GetCompletions(context, app.Endpoints);

    // Assert - Should return all enum values
    completions.Count.ShouldBe(3);
    completions.Any(c => c.Value == "Dev").ShouldBeTrue();
    completions.Any(c => c.Value == "Staging").ShouldBeTrue();
    completions.Any(c => c.Value == "Prod").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_filter_enum_completions_case_insensitively()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddTypeConverter(new EnumTypeConverter<Environment>());
    builder.Map("deploy {env:environment}", (Environment env) => 0);

    NuruApp app = builder.Build();
    CompletionProvider provider = new(app.TypeConverterRegistry);

    // Simulate: user typed "deploy S" (uppercase)
    CompletionContext context = new(
      Args: ["deploy", "S"],
      CursorPosition: 2,
      Endpoints: app.Endpoints,
      HasTrailingSpace: false
    );

    // Act
    ReadOnlyCollection<CompletionCandidate> completions = provider.GetCompletions(context, app.Endpoints);

    // Assert - Should return "Staging" (case-insensitive match)
    completions.Count.ShouldBe(1);
    completions[0].Value.ShouldBe("Staging");

    await Task.CompletedTask;
  }

  public static async Task Should_return_multiple_matches_for_common_prefix()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddTypeConverter(new EnumTypeConverter<LogLevel>());
    builder.Map("log {level:loglevel}", (LogLevel level) => 0);

    NuruApp app = builder.Build();
    CompletionProvider provider = new(app.TypeConverterRegistry);

    // Simulate: user typed "log d" - should match "Debug"
    CompletionContext context = new(
      Args: ["log", "d"],
      CursorPosition: 2,
      Endpoints: app.Endpoints,
      HasTrailingSpace: false
    );

    // Act
    ReadOnlyCollection<CompletionCandidate> completions = provider.GetCompletions(context, app.Endpoints);

    // Assert - Should return "Debug" only
    completions.Count.ShouldBe(1);
    completions[0].Value.ShouldBe("Debug");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_when_no_enum_values_match()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddTypeConverter(new EnumTypeConverter<Environment>());
    builder.Map("deploy {env:environment}", (Environment env) => 0);

    NuruApp app = builder.Build();
    CompletionProvider provider = new(app.TypeConverterRegistry);

    // Simulate: user typed "deploy xyz" - no enum value starts with "xyz"
    CompletionContext context = new(
      Args: ["deploy", "xyz"],
      CursorPosition: 2,
      Endpoints: app.Endpoints,
      HasTrailingSpace: false
    );

    // Act
    ReadOnlyCollection<CompletionCandidate> completions = provider.GetCompletions(context, app.Endpoints);

    // Assert - Should return empty (no matches)
    completions.Count.ShouldBe(0);

    await Task.CompletedTask;
  }
}

public enum LogLevel
{
  Debug,
  Info,
  Warning,
  Error
}
