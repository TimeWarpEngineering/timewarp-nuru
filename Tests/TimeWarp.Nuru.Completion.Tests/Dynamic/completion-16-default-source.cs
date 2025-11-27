#!/usr/bin/dotnet --

return await RunTests<DefaultSourceTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class DefaultSourceTests
{
  public static async Task Should_extract_root_level_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);
    builder.Map("help", () => 0);

    var context = new CompletionContext(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

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
    var builder = new NuruAppBuilder();
    builder.Map("git status", () => 0);
    builder.Map("git commit -m {message}", (string message) => 0);
    builder.Map("git push", () => 0);

    var context = new CompletionContext(
      Args: ["app", "git"],
      CursorPosition: 2,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

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
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env} --force --verbose", (string env, bool force, bool verbose) => 0);

    var context = new CompletionContext(
      Args: ["app", "deploy", "production", "-"],
      CursorPosition: 3,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

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
    var builder = new NuruAppBuilder();
    builder.Map("build --configuration,-c {mode}", (string mode) => 0);

    var context = new CompletionContext(
      Args: ["app", "build", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    completions.Any(c => c.Value == "-c").ShouldBeTrue();
    completions.Any(c => c.Value == "--configuration").ShouldBeTrue();
    completions.All(c => c.Type == CompletionType.Option).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_list_when_no_routes_registered()
  {
    // Arrange
    var builder = new NuruAppBuilder();

    var context = new CompletionContext(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    completions.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_command_names()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("deploy {env} --force", (string env, bool force) => 0);
    builder.Map("deploy {env} {tag?}", (string env, string? tag) => 0);

    var context = new CompletionContext(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    completions.Count(c => c.Value == "deploy").ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_filter_commands_by_prefix()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("delete {resource}", (string resource) => 0);
    builder.Map("status", () => 0);

    var context = new CompletionContext(
      Args: ["app", "de"],
      CursorPosition: 1, // Index 1 is the "de" being completed
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert - DefaultCompletionSource filters commands by prefix
    completions.Count.ShouldBe(2);
    completions.Any(c => c.Value == "deploy").ShouldBeTrue();
    completions.Any(c => c.Value == "delete").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("{command} {*args}", (string command, string[] args) => 0);

    var context = new CompletionContext(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    // Should return empty since there are no literal segments to extract
    completions.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_sort_commands_alphabetically()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("zebra", () => 0);
    builder.Map("apple", () => 0);
    builder.Map("mango", () => 0);

    var context = new CompletionContext(
      Args: ["app"],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

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
    var builder = new NuruAppBuilder();
    builder.Map("build --debug --verbose", (bool debug, bool verbose) => 0);
    builder.Map("build --release --quiet", (bool release, bool quiet) => 0);

    var context = new CompletionContext(
      Args: ["app", "build", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

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
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    var context = new CompletionContext(
      Args: ["app", ""],
      CursorPosition: 1,
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    completions.Count.ShouldBe(2);
    completions.Any(c => c.Value == "status").ShouldBeTrue();
    completions.Any(c => c.Value == "version").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_with_alternate_forms()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("run --verbose,-v --quiet,-q", (bool verbose, bool quiet) => 0);

    var context = new CompletionContext(
      Args: ["app", "run", "-"],
      CursorPosition: 2, // Index 2 is the "-" being completed
      Endpoints: builder.EndpointCollection
    );

    var source = new DefaultCompletionSource();

    // Act
    var completions = source.GetCompletions(context).ToList();

    // Assert
    completions.Any(c => c.Value == "-v").ShouldBeTrue();
    completions.Any(c => c.Value == "--verbose").ShouldBeTrue();
    completions.Any(c => c.Value == "-q").ShouldBeTrue();
    completions.Any(c => c.Value == "--quiet").ShouldBeTrue();

    await Task.CompletedTask;
  }
}
