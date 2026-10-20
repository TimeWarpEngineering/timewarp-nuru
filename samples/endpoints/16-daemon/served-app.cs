#!/usr/bin/env -S dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// SERVED APP - TimeWarp.Nuru.Daemon
// ═══════════════════════════════════════════════════════════════════════════════
// A CLI whose routes run in a long-lived local server once one is running.
// WarmIndex takes about a second to load. Without a server every call pays it;
// with `served-app serve` running, calls forward and pay it once.
//
// DEMONSTRATES:
//   - AddServeRoute(): built-in serve / serve --status / serve --stop
//   - --daemon (forward, starting the server) and --no-daemon (run here)
//   - [LocalOnly] and .LocalOnly() routes that never forward
//   - concurrent calls: each forwarded call has its own output, exit code and directory
//     (NuruCallContext.CurrentDirectory); [Exclusive] / .Exclusive() serializes a route
//   - AddMcpRoute(): `served-app mcp` forwards tools/call to the same server
//
// USAGE:
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- count hello          # cold, in-process
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- --daemon count hello # starts the server
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- count hello          # warm, forwarded
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- pid                  # the server's pid
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- local-pid            # this process's pid
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- serve --status
//   dotnet run samples/endpoints/16-daemon/served-app.cs -- serve --stop
// ═══════════════════════════════════════════════════════════════════════════════
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

using Microsoft.Extensions.DependencyInjection;
using ServedApp;
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("Sample served app: routes forward to a warm local server when one runs.")
  .ConfigureServices(services => services.AddSingleton<WarmIndex>())
  .DiscoverEndpoints()
  .Map("cwd")
    .WithHandler(() => NuruCallContext.CurrentDirectory)
    .WithDescription("Print the caller's directory (a forwarded call carries it; the server's own directory does not change)")
    .AsQuery()
    .Done()
  .Map("local-echo {text}")
    .WithHandler((string text) => $"{text} (pid {Environment.ProcessId})")
    .WithDescription("Echo text from this process; never forwarded")
    .AsQuery()
    .LocalOnly()
    .Done()
  .AddServeRoute(idleTimeoutSeconds: 300)
  .AddMcpRoute(instructions: "Tools run in the warm server when it is running.")
  .Build();

return await app.RunAsync(args);

namespace ServedApp
{
  /// <summary>State that is slow to load, like a Roslyn workspace.</summary>
  /// <remarks>
  /// Source-generated DI constructs singletons when services initialize, on every CLI call, forwarded
  /// or not. So the expensive work waits for first use: a forwarded call never pays it on the client.
  /// </remarks>
  public sealed class WarmIndex
  {
    private readonly Lazy<DateTimeOffset> Loaded = new(Load);

    public DateTimeOffset LoadedAt => Loaded.Value;

    public int Count(string word)
    {
      _ = Loaded.Value;
      return word.Length;
    }

    private static DateTimeOffset Load()
    {
      Thread.Sleep(1000); // Simulated load.
      return DateTimeOffset.UtcNow;
    }
  }

  [NuruRoute("count", Description = "Count a word's letters with the warm index")]
  public sealed class CountQuery : IQuery<int>
  {
    [Parameter(Description = "The word")]
    public string Word { get; set; } = "";

    public sealed class Handler(WarmIndex index) : IQueryHandler<CountQuery, int>
    {
      public Task<int> Handle(CountQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(index.Count(query.Word));
    }
  }

  [NuruRoute("pid", Description = "Print the id of the process that runs the route")]
  public sealed class PidQuery : IQuery<int>
  {
    public sealed class Handler : IQueryHandler<PidQuery, int>
    {
      public Task<int> Handle(PidQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Environment.ProcessId);
    }
  }

  [LocalOnly]
  [NuruRoute("local-pid", Description = "Print this process's id; never forwarded")]
  public sealed class LocalPidQuery : IQuery<int>
  {
    public sealed class Handler : IQueryHandler<LocalPidQuery, int>
    {
      public Task<int> Handle(LocalPidQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Environment.ProcessId);
    }
  }

  [NuruRoute("fail", Description = "Write a line to stderr and exit with the given code")]
  public sealed class FailCommand : ICommand<Unit>
  {
    [Parameter(Description = "Exit code")]
    public int Code { get; set; }

    public sealed class Handler(ITerminal terminal) : ICommandHandler<FailCommand, Unit>
    {
      public Task<Unit> Handle(FailCommand command, CancellationToken cancellationToken)
      {
        terminal.WriteErrorLine($"failing with {command.Code}");
        // The call's own exit code: Environment.ExitCode is process-wide, and a server runs calls concurrently.
        NuruCallContext.CurrentExitCode = command.Code;
        return Task.FromResult(Unit.Value);
      }
    }
  }
}
