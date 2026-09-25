#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using TimeWarp.Nuru.Search.Services;

// 470-008 (M30): ~/.nuru/index.db used to be created under the process umask (typically 0644).
// DatabasePath.EnsureIndexPath must create the directory 0700 and the file 0600 on Unix.
// Uses a temp directory so the real ~/.nuru is never touched.
[TestTag("Search")]
public class DatabasePathTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DatabasePathTests>();

  private const UnixFileMode OwnerOnlyDirectory = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
  private const UnixFileMode OwnerOnlyFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;

  private static string NewTempRoot() =>
    Path.Combine(Path.GetTempPath(), $"nuru-search-tests-{Guid.NewGuid():N}");

  public static async Task Should_create_directory_and_index_file()
  {
    string root = NewTempRoot();
    string nuruDir = Path.Combine(root, ".nuru");

    try
    {
      string indexPath = DatabasePath.EnsureIndexPath(nuruDir);

      indexPath.ShouldBe(Path.Combine(nuruDir, "index.db"));
      Directory.Exists(nuruDir).ShouldBeTrue();
      File.Exists(indexPath).ShouldBeTrue();
      new FileInfo(indexPath).Length.ShouldBe(0);
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_create_directory_owner_only_on_unix()
  {
    if (OperatingSystem.IsWindows())
    {
      return; // Windows relies on user-profile ACLs; no Unix mode to assert.
    }

    string root = NewTempRoot();
    string nuruDir = Path.Combine(root, ".nuru");

    try
    {
      DatabasePath.EnsureIndexPath(nuruDir);

      File.GetUnixFileMode(nuruDir).ShouldBe(OwnerOnlyDirectory);
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_create_index_file_owner_only_on_unix()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    string root = NewTempRoot();
    string nuruDir = Path.Combine(root, ".nuru");

    try
    {
      string indexPath = DatabasePath.EnsureIndexPath(nuruDir);

      File.GetUnixFileMode(indexPath).ShouldBe(OwnerOnlyFile);
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_be_idempotent_and_preserve_existing_content()
  {
    string root = NewTempRoot();
    string nuruDir = Path.Combine(root, ".nuru");

    try
    {
      string first = DatabasePath.EnsureIndexPath(nuruDir);
      await File.WriteAllBytesAsync(first, [1, 2, 3]);

      string second = DatabasePath.EnsureIndexPath(nuruDir);

      second.ShouldBe(first);
      (await File.ReadAllBytesAsync(second)).ShouldBe(new byte[] { 1, 2, 3 });
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
  }

  public static async Task Should_open_created_file_as_sqlite_database()
  {
    string root = NewTempRoot();
    string nuruDir = Path.Combine(root, ".nuru");

    try
    {
      string indexPath = DatabasePath.EnsureIndexPath(nuruDir);

      await using Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={indexPath}");
      await connection.OpenAsync();
      await using Microsoft.Data.Sqlite.SqliteCommand cmd = connection.CreateCommand();
      cmd.CommandText = "CREATE TABLE t (x INTEGER); INSERT INTO t VALUES (42); SELECT x FROM t";
      object? value = await cmd.ExecuteScalarAsync();

      value.ShouldBe(42L);
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
