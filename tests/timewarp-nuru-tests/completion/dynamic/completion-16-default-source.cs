#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.DefaultSource
{

[TestTag("Completion")]
public class DefaultSourceTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DefaultSourceTests>();

  public static async Task Should_extract_root_level_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status").WithHandler(() => { }).AsQuery().Done();
    builder.Map("version").WithHandler(() => { }).AsQuery().Done();
    builder.Map("help").WithHandler(() => { }).AsQuery().Done();

    CompletionContext context = new(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(3);
    completions.Any(c => c.Value == "status").ShouldBeTrue();
    completions.Any(c => c.Value == "version").ShouldBeTrue();
    completions.Any(c => c.Value == "help").ShouldBeTrue();
    completions.All(c => c.Type == CompletionType.Command).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_nested_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git status").WithHandler(() => { }).AsQuery().Done();
    builder.Map("git commit -m {message}").WithHandler((string message) => 0).AsCommand().Done();
    builder.Map("git push").WithHandler(() => { }).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app", "git"],
      CursorPosition: 2,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(3);
    completions.Any(c => c.Value == "status").ShouldBeTrue();
    completions.Any(c => c.Value == "commit").ShouldBeTrue();
    completions.Any(c => c.Value == "push").ShouldBeTrue();
    completions.All(c => c.Type == CompletionType.Command).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_when_cursor_on_dash()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --force --verbose").WithHandler((string env, bool force, bool verbose) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app", "deploy", "production", "-"],
      CursorPosition: 3,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBeGreaterThanOrEqualTo(2);
    completions.Any(c => c.Value == "--force").ShouldBeTrue();
    completions.Any(c => c.Value == "--verbose").ShouldBeTrue();
    completions.All(c => c.Type == CompletionType.Option).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_both_long_and_short_option_forms()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --configuration,-c {mode}").WithHandler((string mode) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app", "build", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Any(c => c.Value == "-c").ShouldBeTrue();
    completions.Any(c => c.Value == "--configuration").ShouldBeTrue();
    completions.All(c => c.Type == CompletionType.Option).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_list_when_no_routes_registered()
  {
    // Arrange
    NuruAppBuilder builder = new();

    CompletionContext context = new(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_command_names()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}").WithHandler((string env) => 0).AsCommand().Done();
    builder.Map("deploy {env} --force").WithHandler((string env, bool force) => 0).AsCommand().Done();
    builder.Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count(c => c.Value == "deploy").ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_filter_commands_by_prefix()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}").WithHandler((string env) => 0).AsCommand().Done();
    builder.Map("delete {resource}").WithHandler((string resource) => 0).AsCommand().Done();
    builder.Map("status").WithHandler(() => { }).AsQuery().Done();

    CompletionContext context = new(
      Args: ["app", "de"],
      CursorPosition: 1, // Index 1 is the "de" being completed
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert - DefaultCompletionSource filters commands by prefix
    completions.Count.ShouldBe(2);
    completions.Any(c => c.Value == "deploy").ShouldBeTrue();
    completions.Any(c => c.Value == "delete").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("{command} {*args}").WithHandler((string command, string[] args) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    // Should return empty since there are no literal segments to extract
    completions.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_sort_commands_alphabetically()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("zebra").WithHandler(() => { }).AsQuery().Done();
    builder.Map("apple").WithHandler(() => { }).AsQuery().Done();
    builder.Map("mango").WithHandler(() => { }).AsQuery().Done();

    CompletionContext context = new(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(3);
    completions[0].Value.ShouldBe("apple");
    completions[1].Value.ShouldBe("mango");
    completions[2].Value.ShouldBe("zebra");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_from_multiple_routes()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --debug --verbose").WithHandler((bool debug, bool verbose) => 0).AsCommand().Done();
    builder.Map("build --release --quiet").WithHandler((bool release, bool quiet) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app", "build", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBeGreaterThanOrEqualTo(4);
    completions.Any(c => c.Value == "--debug").ShouldBeTrue();
    completions.Any(c => c.Value == "--verbose").ShouldBeTrue();
    completions.Any(c => c.Value == "--release").ShouldBeTrue();
    completions.Any(c => c.Value == "--quiet").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_context_with_no_previous_words()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status").WithHandler(() => { }).AsQuery().Done();
    builder.Map("version").WithHandler(() => { }).AsQuery().Done();

    CompletionContext context = new(
      Args: ["app", ""],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Count.ShouldBe(2);
    completions.Any(c => c.Value == "status").ShouldBeTrue();
    completions.Any(c => c.Value == "version").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_with_alternate_forms()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("run --verbose,-v --quiet,-q").WithHandler((bool verbose, bool quiet) => 0).AsCommand().Done();

    CompletionContext context = new(
      Args: ["app", "run", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    DefaultCompletionSource source = new();

    // Act
    List<CompletionCandidate> completions = [.. source.GetCompletions(context)];

    // Assert
    completions.Any(c => c.Value == "-v").ShouldBeTrue();
    completions.Any(c => c.Value == "--verbose").ShouldBeTrue();
    completions.Any(c => c.Value == "-q").ShouldBeTrue();
    completions.Any(c => c.Value == "--quiet").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.DefaultSource
