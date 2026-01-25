#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for nullable value type options (long?, int?, etc.)
/// Reproduces GitHub issue #146: Source generator doesn't handle nullable value types for Options
/// </summary>
[TestTag("Routing")]
public class NullableValueTypeOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NullableValueTypeOptionTests>();

  public static async Task Should_parse_nullable_long_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --id {id:long?}")
      .WithHandler((long? id) => $"id:{id}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--id", "123"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("id:123").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_int_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --count {count:int?}")
      .WithHandler((int? count) => $"count:{count}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--count", "42"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("count:42").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_nullable_long_option_without_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --id? {id:long?}")
      .WithHandler((long? id) => $"id:{id?.ToString(CultureInfo.InvariantCulture) ?? "null"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("id:null").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_error_on_invalid_nullable_long_option_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --id {id:long?}")
      .WithHandler((long? id) => $"id:{id}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--id", "notanumber"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_double_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --rate {rate:double?}")
      .WithHandler((double? rate) => $"rate:{rate}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--rate", "3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("rate:3.14").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_bool_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --flag {flag:bool?}")
      .WithHandler((bool? flag) => $"flag:{flag}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--flag", "true"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_nullable_bool_option_without_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --flag? {flag:bool?}")
      .WithHandler((bool? flag) => $"flag:{(flag.HasValue ? flag.Value.ToString() : "null")}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:null").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_guid_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --guid {guid:guid?}")
      .WithHandler((Guid? guid) => $"guid:{guid}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--guid", "12345678-1234-1234-1234-123456789012"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("guid:12345678-1234-1234-1234-123456789012").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_datetime_option_with_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --date {date:datetime?}")
      .WithHandler((DateTime? date) => $"date:{date:yyyy-MM-dd}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--date", "2024-01-15"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("date:2024-01-15").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_nullable_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --id? {id:long?} --count? {count:int?}")
      .WithHandler((long? id, int? count) => $"id:{id?.ToString(CultureInfo.InvariantCulture) ?? "null"}|count:{count?.ToString(CultureInfo.InvariantCulture) ?? "null"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "--id", "123"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("id:123").ShouldBeTrue();
    terminal.OutputContains("count:null").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
