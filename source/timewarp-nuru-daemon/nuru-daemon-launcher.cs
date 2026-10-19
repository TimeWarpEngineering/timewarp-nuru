#region Purpose
// Start a served app's own serve route in the background and wait until it answers.
#endregion

#region Design
// The server must outlive the client and must not hold the client's standard streams: a shell that runs
// `$(app cmd)` waits for every writer of the pipe to close. So stdio is redirected (the server writes
// nothing when NURU_DAEMON_DETACHED=1), and on Linux setsid puts the server in its own session so a
// Ctrl+C in the client's terminal does not reach it. TimeWarp.Amuru's Shell.Builder awaits the process
// and owns its streams, which is the opposite of a detached daemon, so this type uses ProcessStartInfo
// directly (RS0030 suppressed here only).
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>Starts a server for a served app.</summary>
internal static class NuruDaemonLauncher
{
  private const string SetsidPath = "/usr/bin/setsid";

  /// <summary>
  /// Starts <c>&lt;app&gt; serve</c> detached on <paramref name="socketPath"/> and waits for a server of
  /// build <paramref name="buildId"/> to answer.
  /// </summary>
  /// <returns>Null when a server of this build answers; otherwise the error message.</returns>
  public static async Task<string?> StartAsync(NuruApp app, NuruDaemonOptions options, string socketPath, string buildId, CancellationToken cancellationToken)
  {
    string name = app.Capabilities.Name;
    string? processPath = Environment.ProcessPath;
    if (processPath is null)
      return $"Error: cannot find this program's executable to start the {name} server.";

    List<string> arguments = [];
    string fileName = processPath;
    if (NuruDaemonPaths.IsDotNetHost(processPath))
    {
      string? entry = NuruDaemonPaths.GetEntryPath();
      if (entry is null || string.Equals(entry, processPath, StringComparison.Ordinal))
        return $"Error: cannot find this program's entry assembly to start the {name} server.";
      arguments.Add(entry);
    }

    arguments.AddRange(options.ServeArguments);
    if (OperatingSystem.IsLinux() && File.Exists(SetsidPath))
    {
      arguments.Insert(0, fileName);
      fileName = SetsidPath;
    }

    using System.Diagnostics.Process? process = Start(fileName, arguments, socketPath);
    if (process is null)
      return $"Error: could not start the {name} server ({fileName}).";

    long deadline = Environment.TickCount64 + (long)options.StartTimeout.TotalMilliseconds;
    NuruDaemonStatus? other = null;
    while (Environment.TickCount64 < deadline)
    {
      NuruDaemonStatus? status = await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
      if (status?.BuildId == buildId)
        return null;
      other = status ?? other;

      if (process.HasExited)
      {
        // Another client may have started one at the same time; this one then exits with "already running".
        status = await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
        if (status?.BuildId == buildId)
          return null;
        if (status is not null)
          return DifferentBuild(name, socketPath, status);
        return $"Error: the {name} server exited with code {process.ExitCode.ToString(CultureInfo.InvariantCulture)} before it answered on {socketPath}.";
      }

      await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }

    return other is not null
      ? DifferentBuild(name, socketPath, other)
      : $"Error: the {name} server did not answer on {socketPath} within {options.StartTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s.";
  }

  private static string DifferentBuild(string name, string socketPath, NuruDaemonStatus status) =>
    $"Error: a {name} server from a different build (pid {status.ProcessId.ToString(CultureInfo.InvariantCulture)}) answers on {socketPath}; stop it with 'serve --stop'.";

#pragma warning disable RS0030 // A detached daemon needs ProcessStartInfo; see Design.
  private static System.Diagnostics.Process? Start(string fileName, List<string> arguments, string socketPath)
  {
    System.Diagnostics.ProcessStartInfo startInfo = new(fileName)
    {
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      WorkingDirectory = AppContext.BaseDirectory
    };
    foreach (string argument in arguments)
      startInfo.ArgumentList.Add(argument);
    // The child listens exactly where this client probes, whatever its options or directory resolve to.
    startInfo.Environment[NuruDaemonServer.LaunchSocketEnvironmentVariable] = socketPath;
    startInfo.Environment[NuruDaemonServer.DetachedEnvironmentVariable] = "1";

    System.Diagnostics.Process? process = System.Diagnostics.Process.Start(startInfo);
    process?.StandardInput.Close();
    return process;
  }
#pragma warning restore RS0030
}
