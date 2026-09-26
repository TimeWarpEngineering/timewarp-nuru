#!/usr/bin/env -S dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Expression-bodied async lambda handlers (kanban 474)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: The delegate emitter used to prepend `await` to every async expression body,
// so `async (ISender s) => await s.Send(q)` became `=> await await s.Send(q)` and the
// generated code failed to compile. Each handler below is compiled through the real
// generator; if the emitted code were invalid this file would not build.
//
// WHAT THIS TESTS:
// - async (ISender s) => await s.Send(q) end to end (source-gen DI and runtime DI)
// - await ... .ConfigureAwait(false), parenthesized await, nested awaits, await inside
//   an interpolated string, and void async expression bodies
// - non-async Task-returning expression bodies and async block bodies are unchanged
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIATOR MESSAGES AND HANDLERS (global scope so the mediator generator links them)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record Gm47GreetQuery(string Name) : global::TimeWarp.Mediator.IQuery<string>;

public sealed class Gm47GreetQueryHandler : global::TimeWarp.Mediator.IQueryHandler<Gm47GreetQuery, string>
{
  public Task<string> Handle(Gm47GreetQuery query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);
    return Task.FromResult($"Gm47 hello, {query.Name}!");
  }
}

public sealed record Gm47PingNotification(string Message) : global::TimeWarp.Mediator.INotification;

public sealed class Gm47PingNotificationHandler : global::TimeWarp.Mediator.INotificationHandler<Gm47PingNotification>
{
  public static string? LastMessage { get; set; }

  public Task Handle(Gm47PingNotification notification, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(notification);
    LastMessage = notification.Message;
    return Task.CompletedTask;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.AsyncExpressionBodyLambdas
{
  [TestTag("Generator")]
  [TestTag("Mediator")]
  public class AsyncExpressionBodyLambdaTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AsyncExpressionBodyLambdaTests>();

    public static async Task Should_send_through_isender_with_await_expression_body_under_source_gen_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-send {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) => await s.Send(new Gm47GreetQuery(name)))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-send", "Static"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Static!").ShouldBeTrue();
    }

    public static async Task Should_send_through_isender_with_await_expression_body_under_runtime_di()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .Map("gm47-runtime-send {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) => await s.Send(new Gm47GreetQuery(name)))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-runtime-send", "Runtime"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Runtime!").ShouldBeTrue();
    }

    public static async Task Should_support_configure_await_expression_body()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-configure-await {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) =>
            await s.Send(new Gm47GreetQuery(name)).ConfigureAwait(false))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-configure-await", "Configured"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Configured!").ShouldBeTrue();
    }

    public static async Task Should_support_parenthesized_await_expression_body()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-parenthesized {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) =>
            (await s.Send(new Gm47GreetQuery(name))).ToUpperInvariant())
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-parenthesized", "Paren"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("GM47 HELLO, PAREN!").ShouldBeTrue();
    }

    public static async Task Should_support_nested_await_expression_body()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-nested {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) =>
            await (await Task.FromResult(s)).Send(new Gm47GreetQuery(await Task.FromResult(name))))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-nested", "Nested"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Nested!").ShouldBeTrue();
    }

    public static async Task Should_support_await_inside_non_await_leading_expression_body()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-interpolated {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) =>
            $"[{await s.Send(new Gm47GreetQuery(name))}]")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-interpolated", "Interp"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("[Gm47 hello, Interp!]").ShouldBeTrue();
    }

    public static async Task Should_support_void_await_expression_body()
    {
      // Arrange
      Gm47PingNotificationHandler.LastMessage = null;
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-publish {message}")
          .WithHandler(async (string message, global::TimeWarp.Mediator.IPublisher p) =>
            await p.Publish(new Gm47PingNotification(message)))
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-publish", "voided"]);

      // Assert
      exitCode.ShouldBe(0);
      Gm47PingNotificationHandler.LastMessage.ShouldBe("voided");
    }

    public static async Task Should_still_await_non_async_task_returning_expression_body()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-non-async {name}")
          .WithHandler((string name, global::TimeWarp.Mediator.ISender s) => s.Send(new Gm47GreetQuery(name)))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-non-async", "Plain"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Plain!").ShouldBeTrue();
    }

    public static async Task Should_leave_async_block_body_unchanged()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("gm47-block {name}")
          .WithHandler(async (string name, global::TimeWarp.Mediator.ISender s) =>
          {
            return await s.Send(new Gm47GreetQuery(name));
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gm47-block", "Block"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Gm47 hello, Block!").ShouldBeTrue();
    }
  }
}
