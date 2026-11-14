#!/usr/bin/env -S dotnet --

#region Purpose
// Pins source-generated binding of the built-in string type constraint ({name:string}, {name:string?},
// --opt {value:string}), which binds the captured value with no conversion, as an untyped {name} does.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

// Task 586: `string` is a built-in type constraint, but the generator had no string branch in
// EmitTypeConversions. It emitted "WARNING: No converter found for type constraint 'string'" and
// never declared the handler variable, so `{message:string}` failed with CS0103 in NuruGenerated.g.cs.
// Each test here only compiles when the generator binds the string-typed value.
[TestTag("Routing")]
public class StringTypeConstraintTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<StringTypeConstraintTests>();

  public static async Task Should_bind_string_typed_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {message:string}").WithHandler((string message) => $"message:{message}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "hello"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("message:hello").ShouldBeTrue();
  }

  public static async Task Should_bind_string_typed_parameter_with_uppercase_constraint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("shout {message:String}").WithHandler((string message) => $"shout:{message}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["shout", "hey"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("shout:hey").ShouldBeTrue();
  }

  public static async Task Should_bind_optional_string_typed_parameter_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name:string?}").WithHandler((string? name) => $"name:{name ?? "NULL"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Ada"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:Ada").ShouldBeTrue();
  }

  public static async Task Should_bind_optional_string_typed_parameter_as_null_when_omitted()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name:string?}").WithHandler((string? name) => $"name:{name ?? "NULL"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:NULL").ShouldBeTrue();
  }

  public static async Task Should_bind_string_typed_parameter_next_to_int_typed_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("repeat {text:string} {count:int}")
        .WithHandler((string text, int count) => $"text:{text},count:{count}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["repeat", "abc", "3"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("text:abc,count:3").ShouldBeTrue();
  }

  public static async Task Should_bind_string_typed_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("tag --label {label:string}").WithHandler((string label) => $"label:{label}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["tag", "--label", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("label:release").ShouldBeTrue();
  }

  public static async Task Should_bind_optional_string_typed_option_when_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("note --title? {title:string?}").WithHandler((string? title) => $"title:{title ?? "NULL"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["note", "--title", "draft"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("title:draft").ShouldBeTrue();
  }

  public static async Task Should_bind_optional_string_typed_option_as_null_when_omitted()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("note --title? {title:string?}").WithHandler((string? title) => $"title:{title ?? "NULL"}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["note"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("title:NULL").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
