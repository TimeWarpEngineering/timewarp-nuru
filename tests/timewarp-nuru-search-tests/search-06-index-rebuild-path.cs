#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Nuru.Search.Endpoints;
using TimeWarp.Nuru.Search.Services;
using TimeWarp.Terminal;

// 482-011 (S-3): the index stored only capabilities.Name, and `index rebuild --all` then ran that
// name as a process. A CLI indexed by full path whose file name differs from its capabilities
// name could never be rebuilt. The executed path is now stored beside the name; --all runs the
// path, and `search --cli` still filters on the name. Uses an in-memory index and a fake
// executable in a temp directory, so ~/.nuru is never touched.
[TestTag("Search")]
public class IndexRebuildPathTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<IndexRebuildPathTests>();

  private const string CapabilitiesName = "mycli";

  [UnsupportedOSPlatform("windows")]
  private static string WriteFakeCli(string directory, string version)
  {
    // File name deliberately differs from the capabilities name.
    string path = Path.Combine(directory, "fake-tool-482-011");
    string json = $$"""
      {"name":"{{CapabilitiesName}}","version":"{{version}}","endpoints":[{"pattern":"deploy {env}","groupPath":[],"description":"Deploy hello world","kind":"command","parameters":[],"options":[]}]}
      """;

    File.WriteAllText(path, $"#!/bin/sh\ncat <<'JSON'\n{json}\nJSON\n");
    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    return path;
  }

  private static IndexRebuildCommand.Handler CreateHandler(SearchIndex index, TestTerminal terminal) =>
    new(index, new CapabilitiesClient(NullLogger<CapabilitiesClient>.Instance), terminal);

  public static async Task Should_store_executed_path_and_rebuild_all_from_it()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    string directory = Path.Combine(Path.GetTempPath(), $"nuru-search-482-011-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);

    try
    {
      string cliPath = WriteFakeCli(directory, "1.0.0");
      await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
      using TestTerminal terminal = new();
      IndexRebuildCommand.Handler handler = CreateHandler(index, terminal);

      await handler.Handle(new IndexRebuildCommand { Cli = cliPath }, CancellationToken.None);

      IReadOnlyList<CliInfo> clis = await index.ListClisAsync();
      clis.Count.ShouldBe(1);
      clis[0].Name.ShouldBe(CapabilitiesName);
      clis[0].CliPath.ShouldBe(Path.GetFullPath(cliPath));
      clis[0].Version.ShouldBe("1.0.0");

      // Same path, new version: --all must run the stored path. "mycli" is not on PATH, so
      // running the capabilities name would fail and leave 1.0.0 in place.
      WriteFakeCli(directory, "2.0.0");
      await handler.Handle(new IndexRebuildCommand { All = true }, CancellationToken.None);

      terminal.OutputContains("1 succeeded, 0 failed").ShouldBeTrue(terminal.Output);
      clis = await index.ListClisAsync();
      clis.Count.ShouldBe(1);
      clis[0].Version.ShouldBe("2.0.0");
      clis[0].CliPath.ShouldBe(Path.GetFullPath(cliPath));

      // search --cli filters on the capabilities name, not the file name.
      (await index.SearchAsync("deploy", CapabilitiesName)).Count.ShouldBe(1);
      (await index.SearchAsync("deploy", Path.GetFileName(cliPath))).Count.ShouldBe(0);
    }
    finally
    {
      Directory.Delete(directory, recursive: true);
    }
  }

  public static async Task Should_leave_path_null_when_indexed_without_one()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");

    await index.IndexCliAsync(CapabilitiesName, "1.0.0", "{}", []);

    IReadOnlyList<CliInfo> clis = await index.ListClisAsync();
    clis.Count.ShouldBe(1);
    clis[0].CliPath.ShouldBeNull();
  }

  public static async Task Should_add_cli_path_column_to_existing_index()
  {
    string databasePath = Path.Combine(Path.GetTempPath(), $"nuru-search-482-011-{Guid.NewGuid():N}.db");

    try
    {
      // An index created before cli_path existed.
      await using (SqliteConnection legacy = new($"Data Source={databasePath};Pooling=False"))
      {
        await legacy.OpenAsync();
        await using SqliteCommand cmd = legacy.CreateCommand();
        cmd.CommandText = """
          CREATE TABLE clis (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            version TEXT NOT NULL,
            indexed_at TEXT NOT NULL,
            capabilities_json TEXT NOT NULL
          );
          INSERT INTO clis (name, version, indexed_at, capabilities_json)
          VALUES ('oldcli', '0.1.0', '2026-01-01T00:00:00.0000000Z', '{}');
          """;
        await cmd.ExecuteNonQueryAsync();
      }

      await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, databasePath);

      IReadOnlyList<CliInfo> clis = await index.ListClisAsync();
      clis.Count.ShouldBe(1);
      clis[0].Name.ShouldBe("oldcli");
      clis[0].CliPath.ShouldBeNull();

      await index.IndexCliAsync("newcli", "1.0.0", "{}", [], "/opt/tools/newcli");
      clis = await index.ListClisAsync();
      clis.Single(c => c.Name == "newcli").CliPath.ShouldBe("/opt/tools/newcli");
    }
    finally
    {
      SqliteConnection.ClearAllPools();
      File.Delete(databasePath);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
