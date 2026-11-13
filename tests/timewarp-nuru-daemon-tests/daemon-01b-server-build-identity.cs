#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

#region Purpose
// Pins the daemon build identity and socket handling (task 508-005, 508-017): lock, another build, status, and socket path safety.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.ServerBuildIdentity
{
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TimeWarp.Nuru.Daemon;

[TestTag("Daemon")]
public class DaemonServerBuildIdentityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonServerBuildIdentityTests>();

  // Default terminal (no UseTerminal): the server captures what routes write through it.
  private static NuruApp CreateServedApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("dm-app")
      .Map("dm-add {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery().Done()
      .Map("dm-wait").WithHandler(async (CancellationToken cancellationToken) =>
      {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return "never";
      }).AsCommand().Done()
      .Map("dm-cwd").WithHandler(() => NuruCallContext.CurrentDirectory).AsQuery().Done()
      .Map("dm-hold").WithHandler(async (CancellationToken cancellationToken) =>
      {
        await CcRendezvous.Current.EnterAsync("hold", cancellationToken);
        return "held";
      }).AsCommand().Done()
      .Build();
    return app;
  }

  // Same name, so the same build as CreateServedApp; writes through the given terminal.
  private static NuruApp CreateTerminalApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("dm-app")
      .Map("dm-other").WithHandler(() => "other").AsQuery().Done()
      .Build();
    return app;
  }

  // Another name is another build id.
  private static NuruApp CreateOtherBuildApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("dm-other-build")
      .Map("dm-add {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery().Done()
      .Build();
    return app;
  }

  // CreateTempSubdirectory makes a 0700 directory, which the server requires for its socket.
  private static NuruDaemonOptions CreateOptions(TimeSpan? idleTimeout = null) => new()
  {
    SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-dm-").FullName, "dm.sock"),
    IdleTimeout = idleTimeout ?? TimeSpan.Zero
  };

  private static async Task<string> SendRawAsync(string socketPath, string line)
  {
    using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
    await using NetworkStream stream = new(socket);
    await stream.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"));
    using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
    return await reader.ReadLineAsync() ?? "";
  }

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

  public static async Task Should_not_touch_the_socket_while_another_server_holds_the_lock()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    options.StartTimeout = TimeSpan.FromMilliseconds(300);
    string socketPath = options.SocketPath!;
    await File.WriteAllTextAsync(socketPath, "not a socket");

    // A live server holds the lock but did not answer the ping: the socket must stay as it is.
    using TestTerminal terminal = new();
    NuruApp second = CreateTerminalApp(terminal);
    using (new FileStream(socketPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
    {
      (await NuruDaemonServer.RunAsync(second, options)).ShouldBe(1);
      terminal.ErrorOutput.ShouldContain("holds");
      (await File.ReadAllTextAsync(socketPath)).ShouldBe("not a socket");
    }

    // Lock released (the holder died): the stale file is replaced by this server's socket.
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      (await new NuruDaemonClient(options).PingAsync(app)).ShouldNotBeNull();
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_refuse_a_call_that_names_another_build()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      string socketPath = options.SocketPath!;
      string other = await SendRawAsync(socketPath, """{"v":1,"op":"run","args":["dm-add","1","2"],"cwd":"/","build":"another-build"}""");
      other.ShouldContain("\"ok\":false");
      other.ShouldContain("different build");

      string build = JsonEncodedText.Encode(NuruDaemonPaths.GetBuildId(app)).ToString();
      string same = await SendRawAsync(socketPath, $$"""{"v":1,"op":"run","args":["dm-add","1","2"],"cwd":"/","build":"{{build}}"}""");
      same.ShouldContain("\"exitCode\":0");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_leave_a_server_from_another_build_running()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruApp otherBuild = CreateOtherBuildApp();
      NuruForwardRequest request = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Auto);
      (await new NuruDaemonClient(options).ForwardAsync(otherBuild, request, CancellationToken.None)).ShouldBeNull();
      (await new NuruDaemonClient(options).PingAsync(app)).ShouldNotBeNull();
      server.IsCompleted.ShouldBeFalse();
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_not_cancel_a_call_in_flight_when_another_build_calls()
  {
    CcRendezvous.Current = new(1);
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      Task<NuruForwardResult> held = client.RunAsync(app, ["dm-hold"]);
      await CcRendezvous.Current.AllEntered.WaitAsync(TimeSpan.FromSeconds(10));

      // Another build on the same socket: Auto runs locally, --daemon fails with both builds named.
      NuruApp otherBuild = CreateOtherBuildApp();
      NuruDaemonClient otherClient = new(options);
      NuruForwardRequest auto = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Auto);
      (await otherClient.ForwardAsync(otherBuild, auto, CancellationToken.None)).ShouldBeNull();
      NuruForwardRequest require = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Require);
      NuruForwardResult? refused = await otherClient.ForwardAsync(otherBuild, require, CancellationToken.None);
      refused.ShouldNotBeNull();
      refused.ExitCode.ShouldBe(1);
      refused.ErrorOutput.ShouldContain("is another build (version");
      refused.ErrorOutput.ShouldContain("; this is version");
      refused.ErrorOutput.ShouldContain("serve --stop");
      refused.ErrorOutput.TrimEnd('\n').ShouldNotContain("\n");

      held.IsCompleted.ShouldBeFalse();
      server.IsCompleted.ShouldBeFalse();
      CcRendezvous.Current.ReleaseAll();
      NuruForwardResult result = await held.WaitAsync(TimeSpan.FromSeconds(10));
      result.ExitCode.ShouldBe(0);
      result.Output.ShouldContain("held");
      (await client.PingAsync(app)).ShouldNotBeNull();
    }
    finally
    {
      CcRendezvous.Current.ReleaseAll();
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_list_servers_of_other_builds_in_status()
  {
    NuruApp app = CreateServedApp();
    if (Environment.GetEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable) is not null)
      return; // The listing is for the default socket only.

    // A server on another build's default socket name (the same app stands in for that build).
    NuruDaemonOptions options = new()
    {
      SocketPath = NuruDaemonPaths.GetDefaultSocketPath("dm-app", "dm-another-build"),
      IdleTimeout = TimeSpan.Zero
    };
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      using TestTerminal terminal = new();
      (await NuruDaemonServer.StatusAsync(CreateTerminalApp(terminal), new NuruDaemonOptions())).ShouldBe(1);
      terminal.Output.ShouldContain("dm-app server is not running");
      terminal.Output.ShouldContain($"Also serving dm-app: pid {Environment.ProcessId}, version");
      terminal.Output.ShouldContain($"socket {options.SocketPath}.");
      terminal.Output.ShouldContain($"{NuruDaemonOptions.SocketPathEnvironmentVariable}={options.SocketPath}");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_name_the_build_in_status()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      using TestTerminal terminal = new();
      (await NuruDaemonServer.StatusAsync(CreateTerminalApp(terminal), options)).ShouldBe(0);
      NuruDaemonStatus? status = await new NuruDaemonClient(options).PingAsync(app);
      status.ShouldNotBeNull();
      terminal.Output.ShouldContain($"dm-app server is running: pid {Environment.ProcessId}, {status.DescribeBuild()}, socket {status.SocketPath}.");
      terminal.Output.ShouldNotContain("Also serving"); // A fixed socket lists nothing else.
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_resolve_a_relative_socket_path_under_the_socket_directory()
  {
    NuruApp app = CreateServedApp();
    NuruDaemonOptions options = new() { SocketPath = "dm-relative.sock" };
    string expected = Path.Combine(NuruDaemonPaths.GetDefaultSocketDirectory(), "dm-relative.sock");
    options.ResolveSocketPath(app).ShouldBe(expected);

    string previous = Environment.CurrentDirectory;
    Environment.CurrentDirectory = Path.GetTempPath();
    try
    {
      options.ResolveSocketPath(app).ShouldBe(expected);
    }
    finally
    {
      Environment.CurrentDirectory = previous;
    }

    await Task.CompletedTask;
  }

  public static async Task Should_require_an_owner_only_socket_directory()
  {
    if (OperatingSystem.IsWindows())
      return; // The per-user profile directory is used there; nothing to check.

    NuruApp app = CreateServedApp();
    string root = Directory.CreateTempSubdirectory("nuru-dm-").FullName;

    // A missing directory is created 0700.
    NuruDaemonOptions fresh = new() { SocketPath = Path.Combine(root, "new", "dm.sock") };
    NuruDaemonPaths.EnsureSocketDirectory(fresh.SocketPath);
    File.GetUnixFileMode(Path.Combine(root, "new")).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

    // A directory others can use, or a symlink, is refused by the server and by the client.
    string shared = Path.Combine(root, "shared");
    Directory.CreateDirectory(shared);
    File.SetUnixFileMode(shared, (UnixFileMode)0b111_111_111);
    string link = Path.Combine(root, "link");
    Directory.CreateSymbolicLink(link, Path.Combine(root, "new"));
    foreach (string directory in new[] { shared, link })
    {
      NuruDaemonOptions options = new() { SocketPath = Path.Combine(directory, "dm.sock"), IdleTimeout = TimeSpan.Zero };
      NuruDaemonPaths.CheckSocketDirectory(options.SocketPath).ShouldNotBeNull();

      // Built inside the loop body, which once got no catalog (task 508-008).
      using TestTerminal terminal = new();
      NuruApp refusedApp = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .WithName("dm-app")
        .Map("dm-other").WithHandler(() => "other").AsQuery().Done()
        .Build();
      (await NuruDaemonServer.RunAsync(refusedApp, options)).ShouldBe(1);
      terminal.ErrorOutput.ShouldContain("socket directory");
      File.Exists(options.SocketPath).ShouldBeFalse();

      NuruForwardRequest require = NuruForwardRequest.ForArgs("dm-add {x:int} {y:int}", ["dm-add", "1", "2"], null, NuruForwardMode.Require);
      NuruForwardResult? refused = await new NuruDaemonClient(options).ForwardAsync(app, require, CancellationToken.None);
      refused.ShouldNotBeNull();
      refused.ExitCode.ShouldBe(1);
      refused.ErrorOutput.ShouldContain("socket directory");
    }

    Directory.Delete(root, recursive: true);
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

  public static async Task Should_default_to_one_socket_per_build()
  {
    string a = NuruDaemonPaths.GetDefaultSocketPath("my app/name", "build-a");
    Path.GetFileName(a).ShouldMatch("^my_app_name-[0-9a-f]{8}\\.sock$");
    NuruDaemonPaths.GetDefaultSocketPath("my app/name", "build-a").ShouldBe(a);
    NuruDaemonPaths.GetDefaultSocketPath("my app/name", "build-b").ShouldNotBe(a);

    string longPath = NuruDaemonPaths.GetDefaultSocketPath(new string('x', 200), "build-a");
    longPath.Length.ShouldBeLessThanOrEqualTo(100);
    Path.GetFileName(longPath).ShouldMatch("^[0-9a-f]{16}-[0-9a-f]{8}\\.sock$");

    if (Environment.GetEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable) is null)
    {
      NuruApp app = CreateServedApp();
      new NuruDaemonOptions().ResolveSocketPath(app).ShouldBe(NuruDaemonPaths.GetDefaultSocketPath("dm-app", NuruDaemonPaths.GetBuildId(app)));
    }

    await Task.CompletedTask;
  }
}
}
