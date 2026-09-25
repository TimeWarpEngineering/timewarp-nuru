#!/usr/bin/env -S dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: TimeWarp.Mediator generated mediator registration
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify that a Nuru app using UseMicrosoftDependencyInjection() gets the
// TimeWarp.Mediator source-generated mediator registered via AddGeneratedMediator().
//
// WHAT THIS TESTS:
// - TimeWarp.Mediator.Generators flows from TimeWarp.Nuru to the app compilation
// - Nuru's runtime DI container resolves ISender / IPublisher / IMediator
// - Source-gen DI resolves ISender / IPublisher for delegate and endpoint handlers
// - Mediator handlers in the app compilation are linked (graph membership)
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIATOR MESSAGES AND HANDLERS (global scope so the mediator generator links them)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record Gm46GreetQuery(string Name) : global::TimeWarp.Mediator.IQuery<string>;

public sealed class Gm46GreetQueryHandler : global::TimeWarp.Mediator.IQueryHandler<Gm46GreetQuery, string>
{
  public Task<string> Handle(Gm46GreetQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);
    return Task.FromResult($"Mediated hello, {query.Name}!");
  }
}

public sealed record Gm46PingNotification(string Message) : global::TimeWarp.Mediator.INotification;

public sealed class Gm46PingNotificationHandler : global::TimeWarp.Mediator.INotificationHandler<Gm46PingNotification>
{
  public static string? LastMessage { get; set; }

  public Task Handle(Gm46PingNotification notification, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(notification);
    LastMessage = notification.Message;
    return Task.CompletedTask;
  }
}

public sealed class Gm46Marker;

public sealed record Gm46EchoCommand(string Text) : global::TimeWarp.Mediator.ICommand<global::TimeWarp.Mediator.Unit>;

public sealed class Gm46EchoCommandHandler(ITerminal terminal)
  : global::TimeWarp.Mediator.ICommandHandler<Gm46EchoCommand, global::TimeWarp.Mediator.Unit>
{
  public async Task<global::TimeWarp.Mediator.Unit> Handle(Gm46EchoCommand command, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(command);
    await terminal.WriteLineAsync($"echo: {command.Text}");
    return global::TimeWarp.Mediator.Unit.Value;
  }
}

[NuruRoute("gm46-endpoint-greet", Description = "Endpoint handler that sends through ISender")]
public sealed class Gm46SenderEndpoint : global::TimeWarp.Mediator.IQuery<string>
{
  [Parameter(Description = "Name to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(global::TimeWarp.Mediator.ISender sender)
    : global::TimeWarp.Mediator.IQueryHandler<Gm46SenderEndpoint, string>
  {
    public Task<string> Handle(Gm46SenderEndpoint query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      return sender.Send(new Gm46GreetQuery(query.Name), cancellationToken);
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.GeneratedMediator
{
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("RuntimeDI")]
  [TestTag("Mediator")]
  public class GeneratedMediatorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<GeneratedMediatorTests>();

    public static async Task Should_resolve_generated_sender_under_runtime_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .Map("gm46-greet {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender sender) =>
          {
            string greeting = await sender.Send(new Gm46GreetQuery(name));
            return greeting;
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-greet", "Nuru"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Mediated hello, Nuru!").ShouldBeTrue();
    }

    public static async Task Should_resolve_generated_publisher_under_runtime_di()
    {
      // Arrange
      Gm46PingNotificationHandler.LastMessage = null;
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services => services.AddSingleton<Gm46Marker>())
        .Map("gm46-ping {message}")
          .WithHandler(async (string message, global::TimeWarp.Mediator.IPublisher publisher) =>
          {
            await publisher.Publish(new Gm46PingNotification(message));
          })
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-ping", "published"]);

      // Assert
      exitCode.ShouldBe(0);
      Gm46PingNotificationHandler.LastMessage.ShouldBe("published");
    }

    public static async Task Should_resolve_generated_mediator_type()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .Map("gm46-mediator")
          .WithHandler((global::TimeWarp.Mediator.IMediator mediator) => mediator.GetType().FullName ?? string.Empty)
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-mediator"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("TimeWarp.Mediator.Generated.Mediator").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("Mediator")]
  public class GeneratedMediatorSourceGenDITests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<GeneratedMediatorSourceGenDITests>();

    public static async Task Should_resolve_generated_sender_under_source_gen_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm46-static-greet {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender sender) =>
          {
            string greeting = await sender.Send(new Gm46GreetQuery(name));
            return greeting;
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-static-greet", "Static"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Mediated hello, Static!").ShouldBeTrue();
    }

    public static async Task Should_resolve_generated_publisher_under_source_gen_di()
    {
      // Arrange
      Gm46PingNotificationHandler.LastMessage = null;
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm46-static-ping {message}")
          .WithHandler(async (string message, global::TimeWarp.Mediator.IPublisher publisher) =>
          {
            await publisher.Publish(new Gm46PingNotification(message));
          })
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-static-ping", "static-published"]);

      // Assert
      exitCode.ShouldBe(0);
      Gm46PingNotificationHandler.LastMessage.ShouldBe("static-published");
    }

    public static async Task Should_inject_sender_into_endpoint_handler_under_source_gen_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<Gm46SenderEndpoint>()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-endpoint-greet", "Endpoint"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Mediated hello, Endpoint!").ShouldBeTrue();
    }

    public static async Task Should_supply_nuru_terminal_to_mediator_handlers_under_source_gen_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm46-static-echo {text}")
          .WithHandler(async (string text, global::TimeWarp.Mediator.ISender sender) =>
          {
            await sender.Send(new Gm46EchoCommand(text));
          })
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm46-static-echo", "bridged"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("echo: bridged").ShouldBeTrue();
    }
  }
}
