#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// NuruCallContext (task 508-016): calls in their own context run concurrently, each with its own terminal,
// console capture, exit code, working directory and service scope (source-generated DI and
// UseMicrosoftDependencyInjection()). [Exclusive] / .Exclusive() routes serialize; cancelling one call
// cancels only it; a plain RunAsync still sets Environment.ExitCode.

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

#pragma warning disable CA1063 // Probes only record Dispose calls.
#pragma warning disable CA1816

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.CallContext
{

[TestTag("Routing")]
[TestTag("Concurrency")]
public class CallContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CallContextTests>();

  private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

  private static NuruApp CreateApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .Map("rcc-hold {name}").WithHandler(async (string name, ITerminal terminal, CancellationToken cancellationToken) =>
      {
        await terminal.WriteLineAsync($"start {name}");
        await CcConsole.WriteLineAsync($"console {name}"); // A handler that writes to the console directly.
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
        NuruCallContext.CurrentExitCode = name == "a" ? 3 : 4;
        return $"{name}@{NuruCallContext.CurrentDirectory}|{NuruCallContext.GetFullPath("f.txt")}";
      }).AsCommand().Done()
      .Map("rcc-wait {name}").WithHandler(async (string name, CancellationToken cancellationToken) =>
      {
        try
        {
          await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
          CcRendezvous.Current.Cancelled(name);
          throw;
        }
      }).AsCommand().Done()
      .Map("rcc-excl {n:int}").WithHandler((int n) => CcTracker.Current.RunAsync(n)).AsCommand().Exclusive().Done()
      .Map<RccExclusiveCommand>()
      .Map("rcc-free {name}").WithHandler(async (string name, CancellationToken cancellationToken) =>
      {
        await CcTracker.Current.RunAsync(0);
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
      }).AsCommand().Done()
      .Build();
    return app;
  }

  private static (NuruCallContext Context, TestTerminal Terminal) NewCall(string? directory = null)
  {
    TestTerminal terminal = new();
    return (new NuruCallContext(terminal, directory) { CaptureConsole = true }, terminal);
  }

  public static async Task Should_give_concurrent_calls_their_own_output_exit_code_and_directory()
  {
    NuruApp app = CreateApp();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    string dirA = Directory.CreateTempSubdirectory("rcc-a-").FullName;
    string dirB = Directory.CreateTempSubdirectory("rcc-b-").FullName;
    string processDirectory = Environment.CurrentDirectory;
    Environment.ExitCode = 0;
    (NuruCallContext contextA, TestTerminal terminalA) = NewCall(dirA);
    (NuruCallContext contextB, TestTerminal terminalB) = NewCall(dirB);
    using (terminalA)
    using (terminalB)
    {
      Task<int> a = app.ExecuteAsync(["rcc-hold", "a"], contextA);
      Task<int> b = app.ExecuteAsync(["rcc-hold", "b"], contextB);

      // Both handlers are inside at once: the calls run concurrently.
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      CcRendezvous.Current.ReleaseAll();

      (await a.WaitAsync(Limit)).ShouldBe(3);
      (await b.WaitAsync(Limit)).ShouldBe(4);
      contextA.ExitCode.ShouldBe(3);
      contextB.ExitCode.ShouldBe(4);

      terminalA.Output.ShouldContain("start a");
      terminalA.Output.ShouldContain("console a");
      terminalA.Output.ShouldContain($"a@{dirA}|{Path.Combine(dirA, "f.txt")}");
      terminalA.Output.ShouldNotContain(" b");
      terminalB.Output.ShouldContain("start b");
      terminalB.Output.ShouldContain("console b");
      terminalB.Output.ShouldContain($"b@{dirB}|{Path.Combine(dirB, "f.txt")}");
      terminalB.Output.ShouldNotContain(" a");
    }

    // Nothing leaked into process state.
    Environment.ExitCode.ShouldBe(0);
    Environment.CurrentDirectory.ShouldBe(processDirectory);
    NuruCallContext.Current.ShouldBeNull();
    NuruCallContext.CurrentDirectory.ShouldBe(processDirectory);
  }

  public static async Task Should_carry_the_invoked_calls_exit_code_and_directory_into_render()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .Map<RccCodeQuery>()
      .Build();
    string directory = Directory.CreateTempSubdirectory("rcc-render-").FullName;
    Environment.ExitCode = 0;
    using JsonDocument arguments = JsonDocument.Parse("""{"name":"f.txt","code":9}""");
    app.TryBind("rcc-code", arguments.RootElement, out NuruBoundRequest? request, out _).ShouldBeTrue();

    (NuruCallContext context, TestTerminal terminal) = NewCall(directory);
    using (terminal)
    {
      object? result = await app.InvokeAsync(request!, context);
      context.ExitCode.ShouldBe(9);

      // Rendering as part of the call keeps the handler's exit code and the call's directory.
      using TestTerminal rendered = new();
      (await app.RenderAsync(request!, result, rendered, context)).ShouldBe(9);
      context.ExitCode.ShouldBe(9);
      rendered.Output.ShouldBe($"f.txt->{Path.Combine(directory, "f.txt")}\n");

      // A standalone render is the result's alone: it starts at 0.
      using TestTerminal standalone = new();
      (await app.RenderAsync(request!, result, standalone)).ShouldBe(0);
    }

    Environment.ExitCode.ShouldBe(0);
  }

  public static async Task Should_set_the_process_exit_code_for_a_plain_run()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("rcc-exit {code:int}").WithHandler((int code) => { NuruCallContext.CurrentExitCode = code; }).AsCommand().Done()
      .Build();
    int exitCode = await app.RunAsync(["rcc-exit", "5"]);
    int processExitCode = Environment.ExitCode;
    Environment.ExitCode = 0;

    exitCode.ShouldBe(5);
    processExitCode.ShouldBe(5);
  }

  public static async Task Should_serialize_exclusive_routes_and_run_the_others_concurrently()
  {
    NuruApp app = CreateApp();
    CcTracker.Current = new CcTracker();
    List<Task<int>> exclusive = [];
    List<TestTerminal> terminals = [];
    for (int i = 0; i < 6; i++)
    {
      (NuruCallContext context, TestTerminal terminal) = NewCall();
      terminals.Add(terminal);
      string[] args = i % 2 == 0 ? ["rcc-excl", "1"] : ["rcc-excl-attr", "1"];
      exclusive.Add(app.ExecuteAsync(args, context));
    }

    int[] exits = await Task.WhenAll(exclusive).WaitAsync(Limit);
    exits.ShouldAllBe(code => code == 0);
    // .Exclusive() and [Exclusive] take the same app gate: never two at once.
    CcTracker.Current.MaxActive.ShouldBe(1);
    CcTracker.Current.Runs.ShouldBe(6);

    CcTracker.Current = new CcTracker();
    CcRendezvous.Current = new CcRendezvous(expected: 4);
    List<Task<int>> free = [];
    for (int i = 0; i < 4; i++)
    {
      (NuruCallContext context, TestTerminal terminal) = NewCall();
      terminals.Add(terminal);
      free.Add(app.ExecuteAsync(["rcc-free", $"f{i}"], context));
    }

    await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
    CcRendezvous.Current.ReleaseAll();
    (await Task.WhenAll(free).WaitAsync(Limit)).ShouldAllBe(code => code == 0);
    foreach (TestTerminal terminal in terminals)
      terminal.Dispose();
  }

  public static async Task Should_cancel_only_the_cancelled_call()
  {
    NuruApp app = CreateApp();
    CcRendezvous.Current = new CcRendezvous(expected: 1);
    (NuruCallContext contextA, TestTerminal terminalA) = NewCall();
    (NuruCallContext contextB, TestTerminal terminalB) = NewCall();
    using (terminalA)
    using (terminalB)
    using (CancellationTokenSource cancelA = new())
    {
      Task<int> a = app.ExecuteAsync(["rcc-wait", "a"], contextA, cancelA.Token);
      Task<int> b = app.ExecuteAsync(["rcc-hold", "b"], contextB);
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);

      await cancelA.CancelAsync();
      await Should.ThrowAsync<OperationCanceledException>(() => a.WaitAsync(Limit));
      CcRendezvous.Current.CancelledNames.ShouldBe(["a"]);
      b.IsCompleted.ShouldBeFalse();

      CcRendezvous.Current.ReleaseAll();
      (await b.WaitAsync(Limit)).ShouldBe(4);
    }
  }

  public static async Task Should_give_each_concurrent_call_its_own_service_scope()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .ConfigureServices(services =>
      {
        services.AddScoped<IRccScoped, RccScoped>();
        services.AddCommandScoped<IRccCommand, RccCommand>();
        services.AddSessionScoped<IRccSession, RccSession>();
      })
      .Map("rcc-scope {name}").WithHandler(async (string name, IRccScoped scoped, IRccCommand command, IRccSession session, CancellationToken cancellationToken) =>
      {
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
        return $"{scoped.Id}:{command.Id}:{session.Id}:{scoped.Disposed}{command.Disposed}{session.Disposed}";
      }).AsQuery().Done()
      .Build();

    await AssertScopesAsync(app);
  }

  public static async Task Should_give_each_concurrent_call_its_own_service_scope_with_runtime_di()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseMicrosoftDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddScoped<IRccScoped, RccScoped>();
        services.AddCommandScoped<IRccCommand, RccCommand>();
        services.AddSessionScoped<IRccSession, RccSession>();
      })
      .Map("rcc-scope {name}").WithHandler(async (string name, IRccScoped scoped, IRccCommand command, IRccSession session, CancellationToken cancellationToken) =>
      {
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
        return $"{scoped.Id}:{command.Id}:{session.Id}:{scoped.Disposed}{command.Disposed}{session.Disposed}";
      }).AsQuery().Done()
      .Build();

    await AssertScopesAsync(app);
  }

  // Call a finishes (and disposes its scope) while call b still uses its own instances.
  private static async Task AssertScopesAsync(NuruApp app)
  {
    RccProbe.Reset();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    (NuruCallContext contextA, TestTerminal terminalA) = NewCall();
    (NuruCallContext contextB, TestTerminal terminalB) = NewCall();
    using (terminalA)
    using (terminalB)
    {
      Task<int> a = app.ExecuteAsync(["rcc-scope", "a"], contextA);
      Task<int> b = app.ExecuteAsync(["rcc-scope", "b"], contextB);
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);

      CcRendezvous.Current.Release("a");
      (await a.WaitAsync(Limit)).ShouldBe(0, terminalA.ErrorOutput);
      CcRendezvous.Current.Release("b");
      (await b.WaitAsync(Limit)).ShouldBe(0, terminalB.ErrorOutput);

      string[] partsA = terminalA.Output.Trim().Split(':');
      string[] partsB = terminalB.Output.Trim().Split(':');
      partsA[0].ShouldNotBe(partsB[0]);
      partsA[1].ShouldNotBe(partsB[1]);
      partsA[2].ShouldNotBe(partsB[2]);
      partsA[3].ShouldBe("FalseFalseFalse");
      partsB[3].ShouldBe("FalseFalseFalse");

      // Each call disposed its own scope when it returned.
      RccProbe.Created.Count.ShouldBe(6);
      RccProbe.Created.ShouldAllBe(probe => probe.Disposed);
    }
  }
}

[NuruRoute("rcc-excl-attr")]
[Exclusive]
public sealed class RccExclusiveCommand : ICommand<Unit>
{
  [Parameter]
  public int N { get; set; }

  public sealed class Handler : ICommandHandler<RccExclusiveCommand, Unit>
  {
    public async Task<Unit> Handle(RccExclusiveCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await CcTracker.Current.RunAsync(command.N);
      return Unit.Value;
    }
  }
}

[NuruRoute("rcc-code", Description = "Set the exit code, return a path result")]
public sealed class RccCodeQuery : IQuery<CcPathResult>
{
  [Option("name", Description = "The name")]
  public string Name { get; set; } = "";

  [Option("code", Description = "The exit code")]
  public int Code { get; set; }

  public sealed class Handler : IQueryHandler<RccCodeQuery, CcPathResult>
  {
    public async Task<CcPathResult> Handle(RccCodeQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      await Task.Yield();
      NuruCallContext.CurrentExitCode = query.Code;
      return new CcPathResult(query.Name);
    }
  }
}

}

// Global: handler lambdas are copied into generated code, which sees only file-level usings.
public interface IRccProbe
{
  int Id { get; }

  bool Disposed { get; }
}

public interface IRccScoped : IRccProbe;

public interface IRccCommand : IRccProbe;

public interface IRccSession : IRccProbe;

public abstract class RccProbe : IRccProbe, IDisposable
{
  private static int NextId;

  public static ConcurrentBag<RccProbe> Created { get; private set; } = [];

  protected RccProbe()
  {
    Id = Interlocked.Increment(ref NextId);
    Created.Add(this);
  }

  public int Id { get; }

  public bool Disposed { get; private set; }

  public static void Reset() => Created = [];

  public void Dispose()
  {
    Disposed = true;
    GC.SuppressFinalize(this);
  }
}

public sealed class RccScoped : RccProbe, IRccScoped;

public sealed class RccCommand : RccProbe, IRccCommand;

public sealed class RccSession : RccProbe, IRccSession;
