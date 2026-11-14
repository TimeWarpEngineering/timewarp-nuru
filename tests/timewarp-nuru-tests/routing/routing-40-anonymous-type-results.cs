#!/usr/bin/env -S dotnet --

#region Purpose
// Pins that a delegate handler returning an anonymous type builds and prints its result through
// ToString(): a sync or async new { ... }, a list of anonymous values, a command, and a null result.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.AnonymousTypeResults
{

// Task 585: the generator declared the result local as `<anonymous type: ...> result`, and the JSON
// context MSBuild task wrote the same name into [JsonSerializable(typeof(...))], so the consumer build
// failed with CS1001 / CS1525. Each test here only compiles when both erase the anonymous type to object.
// System.Text.Json source generation cannot name an anonymous type, so the result prints via ToString().
[TestTag("Routing")]
public class AnonymousTypeResultTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AnonymousTypeResultTests>();

  public static async Task Should_print_anonymous_query_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("atr-info")
        .WithHandler(() => new { Name = "MyApp", Version = "1.0.0" })
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["atr-info"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("{ Name = MyApp, Version = 1.0.0 }");
  }

  public static async Task Should_print_async_anonymous_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("atr-async {count:int}")
        .WithHandler(async (int count) =>
        {
          await Task.Yield();
          return new { Count = count };
        })
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["atr-async", "2"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("{ Count = 2 }");
  }

  public static async Task Should_build_a_list_of_anonymous_values()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("atr-list")
        .WithHandler(() => new[] { new { Id = 1 } }.ToList())
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["atr-list"]);

    // Assert: a List<T> prints its type name through ToString(); the point is that it builds and runs.
    exitCode.ShouldBe(0);
    terminal.Output.ShouldContain("List");
  }

  public static async Task Should_print_anonymous_command_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("atr-cmd")
        .WithHandler(() => new { Ok = true })
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["atr-cmd"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("{ Ok = True }");
  }

  public static async Task Should_write_nothing_for_null_anonymous_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("atr-none")
        .WithHandler(() =>
        {
          var found = new[] { new { Id = 1 } }.FirstOrDefault(item => item.Id > 5);
          return found;
        })
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["atr-none"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBeEmpty();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing.AnonymousTypeResults
