#region Purpose
// Where a served app's socket lives, and which build of the app a running server is.
#endregion

#region Design
// Per-user, never a TCP port: $XDG_RUNTIME_DIR/nuru on Linux (tmpfs, mode 0700, owned by the user),
// otherwise a 0700 directory under the temp path named for the user; %LOCALAPPDATA% on Windows (AF_UNIX
// is supported on Windows 10 1803+). The temp name is predictable, so an existing socket directory is
// trusted only when it is not a symlink and grants nothing to group or others (another user's 0700
// directory cannot be entered at all), and the socket file is set to 0600 after bind. Socket paths are limited to about 104 bytes (macOS sun_path), so a
// long name falls back to a hash. The build id lets a client refuse a server started from another build
// of the same app: same name and version, same entry file, same write time.
//
// Socket per build (task 508-017): the default socket name carries a short hash of the build id, so two
// builds of one app (a dnx package-cache copy and a global tool, or a local build beside the installed
// one) each get their own warm server instead of stopping each other's. A rebuilt app gets a new socket;
// the old build's server stops on its idle timeout. The 32-bit hash only separates builds; the build id
// itself is still checked on ping and on every call.
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// Default socket locations and build identity for served Nuru apps.
/// </summary>
public static class NuruDaemonPaths
{
  // macOS sun_path is 104 bytes including the terminator; Linux allows 108.
  private const int MaxSocketPathLength = 100;

  private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

  // The build part of a default socket name: '-' and this many lowercase hex digits.
  private const int BuildHashLength = 8;

  /// <summary>
  /// The per-user socket path for an app named <paramref name="appName"/>, shared by every build.
  /// </summary>
  /// <remarks>
  /// The default a served app uses is <see cref="GetDefaultSocketPath(string?, string?)"/> with its build
  /// id, so builds do not share a server.
  /// </remarks>
  /// <param name="appName">The app name from the capabilities catalog.</param>
  /// <returns>An absolute path in a directory only the current user can use.</returns>
  public static string GetDefaultSocketPath(string? appName) => GetDefaultSocketPath(appName, buildId: null);

  /// <summary>
  /// The per-user default socket path for build <paramref name="buildId"/> of an app named
  /// <paramref name="appName"/>: <c>&lt;app&gt;-&lt;hash&gt;.sock</c>, so each build has its own server.
  /// </summary>
  /// <param name="appName">The app name from the capabilities catalog.</param>
  /// <param name="buildId">The build id (<see cref="GetBuildId"/>); null leaves the build out of the name.</param>
  /// <returns>An absolute path in a directory only the current user can use.</returns>
  public static string GetDefaultSocketPath(string? appName, string? buildId)
  {
    string directory = GetDefaultSocketDirectory();
    string suffix = buildId is null ? "" : "-" + ShortHash(buildId, BuildHashLength);
    string path = Path.Combine(directory, GetSocketNameStem(appName) + suffix + ".sock");
    if (path.Length <= MaxSocketPathLength)
      return path;

    return Path.Combine(directory, GetHashedSocketNameStem(appName) + suffix + ".sock");
  }

  /// <summary>
  /// Default socket paths in <see cref="GetDefaultSocketDirectory"/> that belong to an app named
  /// <paramref name="appName"/>: one per build, and the shared name without a build.
  /// </summary>
  /// <param name="appName">The app name from the capabilities catalog.</param>
  /// <returns>Existing socket file paths, sorted; none when the directory is missing or unsafe.</returns>
  internal static IReadOnlyList<string> FindDefaultSocketPaths(string? appName)
  {
    string directory = GetDefaultSocketDirectory();
    if (!Directory.Exists(directory) || CheckSocketDirectory(Path.Combine(directory, "x.sock")) is not null)
      return [];

    string[] stems = [GetSocketNameStem(appName), GetHashedSocketNameStem(appName)];
    List<string> found = [];
    foreach (string path in Directory.EnumerateFiles(directory, "*.sock"))
    {
      string file = Path.GetFileNameWithoutExtension(path);
      foreach (string stem in stems)
      {
        if (file == stem || IsBuildSocketName(file, stem))
        {
          found.Add(path);
          break;
        }
      }
    }

    found.Sort(StringComparer.Ordinal);
    return found;
  }

  private static bool IsBuildSocketName(string file, string stem)
  {
    if (file.Length != stem.Length + 1 + BuildHashLength || !file.StartsWith(stem, StringComparison.Ordinal) || file[stem.Length] != '-')
      return false;

    foreach (char c in file.AsSpan(stem.Length + 1))
    {
      if (!char.IsAsciiHexDigitLower(c))
        return false;
    }

    return true;
  }

  private static string GetSocketNameStem(string? appName) => Sanitize(string.IsNullOrWhiteSpace(appName) ? "nuru-app" : appName);

  private static string GetHashedSocketNameStem(string? appName) => ShortHash(GetSocketNameStem(appName), 16);

  private static string ShortHash(string value, int length) =>
    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..length].ToLowerInvariant();

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
  /// Creates the socket's directory if needed, and on Unix makes sure only the current user can use it.
  /// </summary>
  /// <remarks>
  /// A missing directory is created with mode 0700. An existing directory must not be a symlink and must
  /// give no access to group or others. The default directory (<see cref="GetDefaultSocketDirectory"/>)
  /// is tightened to 0700 when the current user owns it; a directory someone else made fails, because only
  /// its owner can change its mode.
  /// </remarks>
  /// <param name="socketPath">The socket path.</param>
  /// <exception cref="NuruDaemonException">The directory is not owner-only.</exception>
  public static void EnsureSocketDirectory(string socketPath)
  {
    ArgumentNullException.ThrowIfNull(socketPath);
    string? directory = Path.GetDirectoryName(socketPath);
    if (string.IsNullOrEmpty(directory))
      return;

    if (OperatingSystem.IsWindows())
    {
      Directory.CreateDirectory(directory);
      return;
    }

    if (!Directory.Exists(directory) && !File.Exists(directory))
      Directory.CreateDirectory(directory, OwnerOnly);

    if (CheckSocketDirectory(socketPath) is not { } problem)
      return;

    bool isDefault = string.Equals(
      Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)),
      Path.TrimEndingDirectorySeparator(GetDefaultSocketDirectory()),
      StringComparison.Ordinal);
    if (!isDefault || new DirectoryInfo(directory).LinkTarget is not null)
      throw new NuruDaemonException(problem);

    try
    {
      File.SetUnixFileMode(directory, OwnerOnly);
    }
    catch (UnauthorizedAccessException exception)
    {
      throw new NuruDaemonException(problem, exception);
    }

    if (CheckSocketDirectory(socketPath) is { } stillOpen)
      throw new NuruDaemonException(stillOpen);
  }

  /// <summary>
  /// Checks that the socket's directory is safe to use: on Unix it is a real directory (not a symlink)
  /// that gives no access to group or others.
  /// </summary>
  /// <remarks>
  /// Mode alone proves ownership here: a directory with no group or other bits that this process can
  /// enter belongs to the current user (or the process is root). Windows uses the per-user profile, so
  /// nothing is checked there.
  /// </remarks>
  /// <param name="socketPath">The socket path.</param>
  /// <returns>Null when the directory is safe or does not exist yet; otherwise what is wrong with it.</returns>
  public static string? CheckSocketDirectory(string socketPath)
  {
    ArgumentNullException.ThrowIfNull(socketPath);
    string? directory = Path.GetDirectoryName(socketPath);
    if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(directory))
      return null;

    DirectoryInfo info = new(directory);
    if (info.LinkTarget is not null)
      return $"Error: the socket directory {directory} is a symlink; use a real directory only you can access.";

    if (!info.Exists)
      return File.Exists(directory) ? $"Error: the socket directory {directory} is not a directory." : null;

    UnixFileMode open = info.UnixFileMode & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
      | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
    return open == UnixFileMode.None
      ? null
      : $"Error: the socket directory {directory} can be used by other users; it must be mode 0700 and owned by you.";
  }

  /// <summary>
  /// Makes a bound socket file readable and writable by the current user only (Unix).
  /// </summary>
  /// <param name="socketPath">The socket path.</param>
  internal static void RestrictSocketFile(string socketPath)
  {
    if (!OperatingSystem.IsWindows())
      File.SetUnixFileMode(socketPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
  }

  /// <summary>
  /// Identifies this build of <paramref name="app"/>: name, version, entry file, and its write time.
  /// </summary>
  /// <param name="app">The app.</param>
  /// <returns>A string equal for two processes of the same build.</returns>
  public static string GetBuildId(NuruApp app) => GetBuild(app).Id;

  /// <summary>This build of <paramref name="app"/>: its id and what a person reads to tell builds apart.</summary>
  internal static NuruDaemonBuild GetBuild(NuruApp app)
  {
    ArgumentNullException.ThrowIfNull(app);
    CapabilitiesResponse catalog = app.Capabilities;
    string entry = GetEntryPath() ?? "";
    DateTime? written = entry.Length > 0 && File.Exists(entry) ? File.GetLastWriteTimeUtc(entry) : null;
    long ticks = written?.Ticks ?? 0;
    string id = string.Join('|', catalog.Name, catalog.Version, entry, ticks.ToString(CultureInfo.InvariantCulture));
    string writeTime = written?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture) ?? "";
    return new NuruDaemonBuild(id, catalog.Version, entry, writeTime);
  }

  /// <summary>
  /// One line that names a build: version, entry file and its write time (the id when those are unknown,
  /// as from a server that predates them).
  /// </summary>
  internal static string DescribeBuild(string version, string entryPath, string writeTime, string buildId)
  {
    if (string.IsNullOrEmpty(entryPath))
      return string.IsNullOrEmpty(buildId) ? "unknown build" : buildId;

    string when = string.IsNullOrEmpty(writeTime) ? "" : $", written {writeTime}";
    return $"version {version} at {entryPath}{when}";
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

/// <summary>A build of a served app: its id, and the parts a person reads.</summary>
/// <param name="Id">The build id (<see cref="NuruDaemonPaths.GetBuildId"/>).</param>
/// <param name="Version">The app version.</param>
/// <param name="EntryPath">The file that starts the app; empty when unknown.</param>
/// <param name="WriteTime">That file's write time, ISO 8601 UTC; empty when unknown.</param>
internal sealed record NuruDaemonBuild(string Id, string Version, string EntryPath, string WriteTime)
{
  public string Describe() => NuruDaemonPaths.DescribeBuild(Version, EntryPath, WriteTime, Id);
}
