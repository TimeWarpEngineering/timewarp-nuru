#region Purpose
// Where a served app's socket lives, and which build of the app a running server is.
#endregion

#region Design
// Per-user, never a TCP port: $XDG_RUNTIME_DIR/nuru on Linux (tmpfs, mode 0700, owned by the user),
// otherwise a 0700 directory under the temp path named for the user; %LOCALAPPDATA% on Windows (AF_UNIX
// is supported on Windows 10 1803+). Socket paths are limited to about 104 bytes (macOS sun_path), so a
// long name falls back to a hash. The build id lets a client refuse a server started from another build
// of the same app: same name and version, same entry file, same write time.
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// Default socket locations and build identity for served Nuru apps.
/// </summary>
public static class NuruDaemonPaths
{
  // macOS sun_path is 104 bytes including the terminator; Linux allows 108.
  private const int MaxSocketPathLength = 100;

  /// <summary>
  /// The per-user default socket path for an app named <paramref name="appName"/>.
  /// </summary>
  /// <param name="appName">The app name from the capabilities catalog.</param>
  /// <returns>An absolute path in a directory only the current user can use.</returns>
  public static string GetDefaultSocketPath(string? appName)
  {
    string directory = GetDefaultSocketDirectory();
    string name = Sanitize(string.IsNullOrWhiteSpace(appName) ? "nuru-app" : appName);
    string path = Path.Combine(directory, name + ".sock");
    if (path.Length <= MaxSocketPathLength)
      return path;

    string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..16];
    return Path.Combine(directory, hash.ToLowerInvariant() + ".sock");
  }

  /// <summary>
  /// The directory that holds default socket paths for the current user.
  /// </summary>
  /// <returns>An absolute directory path (not created).</returns>
  public static string GetDefaultSocketDirectory()
  {
    if (OperatingSystem.IsWindows())
    {
      string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      return Path.Combine(localAppData, "TimeWarp.Nuru", "daemon");
    }

    string? runtimeDirectory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
    if (!string.IsNullOrEmpty(runtimeDirectory) && Directory.Exists(runtimeDirectory))
      return Path.Combine(runtimeDirectory, "nuru");

    return Path.Combine(Path.GetTempPath(), "nuru-" + Sanitize(Environment.UserName));
  }

  /// <summary>
  /// Creates the socket's directory if needed, readable and writable only by the current user on Unix.
  /// </summary>
  /// <param name="socketPath">The socket path.</param>
  public static void EnsureSocketDirectory(string socketPath)
  {
    ArgumentNullException.ThrowIfNull(socketPath);
    string? directory = Path.GetDirectoryName(socketPath);
    if (string.IsNullOrEmpty(directory) || Directory.Exists(directory))
      return;

    if (OperatingSystem.IsWindows())
      Directory.CreateDirectory(directory);
    else
      Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
  }

  /// <summary>
  /// Identifies this build of <paramref name="app"/>: name, version, entry file, and its write time.
  /// </summary>
  /// <param name="app">The app.</param>
  /// <returns>A string equal for two processes of the same build.</returns>
  public static string GetBuildId(NuruApp app)
  {
    ArgumentNullException.ThrowIfNull(app);
    CapabilitiesResponse catalog = app.Capabilities;
    string entry = GetEntryPath() ?? "";
    long ticks = entry.Length > 0 && File.Exists(entry) ? File.GetLastWriteTimeUtc(entry).Ticks : 0;
    return string.Join('|', catalog.Name, catalog.Version, entry, ticks.ToString(CultureInfo.InvariantCulture));
  }

  /// <summary>
  /// The file that starts this app: the executable, or the dll when the process is the <c>dotnet</c> host.
  /// </summary>
  /// <returns>The path, or null when it cannot be determined.</returns>
  internal static string? GetEntryPath()
  {
    string? processPath = Environment.ProcessPath;
    if (processPath is not null && !IsDotNetHost(processPath))
      return processPath;

    string[] commandLine = Environment.GetCommandLineArgs();
    return commandLine.Length > 0 && File.Exists(commandLine[0]) ? Path.GetFullPath(commandLine[0]) : processPath;
  }

  /// <summary>True when <paramref name="processPath"/> is the <c>dotnet</c> muxer.</summary>
  internal static bool IsDotNetHost(string processPath)
  {
    string fileName = Path.GetFileNameWithoutExtension(processPath);
    return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
  }

  private static string Sanitize(string value)
  {
    StringBuilder builder = new(value.Length);
    foreach (char c in value)
      builder.Append(char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_');
    return builder.ToString();
  }
}
