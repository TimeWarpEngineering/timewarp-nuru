#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

#region Purpose
// Pins --json-args option defaults, missing required positionals, conversion errors, and stdin/TTY handling (task 478); split from routing-33-json-args.cs.
#endregion

using TimeWarp.Amuru;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.JsonArgsStdinAndDefaults
{
[TestTag("Routing")]
public class JsonArgsStdinAndDefaultsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<JsonArgsStdinAndDefaultsTests>();

  public static async Task Should_keep_option_defaults_when_the_json_key_is_absent()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<JsonArgs478ShipCommand>()
      .Build();

    int baseline = await app.RunAsync(["ship", "--env", "prod"]);
    baseline.ShouldBe(0);
    terminal.OutputContains("env:prod|title:untitled|retries:3").ShouldBeTrue();

    terminal.Clear();
    int fromJson = await app.RunAsync(["ship", "--json-args", """{"env":"prod"}"""]);
    fromJson.ShouldBe(0);
    terminal.OutputContains("env:prod|title:untitled|retries:3").ShouldBeTrue();
  }

  public static async Task Should_name_a_missing_required_positional_when_later_literals_match()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("promote {env} now")
        .WithHandler((string env) => $"promoted:{env}")
        .AsCommand()
        .Done()
      .Build();

    int missing = await app.RunAsync(["promote", "now", "--json-args", "{}"]);
    missing.ShouldBe(1);
    terminal.ErrorContains("Missing required value 'env'").ShouldBeTrue();
    terminal.ErrorContains("Unknown command").ShouldBeFalse();

    terminal.Clear();
    int wrongLiteral = await app.RunAsync(["promote", "later", "--json-args", "{}"]);
    wrongLiteral.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();

    terminal.Clear();
    int ok = await app.RunAsync(["promote", "now", "--json-args", """{"env":"prod"}"""]);
    ok.ShouldBe(0);
    terminal.OutputContains("promoted:prod").ShouldBeTrue();
  }

  public static async Task Should_not_call_an_argv_conversion_failure_a_json_type_error()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --count {count:int}")
        .WithHandler((int count) => $"count:{count}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "--count", "nope", "--json-args", "{}"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("wrong JSON type").ShouldBeFalse();
    terminal.ErrorContains("Invalid value for 'count'").ShouldBeTrue();
    terminal.ErrorContains("Expected int").ShouldBeTrue();
  }

  public static async Task Should_cover_stdin_tty_and_a_builtin_that_does_not_read_stdin()
  {
    string dir = FindTestAppsHarnessDir();
    Directory.CreateDirectory(dir);
    string harness = Path.Combine(dir, "harness.cs");
    try
    {
      await File.WriteAllTextAsync(harness, """
        using TimeWarp.Nuru;
        NuruApp app = NuruApp.CreateBuilder()
          .Map("apply {patch}").WithHandler((string patch) => patch.Length.ToString()).Done()
          .Build();
        return await app.RunAsync(args);
        """);

      string big = new('x', 131073);
      (int bigCode, string bigOut, string bigErr) = await RunDotnet(harness, ["apply", "--json-args", "-"], "{\"patch\":\"" + big + "\"}");
      bigCode.ShouldBe(0, bigErr + bigOut);
      bigOut.ShouldContain("131073");

      (int emptyCode, _, string emptyErr) = await RunDotnet(harness, ["apply", "--json-args", "-"], "");
      emptyCode.ShouldBe(1, emptyErr);
      emptyErr.ShouldContain("empty stdin");

      (int ttyCode, _, string ttyErr) = await RunOnPty(harness, ["apply", "--json-args", "-"]);
      ttyCode.ShouldBe(1, ttyErr);
      ttyErr.ShouldContain("requires redirected stdin");

      (int versionCode, string versionOut, string versionErr) = await RunDotnet(harness, ["--version", "--json-args", "-"], "");
      versionCode.ShouldBe(0, versionErr + versionOut);
      versionErr.ShouldNotContain("empty stdin");
      versionOut.ShouldNotBeNullOrWhiteSpace();
    }
    finally
    {
      if (File.Exists(harness))
        File.Delete(harness);
    }
  }

  private static string FindTestAppsHarnessDir()
  {
    foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
      DirectoryInfo? dir = new(start);
      while (dir is not null)
      {
        string candidate = Path.Combine(dir.FullName, "tests", "test-apps", "Directory.Build.props");
        if (File.Exists(candidate))
          return Path.Combine(dir.FullName, "tests", "test-apps", "json-args-harness");

        dir = dir.Parent;
      }
    }

    throw new InvalidOperationException("tests/test-apps was not found.");
  }

  private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDotnet(
    string harness,
    string[] args,
    string stdin)
  {
    List<string> argv = ["run", harness, "--", .. args];
    CommandOutput output = await Shell.Builder("dotnet")
      .WithArguments([.. argv])
      .WithStandardInput(stdin)
      .WithNoValidation()
      .CaptureAsync();
    return (output.ExitCode, output.Stdout, output.Stderr);
  }

  private static async Task<(int ExitCode, string Stdout, string Stderr)> RunOnPty(string harness, string[] args)
  {
    string script = Path.Combine(Path.GetTempPath(), "nuru-json-args-478-pty.py");
    await File.WriteAllTextAsync(script, """
      import os, pty, subprocess, sys
      master, slave = pty.openpty()
      proc = subprocess.Popen(sys.argv[1:], stdin=slave, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
      os.close(slave)
      try:
          out, err = proc.communicate(timeout=180)
      except subprocess.TimeoutExpired:
          proc.kill()
          sys.stderr.write(b"pty timeout\n")
          sys.exit(3)
      sys.stdout.buffer.write(out)
      sys.stderr.buffer.write(err)
      sys.exit(proc.returncode)
      """);

    List<string> argv = [script, "dotnet", "run", harness, "--", .. args];
    CommandOutput output = await Shell.Builder("python3")
      .WithArguments([.. argv])
      .WithNoValidation()
      .CaptureAsync();
    return (output.ExitCode, output.Stdout, output.Stderr);
  }
}

[NuruRoute("ship")]
public sealed class JsonArgs478ShipCommand : ICommand<Unit>
{
  [Option("env", Description = "Target environment")]
  public string Env { get; set; } = "dev";

  [Option("title", Description = "Display title")]
  public string Title { get; set; } = "untitled";

  [Option("retries", Description = "Retry count")]
  public int Retries { get; set; } = 3;

  internal sealed class Handler(ITerminal terminal) : IRequestHandler<JsonArgs478ShipCommand, Unit>
  {
    public async ValueTask<Unit> Handle(JsonArgs478ShipCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      cancellationToken.ThrowIfCancellationRequested();
      await terminal.WriteLineAsync($"env:{command.Env}|title:{command.Title}|retries:{command.Retries}").ConfigureAwait(false);
      return default;
    }
  }
}
}
