#!/usr/bin/dotnet --
#pragma warning disable CA1062 // Validate arguments of public methods

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto
{

/// <summary>
/// Tests for nullable value type options on [NuruRoute] endpoints.
/// Reproduces GitHub issue #149: Source generator does not generate TryParse for nullable value type options.
/// </summary>
[TestTag("Auto")]
public class EndpointNullableOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EndpointNullableOptionTests>();

  public static async Task Should_parse_nullable_long_option_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nulllong", "123", "--exclude", "456"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Id:123").ShouldBeTrue();
    terminal.OutputContains("Exclude:456").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_omitted_nullable_long_option_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nulllong", "123"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Id:123").ShouldBeTrue();
    terminal.OutputContains("Exclude:null").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_nullable_int_option_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nullint", "--max", "100"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Max:100").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_omitted_nullable_int_option_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nullint"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Max:null").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_error_on_invalid_nullable_long_option_value_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nulllong", "123", "--exclude", "notanumber"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_nullable_options_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["nullmulti", "--id", "999", "--count", "50"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Id:999").ShouldBeTrue();
    terminal.OutputContains("Count:50").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_partial_nullable_options_on_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act - only provide one of the two nullable options
    int exitCode = await app.RunAsync(["nullmulti", "--id", "777"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Id:777").ShouldBeTrue();
    terminal.OutputContains("Count:null").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

// Test endpoint with nullable long option
[NuruRoute("nulllong", Description = "Test with nullable long option")]
public sealed class NullLongEndpoint : ICommand<Unit>
{
  [Parameter(Description = "ID parameter")]
  public long Id { get; set; }

  [Option("exclude", "e", Description = "Exclude ID")]
  public long? ExcludeId { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<NullLongEndpoint, Unit>
  {
    public async ValueTask<Unit> Handle(NullLongEndpoint command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync($"Id:{command.Id}");
      await terminal.WriteLineAsync($"Exclude:{command.ExcludeId?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
      return default;
    }
  }
}

// Test endpoint with nullable int option
[NuruRoute("nullint", Description = "Test with nullable int option")]
public sealed class NullIntEndpoint : ICommand<Unit>
{
  [Option("max", "m", Description = "Maximum count")]
  public int? Max { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<NullIntEndpoint, Unit>
  {
    public async ValueTask<Unit> Handle(NullIntEndpoint command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync($"Max:{command.Max?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
      return default;
    }
  }
}

// Test endpoint with multiple nullable options
[NuruRoute("nullmulti", Description = "Test with multiple nullable options")]
public sealed class NullMultiEndpoint : ICommand<Unit>
{
  [Option("id", "i", Description = "Optional ID")]
  public long? Id { get; set; }

  [Option("count", "c", Description = "Optional count")]
  public int? Count { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<NullMultiEndpoint, Unit>
  {
    public async ValueTask<Unit> Handle(NullMultiEndpoint command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync($"Id:{command.Id?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
      await terminal.WriteLineAsync($"Count:{command.Count?.ToString(CultureInfo.InvariantCulture) ?? "null"}");
      return default;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Auto
