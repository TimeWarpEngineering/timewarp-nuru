#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#region Purpose
// Pins the `--cli` key: `nuru search --cli` and `nuru index clear --cli` accept the app name (WithName) or
// the executable name of an indexed CLI (task 534).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Amuru;
using TimeWarp.Nuru.Search.Endpoints;
using TimeWarp.Terminal;

// 534: an app built with WithName("probe") from an executable named core-routing was indexed, and
// `nuru search --cli core-routing` found nothing: the filter compared the value to the app name only,
// while auto-index had resolved it as an executable on PATH. Both names now select the CLI. The fake
// executable is a shell script in a temp directory and every index is in memory or under a temp HOME,
// so ~/.nuru is never touched.
[TestTag("Search")]
public class CliKeyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CliKeyTests>();

  private const string AppName = "probe";
  private const string ExecutableName = "core-routing-534";

  [UnsupportedOSPlatform("windows")]
  private static string WriteFakeCli(string directory)
  {
    string path = Path.Combine(directory, ExecutableName);
    string json = $$"""
      {"name":"{{AppName}}","version":"1.0.0","endpoints":[{"pattern":"add {x:int} {y:int}","groupPath":[],"description":"Add two integers","kind":"command","parameters":[],"options":[]}]}
      """;

    File.WriteAllText(path, $"#!/bin/sh\ncat <<'JSON'\n{json}\nJSON\n");
    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    return path;
  }

  private static EndpointCapability AddEndpoint() => new()
  {
    Pattern = "add {x:int} {y:int}",
    GroupPath = [],
    Description = "Add two integers",
    Kind = EndpointKind.Command,
    Parameters = [],
    Options = []
  };

  public static async Task Should_find_cli_by_app_name_and_by_executable_name()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync(AppName, "1.0.0", "{}", [AddEndpoint()], $"/opt/tools/{ExecutableName}");

    (await index.SearchAsync("integers", AppName)).Single().CliName.ShouldBe(AppName);
    (await index.SearchAsync("integers", ExecutableName)).Single().CliName.ShouldBe(AppName);
    (await index.SearchAsync("integers", "no-such-cli")).Count.ShouldBe(0);
  }

  public static async Task Should_match_executable_name_without_extension()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync(AppName, "1.0.0", "{}", [AddEndpoint()], $"/opt/tools/{ExecutableName}.exe");

    (await index.ResolveCliNamesAsync(ExecutableName)).ShouldBe([AppName]);
    (await index.ResolveCliNamesAsync($"{ExecutableName}.exe")).ShouldBe([AppName]);
  }

  public static async Task Should_prefer_exact_app_name_over_executable_name()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");

    // "tool" is one CLI's app name and another CLI's executable name.
    await index.IndexCliAsync("tool", "1.0.0", "{}", [AddEndpoint()], "/opt/tools/tool-exe");
    await index.IndexCliAsync("other", "1.0.0", "{}", [AddEndpoint()], "/opt/tools/tool");

    (await index.ResolveCliNamesAsync("tool")).ShouldBe(["tool"]);
    (await index.SearchAsync("integers", "tool")).Single().CliName.ShouldBe("tool");
  }

  public static async Task Should_match_every_cli_sharing_an_executable_name()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync("alpha", "1.0.0", "{}", [AddEndpoint()], "/a/bin/tool");
    await index.IndexCliAsync("beta", "1.0.0", "{}", [AddEndpoint()], "/b/bin/tool");
    await index.IndexCliAsync("gamma", "1.0.0", "{}", [AddEndpoint()], "/c/bin/other");

    (await index.ResolveCliNamesAsync("tool")).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(["alpha", "beta"]);
    (await index.SearchAsync("integers", "tool")).Count.ShouldBe(2);
  }

  public static async Task Should_not_match_rows_without_a_stored_path_by_executable_name()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync(AppName, "1.0.0", "{}", [AddEndpoint()]);

    (await index.ResolveCliNamesAsync(ExecutableName)).Count.ShouldBe(0);
    (await index.ResolveCliNamesAsync(AppName)).ShouldBe([AppName]);
  }

  public static async Task Should_clear_cli_by_executable_name()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync(AppName, "1.0.0", "{}", [AddEndpoint()], $"/opt/tools/{ExecutableName}");
    using TestTerminal terminal = new();
    IndexClearCommand.Handler handler = new(index, terminal);

    await handler.Handle(new IndexClearCommand { Cli = ExecutableName, Force = true }, CancellationToken.None);

    terminal.OutputContains($"Cleared index for CLI: {AppName}").ShouldBeTrue(terminal.Output);
    (await index.ListClisAsync()).Count.ShouldBe(0);
  }

  public static async Task Should_report_no_match_when_clearing_unknown_cli()
  {
    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");
    await index.IndexCliAsync(AppName, "1.0.0", "{}", [AddEndpoint()], $"/opt/tools/{ExecutableName}");
    using TestTerminal terminal = new();
    IndexClearCommand.Handler handler = new(index, terminal);

    await handler.Handle(new IndexClearCommand { Cli = "no-such-cli", Force = true }, CancellationToken.None);

    terminal.OutputContains("No indexed CLI matches: no-such-cli").ShouldBeTrue(terminal.Output);
    (await index.ListClisAsync()).Count.ShouldBe(1);
  }

  // End to end through the real tool: `--cli <executable name>` auto-indexes from PATH, finds the
  // route, and does not index again on the next search; `--cli <app name>` finds the same route.
  public static async Task Should_auto_index_by_executable_name_then_search_by_either_name()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    string root = Path.Combine(Path.GetTempPath(), $"nuru-search-534-{Guid.NewGuid():N}");
    string home = Path.Combine(root, "home");
    string bin = Path.Combine(root, "bin");
    Directory.CreateDirectory(home);
    Directory.CreateDirectory(bin);

    try
    {
      WriteFakeCli(bin);
      string path = $"{bin}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";

      CommandOutput first = await RunSearchAsync(home, path, "--cli", ExecutableName, "integers");
      first.Stdout.ShouldContain($"Auto-indexed {AppName} v1.0.0 (1 endpoints)");
      first.Stdout.ShouldContain($"  add {{x:int}} {{y:int}} [{AppName}]");

      CommandOutput second = await RunSearchAsync(home, path, "--cli", ExecutableName, "integers");
      second.Stdout.ShouldNotContain("Auto-indexed");
      second.Stdout.ShouldContain($"  add {{x:int}} {{y:int}} [{AppName}]");

      CommandOutput byAppName = await RunSearchAsync(home, path, "--cli", AppName, "integers");
      byAppName.Stdout.ShouldNotContain("Auto-indexed");
      byAppName.Stdout.ShouldContain($"  add {{x:int}} {{y:int}} [{AppName}]");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private static async Task<CommandOutput> RunSearchAsync(string home, string path, params string[] arguments)
  {
    string searchDll = Path.Combine(AppContext.BaseDirectory, "TimeWarp.Nuru.Search.dll");

    CommandOutput output = await Shell.Builder("dotnet")
      .WithArguments([searchDll, "search", .. arguments])
      .WithEnvironmentVariable("HOME", home)
      .WithEnvironmentVariable("USERPROFILE", home)
      .WithEnvironmentVariable("PATH", path)
      .WithNoValidation()
      .CaptureAsync();

    output.Success.ShouldBeTrue($"nuru search exited {output.ExitCode}: {output.Stderr}");
    return output;
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
