#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// TimeWarp.Nuru.Daemon (task 508-005): the server over a real Unix domain socket, in this process.
// Ping, argv runs, binder invokes, exit codes and stderr, cancellation on disconnect, stop, idle
// shutdown, one server per socket, and the forwarder declining for the app it serves.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.Server
{
using System.Text.Json;
using TimeWarp.Nuru.Daemon;

[TestTag("Daemon")]
public class DaemonServerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonServerTests>();

  // Default terminal (no UseTerminal): the server captures what routes write through it.
  private static NuruApp CreateServedApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("dm-app")
      .Map("dm-add {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery().Done()
#pragma warning disable RS0030 // The handler writes to stderr directly, as a CLI handler may.
      .Map("dm-fail {code:int}").WithHandler((int code) =>
      {
        Console.Error.WriteLine("dm failing");
        Environment.ExitCode = code;
      }).AsCommand().Done()
#pragma warning restore RS0030
      .Map("dm-wait").WithHandler(async (CancellationToken cancellationToken) =>
      {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return "never";
      }).AsCommand().Done()
      .Map("dm-cwd").WithHandler(() => Environment.CurrentDirectory).AsQuery().Done()
      .Build();
    return app;
  }

  private static NuruDaemonOptions CreateOptions(TimeSpan? idleTimeout = null) => new()
  {
    SocketPath = Path.Combine(Path.GetTempPath(), $"nuru-dm-{Guid.NewGuid():N}"[..20] + ".sock"),
    IdleTimeout = idleTimeout ?? TimeSpan.Zero
  };

  private static async Task<(Task<int> Server, CancellationTokenSource Stop)> StartAsync(NuruApp app, NuruDaemonOptions options)
  {
    CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(app, options, stop.Token));
    NuruDaemonClient client = new(options);
    for (int i = 0; i < 100 && await client.PingAsync(app) is null; i++)
      await Task.Delay(50);
    return (server, stop);
  }

  private static async Task StopAsync(Task<int> server, CancellationTokenSource stop)
  {
    await stop.CancelAsync();
    (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    stop.Dispose();
  }

  public static async Task Should_report_itself_on_ping()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonStatus? status = await new NuruDaemonClient(options).PingAsync(app);
      status.ShouldNotBeNull();
      status.Name.ShouldBe("dm-app");
      status.ProcessId.ShouldBe(Environment.ProcessId);
      status.BuildId.ShouldBe(NuruDaemonPaths.GetBuildId(app));
      status.SocketPath.ShouldBe(Path.GetFullPath(options.SocketPath!));
      NuruDaemonServer.IsServing(app).ShouldBeTrue();
    }
    finally
    {
      await StopAsync(server, stop);
    }

    NuruDaemonServer.IsServing(app).ShouldBeFalse();
    File.Exists(options.SocketPath).ShouldBeFalse();
  }

  public static async Task Should_run_argv_and_return_output_and_exit_code()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      NuruForwardResult add = await client.RunAsync(app, ["dm-add", "2", "3"]);
      add.ExitCode.ShouldBe(0);
      add.Output.TrimEnd().ShouldBe("5");
      add.ErrorOutput.ShouldBeEmpty();

      NuruForwardResult fail = await client.RunAsync(app, ["dm-fail", "3"]);
      fail.ExitCode.ShouldBe(3);
      fail.ErrorOutput.ShouldContain("dm failing");

      // A parse failure on the server is the CLI's own message and exit code.
      NuruForwardResult unknown = await client.RunAsync(app, ["dm-nope"]);
      unknown.ExitCode.ShouldBe(1);
      unknown.ErrorOutput.ShouldContain("Unknown command");

      // The client's directory travels with the call.
      NuruForwardResult cwd = await client.RunAsync(app, ["dm-cwd"]);
      cwd.Output.TrimEnd().ShouldBe(Environment.CurrentDirectory);
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_bind_invoke_and_render_json_arguments()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      using JsonDocument good = JsonDocument.Parse("""{"x":4,"y":5}""");
      NuruForwardResult add = await client.InvokeAsync(app, "dm-add {x:int} {y:int}", good.RootElement);
      add.ExitCode.ShouldBe(0);
      add.RenderedOutput.TrimEnd().ShouldBe("9");
      add.Output.ShouldBeEmpty();

      // A bind error is exit 1 with exactly the message TryBind (and --json-args) gives.
      using JsonDocument bad = JsonDocument.Parse("""{"x":4,"y":5,"z":6}""");
      app.TryBind("dm-add {x:int} {y:int}", bad.RootElement, out _, out NuruBindError? expected).ShouldBeFalse();
      NuruForwardResult unknownKey = await client.InvokeAsync(app, "dm-add {x:int} {y:int}", bad.RootElement);
      unknownKey.ExitCode.ShouldBe(1);
      unknownKey.ErrorOutput.TrimEnd().ShouldBe(expected!.Message);
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_cancel_the_call_when_the_client_disconnects()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      using (CancellationTokenSource giveUp = new(TimeSpan.FromMilliseconds(500)))
      {
        await Should.ThrowAsync<OperationCanceledException>(() => client.RunAsync(app, ["dm-wait"], giveUp.Token));
      }

      // The abandoned handler was cancelled and released the call gate, so the next call runs.
      using CancellationTokenSource limit = new(TimeSpan.FromSeconds(10));
      NuruForwardResult add = await client.RunAsync(app, ["dm-add", "1", "1"], limit.Token);
      add.Output.TrimEnd().ShouldBe("2");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_stop_on_request_and_remove_the_socket()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    using (stop)
    {
      NuruDaemonClient client = new(options);
      (await client.StopAsync(app)).ShouldBeTrue();
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
      File.Exists(options.SocketPath).ShouldBeFalse();
      (await client.PingAsync(app)).ShouldBeNull();
      (await client.StopAsync(app)).ShouldBeFalse();
      await Should.ThrowAsync<NuruDaemonException>(() => client.RunAsync(app, ["dm-add", "1", "1"]));
    }
  }

  public static async Task Should_stop_after_the_idle_timeout()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions(TimeSpan.FromSeconds(1));
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    using (stop)
    {
      (await new NuruDaemonClient(options).RunAsync(app, ["dm-add", "1", "2"])).ExitCode.ShouldBe(0);
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
      File.Exists(options.SocketPath).ShouldBeFalse();
    }
  }

  public static async Task Should_refuse_a_second_server_on_the_same_socket()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      using TestTerminal terminal = new();
      NuruApp second = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .WithName("dm-app")
        .Map("dm-other").WithHandler(() => "other").AsQuery().Done()
        .Build();
      (await NuruDaemonServer.RunAsync(second, options)).ShouldBe(1);
      terminal.ErrorOutput.ShouldContain("already running");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_run_locally_when_forwarding_is_not_possible()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    NuruDaemonClient client = new(options);
    NuruForwardRequest request = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Auto);

    // No server and no auto-start: the caller runs the route.
    (await client.ForwardAsync(app, request, CancellationToken.None)).ShouldBeNull();

    // --no-daemon never forwards.
    NuruForwardRequest never = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Never);
    (await client.ForwardAsync(app, never, CancellationToken.None)).ShouldBeNull();

    // The app being served runs its routes itself, even with --daemon.
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruForwardRequest require = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Require);
      (await client.ForwardAsync(app, require, CancellationToken.None)).ShouldBeNull();

      // Another instance of the same build forwards to it.
      NuruApp other = CreateServedApp();
      NuruForwardResult? forwarded = await client.ForwardAsync(other, request, CancellationToken.None);
      forwarded.ShouldNotBeNull();
      forwarded.Output.TrimEnd().ShouldBe("3");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_inline_resolved_json_args_when_forwarding()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      // The CLI read '-' from its own stdin; the server gets the object text, never '-'.
      NuruApp client = CreateServedApp();
      NuruForwardRequest request = NuruForwardRequest.ForArgs(
        "dm-add {x:int} {y:int}", ["dm-add", "--json-args", "-"], """{"x":20,"y":22}""", NuruForwardMode.Auto);
      NuruForwardResult? forwarded = await new NuruDaemonClient(options).ForwardAsync(client, request, CancellationToken.None);
      forwarded.ShouldNotBeNull();
      forwarded.ExitCode.ShouldBe(0, forwarded.ErrorOutput);
      forwarded.Output.TrimEnd().ShouldBe("42");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_default_to_a_per_user_socket_named_for_the_app()
  {
    string path = NuruDaemonPaths.GetDefaultSocketPath("my app/name");
    Path.GetFileName(path).ShouldBe("my_app_name.sock");
    path.ShouldStartWith(NuruDaemonPaths.GetDefaultSocketDirectory());
    path.Length.ShouldBeLessThanOrEqualTo(100);

    string longPath = NuruDaemonPaths.GetDefaultSocketPath(new string('x', 200));
    longPath.Length.ShouldBeLessThanOrEqualTo(100);
    longPath.ShouldEndWith(".sock");
    await Task.CompletedTask;
  }
}
}
