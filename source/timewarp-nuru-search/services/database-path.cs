namespace TimeWarp.Nuru.Search.Services;

public static class DatabasePath
{
  private const string NuruDirectoryName = ".nuru";
  private const string IndexFileName = "index.db";

  // ~/.nuru is a private trust directory (it also holds REPL history). Owner-only on Unix;
  // Windows relies on the user-profile ACLs, which already exclude other users.
  private const UnixFileMode OwnerOnlyDirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
  private const UnixFileMode OwnerOnlyFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

  public static string GetIndexPath()
  {
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return EnsureIndexPath(Path.Combine(homeDir, NuruDirectoryName));
  }

  /// <summary>
  /// Ensures <paramref name="nuruDirectory"/> (mode 0700) and its <c>index.db</c> (mode 0600) exist
  /// and returns the index path. Modes are applied at creation only; SQLite inherits the file's
  /// mode for its journal/WAL side files.
  /// </summary>
  internal static string EnsureIndexPath(string nuruDirectory)
  {
    ArgumentException.ThrowIfNullOrEmpty(nuruDirectory);

    if (!Directory.Exists(nuruDirectory))
    {
      if (OperatingSystem.IsWindows())
      {
        Directory.CreateDirectory(nuruDirectory);
      }
      else
      {
        Directory.CreateDirectory(nuruDirectory, OwnerOnlyDirectoryMode);
      }
    }

    string indexPath = Path.Combine(nuruDirectory, IndexFileName);

    if (!File.Exists(indexPath))
    {
      // An empty file is a valid (empty) SQLite database. Creating it here, rather than letting
      // SQLite create it under the process umask, is what lets us pin the owner-only mode.
      FileStreamOptions options = new()
      {
        Mode = FileMode.OpenOrCreate,
        Access = FileAccess.Write,
        Share = FileShare.ReadWrite
      };

      if (!OperatingSystem.IsWindows())
      {
        options.UnixCreateMode = OwnerOnlyFileMode;
      }

      using FileStream stream = new(indexPath, options);
    }

    return indexPath;
  }
}
