#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

#region Purpose
// Pins --json-args: argv peel, route selection, and reflection-free binding of handler arguments from JSON.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{
[TestTag("Routing")]
public class JsonArgsTests
{
  public enum DeployEnv
  {
    Dev,
    Staging,
    Prod
  }

  [ModuleInitializer]
  internal static void Register() => RegisterTests<JsonArgsTests>();

  public static async Task Should_keep_todays_matcher_without_json_args()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Map("deploy --env {env}")
        .WithHandler((string env) => $"opt:{env}")
        .AsCommand()
        .Done()
      .Build();

    int positional = await app.RunAsync(["deploy", "prod"]);
    positional.ShouldBe(0);
    terminal.OutputContains("pos:prod").ShouldBeTrue();

    terminal.Clear();
    int option = await app.RunAsync(["deploy", "--env", "prod"]);
    option.ShouldBe(0);
    terminal.OutputContains("opt:prod").ShouldBeTrue();
  }

  public static async Task Should_let_argv_override_json_and_fill_absent_keys()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env} --title {title}")
        .WithHandler((string env, string title) => $"env:{env}|title:{title}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "staging", "--json-args", """{"env":"prod","title":"from-json"}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("env:staging|title:from-json").ShouldBeTrue();
  }

  public static async Task Should_let_the_last_duplicate_json_key_win()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "--json-args", """{"env":"dev","env":"prod"}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("pos:prod").ShouldBeTrue();
  }

  public static async Task Should_match_a_route_only_because_the_required_positional_is_in_json()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();

    int missing = await app.RunAsync(["deploy"]);
    missing.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();

    terminal.Clear();
    int exitCode = await app.RunAsync(["deploy", "--json-args", """{"env":"prod"}"""]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("pos:prod").ShouldBeTrue();
  }

  public static async Task Should_exit_1_for_an_unknown_key_and_name_the_pattern()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "--json-args", """{"env":"prod","nope":1}"""]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown key 'nope'").ShouldBeTrue();
    terminal.ErrorContains("deploy {env}").ShouldBeTrue();
    terminal.ErrorContains("Known names:").ShouldBeTrue();
    terminal.OutputContains("Unknown command").ShouldBeFalse();
  }

  public static async Task Should_exit_1_when_a_json_value_has_the_wrong_type()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --count {count:int}")
        .WithHandler((int count) => $"count:{count}")
        .AsCommand()
        .Done()
      .Build();

    int mismatch = await app.RunAsync(["deploy", "--json-args", """{"count":"12"}"""]);
    mismatch.ShouldBe(1);
    terminal.ErrorContains("wrong JSON type").ShouldBeTrue();
    terminal.ErrorContains("count").ShouldBeTrue();

    terminal.Clear();
    int ok = await app.RunAsync(["deploy", "--json-args", """{"count":12}"""]);
    ok.ShouldBe(0);
    terminal.OutputContains("count:12").ShouldBeTrue();
  }

  public static async Task Should_bind_an_enum_from_json_and_reject_an_unknown_name()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {env}")
        .WithHandler((DeployEnv env) => $"env:{env}")
        .AsCommand()
        .Done()
      .Build();

    int ok = await app.RunAsync(["deploy", "--json-args", """{"env":"prod"}"""]);
    ok.ShouldBe(0);
    terminal.OutputContains("env:Prod").ShouldBeTrue();

    terminal.Clear();
    int bad = await app.RunAsync(["deploy", "--json-args", """{"env":"nope"}"""]);
    bad.ShouldBe(1);
    terminal.ErrorContains("wrong JSON type").ShouldBeTrue();
  }

  public static async Task Should_replace_a_repeated_option_from_json_when_argv_also_supplies_it()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("docker run --env {e}*")
        .WithHandler((string[] e) => $"e:[{string.Join(",", e)}]")
        .AsCommand()
        .Done()
      .Build();

    int fromJson = await app.RunAsync(["docker", "run", "--json-args", """{"env":["A","B"]}"""]);
    fromJson.ShouldBe(0);
    terminal.OutputContains("e:[A,B]").ShouldBeTrue();

    terminal.Clear();
    int replaced = await app.RunAsync(["docker", "run", "--env", "C", "--json-args", """{"env":["A","B"]}"""]);
    replaced.ShouldBe(0);
    terminal.OutputContains("e:[C]").ShouldBeTrue();
  }

  public static async Task Should_bind_a_catch_all_from_a_json_array()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run {*args}")
        .WithHandler((string[] args) => $"args:[{string.Join(",", args)}]")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["run", "--json-args", """{"args":["one","two"]}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[one,two]").ShouldBeTrue();
  }

  public static async Task Should_read_json_from_an_at_file_and_reject_a_uri()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();
    string path = Path.Combine(Path.GetTempPath(), "nuru-json-args-478.json");
    await File.WriteAllTextAsync(path, """{"env":"prod"}""");
    try
    {
      int exitCode = await app.RunAsync(["deploy", "--json-args", "@" + path]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("pos:prod").ShouldBeTrue();
    }
    finally
    {
      File.Delete(path);
    }

    terminal.Clear();
    int uri = await app.RunAsync(["deploy", "--json-args", "@https://example.com/args.json"]);
    uri.ShouldBe(1);
    terminal.ErrorContains("does not accept URIs").ShouldBeTrue();
  }

  public static async Task Should_reject_each_attached_form()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();
    int equals = await app.RunAsync(["deploy", "--json-args={}"]);
    equals.ShouldBe(1);
    terminal.ErrorContains("must be a separate token").ShouldBeTrue();

    terminal.Clear();
    int colon = await app.RunAsync(["deploy", "--json-args:{}"]);
    colon.ShouldBe(1);
    terminal.ErrorContains("must be a separate token").ShouldBeTrue();

    terminal.Clear();
    int slashEquals = await app.RunAsync(["deploy", "/json-args={}"]);
    slashEquals.ShouldBe(1);
    terminal.ErrorContains("must be a separate token").ShouldBeTrue();

    terminal.Clear();
    int slashColon = await app.RunAsync(["deploy", "/json-args:{}"]);
    slashColon.ShouldBe(1);
    terminal.ErrorContains("must be a separate token").ShouldBeTrue();
  }

  public static async Task Should_list_both_patterns_when_literal_counts_tie()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Map("deploy --env {env}")
        .WithHandler((string env) => $"opt:{env}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "--json-args", """{"env":"prod"}"""]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("matches more than one route").ShouldBeTrue();
    terminal.ErrorContains("deploy {env}").ShouldBeTrue();
    terminal.ErrorContains("deploy --env {env}").ShouldBeTrue();
  }

  public static async Task Should_treat_help_as_the_json_value_when_it_is_the_next_token()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["deploy", "--json-args", "--help"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("must be -, @path, or a JSON object").ShouldBeTrue();
    terminal.OutputContains("--json-args").ShouldBeFalse();
  }

  public static async Task Should_show_a_root_help_row_and_a_per_route_line()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .Build();

    int root = await app.RunAsync(["--help"]);
    root.ShouldBe(0);
    terminal.OutputContains("--json-args").ShouldBeTrue("root help missing --json-args");
    terminal.OutputContains("Bind arguments").ShouldBeTrue("root help missing description");

    terminal.Clear();
    int route = await app.RunAsync(["deploy", "--help"]);
    route.ShouldBe(0);
    terminal.OutputContains("Values may come from --json-args.").ShouldBeTrue("per-route help missing json-args line");
  }

  public static async Task Should_reject_stdin_json_args_inside_the_repl_and_allow_a_quoted_object()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("deploy --json-args -");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueKeys("deploy --json-args '{\"env\":\"prod\"}'");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env}")
        .WithHandler((string env) => $"pos:{env}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.ErrorContains("not available in the REPL").ShouldBeTrue();
    terminal.OutputContains("pos:prod").ShouldBeTrue();
  }
}
}
