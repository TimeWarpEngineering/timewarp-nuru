#!/usr/bin/env -S dotnet --

// REPL and single-run lifetimes for Singleton, SessionScoped, CommandScoped, Scoped, and Transient.
// Covers source-generated DI and UseMicrosoftDependencyInjection().

using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CA1000 // Static counters are per closed probe type.
#pragma warning disable CA1002 // Test lists are mutated by handlers.
#pragma warning disable CA1063 // Probes only count Dispose calls.
#pragma warning disable CA1816
#pragma warning disable CA2211

#if !JARIBU_MULTI
return await RunAllTests();
#endif

public static class Gen51Stats<TMarker>
{
  public static int NextId;
  public static int AsyncDisposeCount;
  public static int DisposeCount;
  public static readonly List<int> Seen = [];
  public static readonly List<int> DisposedAtEntry = [];
  public static readonly List<bool> Same = [];

  public static void Reset()
  {
    NextId = 0;
    AsyncDisposeCount = 0;
    DisposeCount = 0;
    Seen.Clear();
    DisposedAtEntry.Clear();
    Same.Clear();
  }
}

public class Gen51Probe<TMarker> : IDisposable, IAsyncDisposable
{
  public int Id { get; } = ++Gen51Stats<TMarker>.NextId;

  public void Dispose() => Gen51Stats<TMarker>.DisposeCount++;

  public ValueTask DisposeAsync()
  {
    Gen51Stats<TMarker>.AsyncDisposeCount++;
    return default;
  }
}

public interface IGen51Singleton
{
  int Id { get; }
}

public interface IGen51Session
{
  int Id { get; }
}

public interface IGen51Command
{
  int Id { get; }

  IGen51Session Session { get; }
}

public interface IGen51Scoped
{
  int Id { get; }
}

public interface IGen51Transient
{
  int Id { get; }
}

public interface IGen51SingletonBag
{
  IGen51Singleton Inner { get; }
}

public interface IGen51SessionBag
{
  IGen51Session Inner { get; }
}

public interface IGen51CommandBag
{
  IGen51Command Inner { get; }
}

public interface IGen51ScopedBag
{
  IGen51Scoped Inner { get; }
}

public interface IGen51TransientBag
{
  IGen51Transient Inner { get; }
}

public sealed class Gen51Singleton : Gen51Probe<Gen51Singleton>, IGen51Singleton;
public sealed class Gen51SingletonBag(IGen51Singleton inner) : IGen51SingletonBag
{
  public IGen51Singleton Inner { get; } = inner;
}

public sealed class Gen51Session : Gen51Probe<Gen51Session>, IGen51Session;
public sealed class Gen51SessionBag(IGen51Session inner) : IGen51SessionBag
{
  public IGen51Session Inner { get; } = inner;
}

public sealed class Gen51Command(IGen51Session session) : Gen51Probe<Gen51Command>, IGen51Command
{
  public IGen51Session Session { get; } = session;
}

public sealed class Gen51CommandBag(IGen51Command inner) : IGen51CommandBag
{
  public IGen51Command Inner { get; } = inner;
}

public sealed class Gen51Scoped : Gen51Probe<Gen51Scoped>, IGen51Scoped;
public sealed class Gen51ScopedBag(IGen51Scoped inner) : IGen51ScopedBag
{
  public IGen51Scoped Inner { get; } = inner;
}

public sealed class Gen51Transient : Gen51Probe<Gen51Transient>, IGen51Transient;
public sealed class Gen51TransientBag(IGen51Transient inner) : IGen51TransientBag
{
  public IGen51Transient Inner { get; } = inner;
}

public sealed record Gen51Ping : IQuery<string>;

public sealed class Gen51PingHandler : IQueryHandler<Gen51Ping, string>
{
  public Task<string> Handle(Gen51Ping query, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(query);
    return Task.FromResult("pong");
  }
}

namespace TimeWarp.Nuru.Tests.Generator.ReplServiceScopes
{
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("REPL")]
  public class SourceGenReplScopeTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SourceGenReplScopeTests>();

    public static async Task Should_keep_single_run_lifetimes()
    {
      Gen51Apps.ResetAll();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(Gen51Apps.RegisterServices)
        .AddRepl()
        .Map("check").WithHandler(Gen51Apps.Check).AsQuery().Done()
        .Map("ping").WithHandler(Gen51Apps.Ping).AsQuery().Done()
        .Build();

      int exitCode = await app.RunAsync(["check"]);

      exitCode.ShouldBe(0);
      Gen51Stats<Gen51Singleton>.Seen.Count.ShouldBe(1);
      Gen51Stats<Gen51Singleton>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Session>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Session>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Session>.DisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Command>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Command>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Scoped>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Scoped>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Transient>.Same.ShouldBe([false]);

      int singletonId = Gen51Stats<Gen51Singleton>.Seen[0];
      int scopedId = Gen51Stats<Gen51Scoped>.Seen[0];
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["check"]);

      exitCode.ShouldBe(0);
      Gen51Stats<Gen51Singleton>.Seen[1].ShouldBe(singletonId);
      Gen51Stats<Gen51Scoped>.Seen[1].ShouldBe(scopedId);
      Gen51Stats<Gen51Session>.Seen[1].ShouldNotBe(Gen51Stats<Gen51Session>.Seen[0]);
      Gen51Stats<Gen51Command>.Seen[1].ShouldNotBe(Gen51Stats<Gen51Command>.Seen[0]);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Scoped>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Session>.AsyncDisposeCount.ShouldBe(2);
      Gen51Stats<Gen51Command>.AsyncDisposeCount.ShouldBe(2);
    }

    public static async Task Should_scope_repl_commands_and_resolve_mediator()
    {
      Gen51Apps.ResetAll();
      using TestTerminal terminal = new();
      terminal.QueueLine("check");
      terminal.QueueLine("ping");
      terminal.QueueLine("check");
      terminal.QueueLine("exit");
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(Gen51Apps.RegisterServices)
        .AddRepl()
        .Map("check").WithHandler(Gen51Apps.Check).AsQuery().Done()
        .Map("ping").WithHandler(Gen51Apps.Ping).AsQuery().Done()
        .Build();

      await app.RunAsync(["--interactive"]);

      Gen51Stats<Gen51Singleton>.Seen.Count.ShouldBe(2);
      Gen51Stats<Gen51Singleton>.Seen[0].ShouldBe(Gen51Stats<Gen51Singleton>.Seen[1]);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Session>.Seen[0].ShouldBe(Gen51Stats<Gen51Session>.Seen[1]);
      Gen51Stats<Gen51Session>.DisposedAtEntry.ShouldBe([0, 0]);
      Gen51Stats<Gen51Session>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Session>.DisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Command>.Seen[0].ShouldNotBe(Gen51Stats<Gen51Command>.Seen[1]);
      Gen51Stats<Gen51Command>.DisposedAtEntry[0].ShouldBe(0);
      Gen51Stats<Gen51Command>.DisposedAtEntry[1].ShouldBeGreaterThan(0);
      Gen51Stats<Gen51Command>.Same.ShouldBe([true, true]);
      Gen51Stats<Gen51Scoped>.Seen[0].ShouldNotBe(Gen51Stats<Gen51Scoped>.Seen[1]);
      Gen51Stats<Gen51Scoped>.DisposedAtEntry[0].ShouldBe(0);
      Gen51Stats<Gen51Scoped>.DisposedAtEntry[1].ShouldBeGreaterThan(0);
      Gen51Stats<Gen51Transient>.Same.ShouldBe([false, false]);
      terminal.OutputContains("pong").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("RuntimeDI")]
  [TestTag("REPL")]
  public class RuntimeDiReplScopeTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RuntimeDiReplScopeTests>();

    public static async Task Should_keep_single_run_lifetimes()
    {
      Gen51Apps.ResetAll();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(Gen51Apps.RegisterServices)
        .AddRepl()
        .Map("check").WithHandler(Gen51Apps.Check).AsQuery().Done()
        .Map("ping").WithHandler(Gen51Apps.Ping).AsQuery().Done()
        .Build();

      int exitCode = await app.RunAsync(["check"]);

      exitCode.ShouldBe(0);
      Gen51Stats<Gen51Singleton>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Session>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Session>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Command>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Command>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Scoped>.Same.ShouldBe([true]);
      Gen51Stats<Gen51Scoped>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Transient>.Same.ShouldBe([false]);

      int singletonId = Gen51Stats<Gen51Singleton>.Seen[0];
      int scopedId = Gen51Stats<Gen51Scoped>.Seen[0];
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["check"]);

      exitCode.ShouldBe(0);
      Gen51Stats<Gen51Singleton>.Seen[1].ShouldBe(singletonId);
      Gen51Stats<Gen51Scoped>.Seen[1].ShouldBe(scopedId);
      Gen51Stats<Gen51Session>.Seen[1].ShouldNotBe(Gen51Stats<Gen51Session>.Seen[0]);
      Gen51Stats<Gen51Command>.Seen[1].ShouldNotBe(Gen51Stats<Gen51Command>.Seen[0]);
      Gen51Stats<Gen51Scoped>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
    }

    public static async Task Should_scope_repl_commands_and_resolve_mediator()
    {
      Gen51Apps.ResetAll();
      using TestTerminal terminal = new();
      terminal.QueueLine("check");
      terminal.QueueLine("ping");
      terminal.QueueLine("check");
      terminal.QueueLine("exit");
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(Gen51Apps.RegisterServices)
        .AddRepl()
        .Map("check").WithHandler(Gen51Apps.Check).AsQuery().Done()
        .Map("ping").WithHandler(Gen51Apps.Ping).AsQuery().Done()
        .Build();

      await app.RunAsync(["--interactive"]);

      Gen51Stats<Gen51Singleton>.Seen[0].ShouldBe(Gen51Stats<Gen51Singleton>.Seen[1]);
      Gen51Stats<Gen51Singleton>.AsyncDisposeCount.ShouldBe(0);
      Gen51Stats<Gen51Session>.Seen[0].ShouldBe(Gen51Stats<Gen51Session>.Seen[1]);
      Gen51Stats<Gen51Session>.DisposedAtEntry.ShouldBe([0, 0]);
      Gen51Stats<Gen51Session>.AsyncDisposeCount.ShouldBe(1);
      Gen51Stats<Gen51Command>.Seen[0].ShouldNotBe(Gen51Stats<Gen51Command>.Seen[1]);
      Gen51Stats<Gen51Command>.DisposedAtEntry[1].ShouldBeGreaterThan(0);
      Gen51Stats<Gen51Command>.Same.ShouldBe([true, true]);
      Gen51Stats<Gen51Scoped>.Seen[0].ShouldNotBe(Gen51Stats<Gen51Scoped>.Seen[1]);
      Gen51Stats<Gen51Scoped>.DisposedAtEntry[1].ShouldBeGreaterThan(0);
      Gen51Stats<Gen51Transient>.Same.ShouldBe([false, false]);
      terminal.OutputContains("pong").ShouldBeTrue();
    }
  }

  internal static class Gen51Apps
  {
    internal static void ResetAll()
    {
      Gen51Stats<Gen51Singleton>.Reset();
      Gen51Stats<Gen51Session>.Reset();
      Gen51Stats<Gen51Command>.Reset();
      Gen51Stats<Gen51Scoped>.Reset();
      Gen51Stats<Gen51Transient>.Reset();
    }

    internal static void RegisterServices(IServiceCollection services)
    {
      services.AddSingleton<IGen51Singleton, Gen51Singleton>();
      services.AddSingleton<IGen51SingletonBag, Gen51SingletonBag>();
      services.AddSessionScoped<IGen51Session, Gen51Session>();
      services.AddSessionScoped<IGen51SessionBag, Gen51SessionBag>();
      services.AddCommandScoped<IGen51Command, Gen51Command>();
      services.AddCommandScoped<IGen51CommandBag, Gen51CommandBag>();
      services.AddScoped<IGen51Scoped, Gen51Scoped>();
      services.AddScoped<IGen51ScopedBag, Gen51ScopedBag>();
      services.AddTransient<IGen51Transient, Gen51Transient>();
      services.AddTransient<IGen51TransientBag, Gen51TransientBag>();
    }

    internal static string Check
    (
      IGen51Singleton singleton,
      IGen51SingletonBag singletonBag,
      IGen51Session session,
      IGen51SessionBag sessionBag,
      IGen51Command command,
      IGen51CommandBag commandBag,
      IGen51Scoped scoped,
      IGen51ScopedBag scopedBag,
      IGen51Transient transient,
      IGen51TransientBag transientBag
    )
    {
      Note(Gen51Stats<Gen51Singleton>.Seen, Gen51Stats<Gen51Singleton>.DisposedAtEntry, Gen51Stats<Gen51Singleton>.Same, singleton.Id, Gen51Stats<Gen51Singleton>.AsyncDisposeCount, ReferenceEquals(singleton, singletonBag.Inner));
      Note(Gen51Stats<Gen51Session>.Seen, Gen51Stats<Gen51Session>.DisposedAtEntry, Gen51Stats<Gen51Session>.Same, session.Id, Gen51Stats<Gen51Session>.AsyncDisposeCount, ReferenceEquals(session, sessionBag.Inner) && ReferenceEquals(session, command.Session));
      Note(Gen51Stats<Gen51Command>.Seen, Gen51Stats<Gen51Command>.DisposedAtEntry, Gen51Stats<Gen51Command>.Same, command.Id, Gen51Stats<Gen51Command>.AsyncDisposeCount, ReferenceEquals(command, commandBag.Inner));
      Note(Gen51Stats<Gen51Scoped>.Seen, Gen51Stats<Gen51Scoped>.DisposedAtEntry, Gen51Stats<Gen51Scoped>.Same, scoped.Id, Gen51Stats<Gen51Scoped>.AsyncDisposeCount, ReferenceEquals(scoped, scopedBag.Inner));
      Note(Gen51Stats<Gen51Transient>.Seen, Gen51Stats<Gen51Transient>.DisposedAtEntry, Gen51Stats<Gen51Transient>.Same, transient.Id, Gen51Stats<Gen51Transient>.AsyncDisposeCount, ReferenceEquals(transient, transientBag.Inner));
      return "ok";
    }

    internal static Task<string> Ping(ISender sender) => sender.Send(new Gen51Ping());

    internal static void Note(List<int> seen, List<int> disposedAtEntry, List<bool> same, int id, int disposed, bool identical)
    {
      disposedAtEntry.Add(disposed);
      seen.Add(id);
      same.Add(identical);
    }
  }
}
