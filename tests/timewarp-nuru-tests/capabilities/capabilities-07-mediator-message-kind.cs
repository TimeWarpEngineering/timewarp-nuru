#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Tests that [NuruRoute] endpoints classify their message kind from TimeWarp.Mediator interfaces.
// IIdempotentCommand<T> derives ICommand<T>, so a first-match scan of AllInterfaces could report
// an idempotent command as a plain Command. Precedence is Query, then IdempotentCommand, then Command.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Capabilities.MediatorMessageKind
{

[TestTag("Capabilities")]
[TestTag("Mediator")]
public class MediatorMessageKindTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MediatorMessageKindTests>();

  public static async Task Should_report_query_for_iquery()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkQuery>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-query");
    kind.ShouldBe("query");
  }

  public static async Task Should_report_command_for_icommand()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkCommand>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-command");
    kind.ShouldBe("command");
  }

  public static async Task Should_report_idempotent_command_for_iidempotentcommand()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkIdempotentCommand>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-idempotent");
    kind.ShouldBe("idempotentCommand");
  }

  public static async Task Should_report_idempotent_command_when_icommand_is_listed_first()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkIdempotentCommandListedAfterCommand>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-idempotent-after-command");
    kind.ShouldBe("idempotentCommand");
  }

  public static async Task Should_report_idempotent_command_for_icommand_with_iidempotent_marker()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkIdempotentMarkerCommand>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-idempotent-marker");
    kind.ShouldBe("idempotentCommand");
  }

  public static async Task Should_report_query_when_icommand_is_also_implemented()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder().UseTerminal(terminal).Map<MkCommandAndQuery>().Build();
    int exitCode = await app.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);
    string kind = GetKind(terminal.Output, "mk-command-and-query");
    kind.ShouldBe("query");
  }

  public static async Task Should_run_idempotent_command_handler()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<MkIdempotentCommand>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["mk-idempotent"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mk-idempotent handled").ShouldBeTrue();
  }

  private static string GetKind(string capabilitiesJson, string pattern)
  {
    using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(capabilitiesJson);
    foreach (System.Text.Json.JsonElement endpoint in doc.RootElement.GetProperty("endpoints").EnumerateArray())
    {
      if (endpoint.GetProperty("pattern").GetString() == pattern)
        return endpoint.GetProperty("kind").GetString() ?? string.Empty;
    }

    throw new InvalidOperationException($"Endpoint '{pattern}' not found in --capabilities output.");
  }
}

[NuruRoute("mk-query", Description = "Query")]
public sealed class MkQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<MkQuery, string>
  {
    public Task<string> Handle(MkQuery query, CancellationToken cancellationToken) => Task.FromResult("query");
  }
}

[NuruRoute("mk-command", Description = "Command")]
public sealed class MkCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<MkCommand, Unit>
  {
    public Task<Unit> Handle(MkCommand command, CancellationToken cancellationToken) => Unit.Task;
  }
}

[NuruRoute("mk-idempotent", Description = "Idempotent command")]
public sealed class MkIdempotentCommand : IIdempotentCommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : IIdempotentCommandHandler<MkIdempotentCommand, Unit>
  {
    public async Task<Unit> Handle(MkIdempotentCommand command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync("mk-idempotent handled");
      return Unit.Value;
    }
  }
}

[NuruRoute("mk-idempotent-after-command", Description = "Idempotent command listing ICommand first")]
public sealed class MkIdempotentCommandListedAfterCommand : ICommand<Unit>, IIdempotentCommand<Unit>
{
  public sealed class Handler : IIdempotentCommandHandler<MkIdempotentCommandListedAfterCommand, Unit>
  {
    public Task<Unit> Handle(MkIdempotentCommandListedAfterCommand command, CancellationToken cancellationToken) => Unit.Task;
  }
}

[NuruRoute("mk-idempotent-marker", Description = "Command with IIdempotent marker")]
public sealed class MkIdempotentMarkerCommand : ICommand<Unit>, IIdempotent
{
  public sealed class Handler : ICommandHandler<MkIdempotentMarkerCommand, Unit>
  {
    public Task<Unit> Handle(MkIdempotentMarkerCommand command, CancellationToken cancellationToken) => Unit.Task;
  }
}

[NuruRoute("mk-command-and-query", Description = "Query that also implements ICommand")]
public sealed class MkCommandAndQuery : ICommand<string>, IQuery<string>
{
  public sealed class Handler : IQueryHandler<MkCommandAndQuery, string>
  {
    public Task<string> Handle(MkCommandAndQuery query, CancellationToken cancellationToken) => Task.FromResult("both");
  }
}

} // namespace TimeWarp.Nuru.Tests.Capabilities.MediatorMessageKind
