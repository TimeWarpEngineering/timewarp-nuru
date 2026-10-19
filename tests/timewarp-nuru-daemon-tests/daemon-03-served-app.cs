#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

// TimeWarp.Nuru.Daemon (task 508-005): the served-app sample as real processes. --daemon starts a
// detached server; later calls forward to it (same pid, the caller's directory, stderr and exit code,
// --json-args from stdin); local-only routes and --no-daemon run in the CLI process; `served-app mcp`
// forwards tools/call to the same server; serve --status / --stop manage it.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.ServedApp
{
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TimeWarp.Amuru;
using TimeWarp.Nuru.Daemon;

[TestTag("Daemon")]
public class DaemonServedAppTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonServedAppTests>();

  public static async Task Should_forward_cli_and_mcp_calls_to_one_server()
  {
    string socket = Path.Combine(Path.GetTempPath(), $"nuru-sa-{Guid.NewGuid():N}"[..20] + ".sock");
    string workDirectory = Directory.CreateTempSubdirectory("nuru-sa-").FullName;

    // Builds the sample once; later calls use --no-build.
    CommandOutput version = await RunAsync(socket, build: true, args: ["--version"]);
    version.ExitCode.ShouldBe(0, version.Stderr);

    try
    {
      (await RunAsync(socket, args: ["serve", "--status"])).ExitCode.ShouldBe(1);

      // No server: the call runs in the CLI process.
      int localPid = ParsePid(await RunAsync(socket, args: ["pid"]));

      // --daemon starts the server and forwards.
      int serverPid = ParsePid(await RunAsync(socket, args: ["--daemon", "pid"]));
      serverPid.ShouldNotBe(localPid);

      CommandOutput status = await RunAsync(socket, args: ["serve", "--status"]);
      status.ExitCode.ShouldBe(0, status.Stderr);
      status.Stdout.ShouldContain($"pid {serverPid.ToString(CultureInfo.InvariantCulture)}");

      // Later calls forward to the same server without --daemon.
      ParsePid(await RunAsync(socket, args: ["pid"])).ShouldBe(serverPid);
      CommandOutput count = await RunAsync(socket, args: ["count", "hello"]);
      count.Stdout.Trim().ShouldBe("5");

      // Local-only routes and --no-daemon run in the CLI process.
      ParsePid(await RunAsync(socket, args: ["local-pid"])).ShouldNotBe(serverPid);
      (await RunAsync(socket, args: ["local-echo", "hi"])).Stdout.ShouldNotContain($"pid {serverPid.ToString(CultureInfo.InvariantCulture)}");
      ParsePid(await RunAsync(socket, args: ["--no-daemon", "pid"])).ShouldNotBe(serverPid);

      // The caller's directory travels with the call.
      CommandOutput cwd = await RunAsync(socket, workingDirectory: workDirectory, args: ["cwd"]);
      Path.GetFullPath(cwd.Stdout.Trim()).ShouldBe(Path.GetFullPath(workDirectory));

      // stderr and the exit code come back from the server.
      CommandOutput fail = await RunAsync(socket, args: ["fail", "3"]);
      fail.ExitCode.ShouldBe(3);
      fail.Stderr.ShouldContain("failing with 3");

      // --json-args - is read by the CLI, then forwarded as the object.
      CommandOutput json = await RunAsync(socket, stdin: """{"word":"abcdefg"}""", args: ["count", "--json-args", "-"]);
      json.ExitCode.ShouldBe(0, json.Stderr);
      json.Stdout.Trim().ShouldBe("7");

      // The MCP host forwards tools/call to the same server.
      await using (McpClient client = await StartMcpAsync(socket))
      {
        CallToolResult pid = await client.CallToolAsync(Call("pid", "{}"));
        pid.IsError.ShouldBe(false);
        Text(pid).ShouldBe(serverPid.ToString(CultureInfo.InvariantCulture));

        CallToolResult localOnly = await client.CallToolAsync(Call("local-pid", "{}"));
        Text(localOnly).ShouldNotBe(serverPid.ToString(CultureInfo.InvariantCulture));

        CallToolResult wrongType = await client.CallToolAsync(Call("count", """{"word":7}"""));
        wrongType.IsError.ShouldBe(true);
        Text(wrongType).ShouldContain("wrong JSON type");
      }

      CommandOutput stop = await RunAsync(socket, args: ["serve", "--stop"]);
      stop.Stdout.ShouldContain("server stopped");
      (await RunAsync(socket, args: ["serve", "--status"])).ExitCode.ShouldBe(1);
      File.Exists(socket).ShouldBeFalse();
    }
    finally
    {
      await RunAsync(socket, args: ["serve", "--stop"]);
      Directory.Delete(workDirectory, recursive: true);
    }
  }

  private static int ParsePid(CommandOutput output)
  {
    output.ExitCode.ShouldBe(0, output.Stderr);
    return int.Parse(output.Stdout.Trim(), CultureInfo.InvariantCulture);
  }

  private static Task<CommandOutput> RunAsync(string socket, string[] args, bool build = false, string? workingDirectory = null, string? stdin = null)
  {
    string[] dotnetArgs = build
      ? ["run", SamplePath(), "--", .. args]
      : ["run", "--no-build", SamplePath(), "--", .. args];
    ShellBuilder builder = Shell.Builder("dotnet")
      .WithArguments(dotnetArgs)
      .WithEnvironmentVariable(NuruDaemonOptions.SocketPathEnvironmentVariable, socket)
      .WithTimeout(TimeSpan.FromMinutes(3))
      .WithNoValidation();
    if (workingDirectory is not null)
      builder = builder.WithWorkingDirectory(workingDirectory);
    if (stdin is not null)
      builder = builder.WithStandardInput(stdin);
    return builder.CaptureAsync();
  }

  private static async Task<McpClient> StartMcpAsync(string socket)
  {
    StdioClientTransport transport = new(new StdioClientTransportOptions
    {
      Name = "served-app",
      Command = "dotnet",
      Arguments = ["run", "--no-build", SamplePath(), "--", "mcp"],
      EnvironmentVariables = new Dictionary<string, string?> { [NuruDaemonOptions.SocketPathEnvironmentVariable] = socket },
      ShutdownTimeout = TimeSpan.FromSeconds(10)
    });
    using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
    return await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
  }

  private static CallToolRequestParams Call(string name, string argumentsJson)
  {
    using JsonDocument document = JsonDocument.Parse(argumentsJson);
    return new CallToolRequestParams
    {
      Name = name,
      Arguments = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
    };
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

  private static string SamplePath([CallerFilePath] string thisFile = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "samples", "endpoints", "16-daemon", "served-app.cs"));
}
}
