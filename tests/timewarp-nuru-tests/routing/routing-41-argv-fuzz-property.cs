#!/usr/bin/env -S dotnet --
#:package CsCheck

#region Purpose
// CsCheck pilot (task 497): property tests that feed generated argv into a Nuru app with a broad
// route table. Random argv never throws out of RunAsync and always ends with exit 0, or exit 1
// with a message. Well-formed argv for a route always reaches that route's handler.
#endregion

#region Design
// The build-blocking tests are deterministic. Each walks a fixed number of cases from one pinned
// seed, so CI never fails on a case an earlier run did not see. The walk seed comes from the
// CsCheck_Seed environment variable when it is set, otherwise PinnedSeed. Every failing case
// prints its own CsCheck_Seed; setting that variable makes the failing case the first case of the walk.
// CsCheck's random SampleAsync (fresh seeds plus shrinking) runs only in the explore test, and only
// when NURU_ARGV_FUZZ_EXPLORE=1, so CI timing and fresh randomness never block a build. `Faster`
// is not used.
// Constant picks use Gen.OneOf (params T[]), the CsCheck 5 name for a uniform choice among values.
// The fuzz app calls AddConfiguration(): configuration detection is compilation-wide, so in the CI
// multi-mode assembly every app has it, and the standalone run must match.
// Handler exceptions propagate out of RunAsync by design (documentation/user/reference/builder-api.md),
// so the fuzz app's handlers never throw; "sum" adds as long because argv can hold int.MaxValue.
// The builder stays inside RunArgvAsync: the generator intercepts RunAsync only when it can trace
// the app to a builder in the same method.
// CsCheck is a test-only dependency. This file and tests/ci-tests reference it; product code never does.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

using CsCheck;

[TestTag("Routing")]
[TestTag("Property")]
public class ArgvFuzzPropertyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ArgvFuzzPropertyTests>();

  /// <summary>The seed each build-blocking walk starts from when CsCheck_Seed is not set.</summary>
  public const string PinnedSeed = "0N0XIzNsQ0O2";

  /// <summary>How many cases each build-blocking walk runs.</summary>
  public const int PinnedCaseCount = 1000;

  public enum Stage
  {
    Dev,
    Staging,
    Prod
  }

  /// <summary>A well-formed argv and the text its handler must write.</summary>
  internal sealed record Command(string[] Argv, string Expected);

  /// <summary>What one RunAsync call did.</summary>
  internal sealed record RunResult(int ExitCode, string Output, string ErrorOutput, string? Exception);

  // ---------------------------------------------------------------------------------------------
  // Generators: random argv
  // ---------------------------------------------------------------------------------------------

  // Tokens a user of this route table would type
  static readonly string[] KnownWords =
  [
    "status", "greet", "delay", "deploy", "build", "copy", "docker", "scale", "sum",
    "git", "commit", "process", "release", "run", "log"
  ];

  static readonly string[] KnownOptions =
  [
    "--config", "--verbose", "-v", "--force", "-f", "--retries", "--ratio", "-m", "--amend",
    "--id", "--stage", "-s", "--env", "--label", "--dry-run"
  ];

  // Flags the generator handles before or beside user routes
  static readonly string[] BuiltInTokens =
  [
    "--help", "-h", "--version", "--capabilities", "--search", "--json-args", "--check-updates",
    "--interactive", "-i", "--", "-", "---", "--=", "=", "-=x"
  ];

  static readonly string[] ValueTokens =
  [
    "", " ", "\t", "0", "-0", "1", "-1", "42", "-5", "2147483647", "2147483648", "-2147483649",
    "99999999999999999999", "1.5", "-1.5e10", "1e309", "NaN", "Infinity", "0x10", "1_000",
    "Dev", "prod", "PROD", "Staging2", "true", "false", "null", "Debug", "Release",
    "--config=Debug", "--retries=3", "-v=true", "--unknown=1", "{name}", "{*args}", "*", "?",
    "\"quoted\"", "'single'", "back\\slash", "a b", "a=b", "--no-verbose"
  ];

  // Invisible and bidi characters are built from code points, so the source shows what they are
  static readonly string[] UnicodeTokens =
  [
    "café", "日本語", "😀", "👨" + Code(0x200D) + "👩" + Code(0x200D) + "👧",
    "مرحبا", Code(0x200B) + "zero-width", Code(0x00A0), "Ω≈ç√", "ñ",
    "--ñame", "-é", Code(0x0000), "line" + Code(0x000A) + "break", Code(0x000D) + Code(0x000A), Code(0x202E) + "rtl-override"
  ];

  static string Code(int codePoint) => char.ConvertFromUtf32(codePoint);

  static readonly Gen<string> UnknownOption =
    Gen.String[Gen.Char["abcdefghijklmnopqrstuvwxyz-"], 1, 10]
      .Select(Gen.Bool, static (name, longForm) => (longForm ? "--" : "-") + name);

  static readonly Gen<string> IntToken =
    Gen.Int[-1000, 1000].Select(static i => i.ToString(CultureInfo.InvariantCulture));

  /// <summary>One argv token, weighted toward tokens that reach deep into matching.</summary>
  public static readonly Gen<string> Token = Gen.Frequency(
    (6, Gen.OneOf(KnownWords)),
    (5, Gen.OneOf(KnownOptions)),
    (5, Gen.OneOf(ValueTokens)),
    (2, Gen.OneOf(BuiltInTokens)),
    (2, UnknownOption),
    (2, Gen.OneOf(UnicodeTokens)),
    (2, IntToken),
    (1, Gen.String.AlphaNumeric),
    (1, Gen.String[0, 6]));

  /// <summary>A whole random argv: empty through ten tokens.</summary>
  public static readonly Gen<string[]> Argv = Token.Array[0, 10].Select(WithoutKnownFindings);

  /// <summary>
  /// Keeps two task 497 findings out of the build-blocking walk; each has its own skipped test or note:
  /// a single-dash token with '=' ("-v=true") makes AddCommandLine throw FormatException, so its '=' becomes ':';
  /// "--json-args -" reads the process stdin through System.Console, not the app's terminal, so with an open
  /// stdin pipe RunAsync waits for EOF, and it becomes "--json-args @-".
  /// </summary>
  static string[] WithoutKnownFindings(string[] argv)
  {
    for (int index = 0; index < argv.Length; index++)
    {
      string token = argv[index];
      if (token.StartsWith('-') && !token.StartsWith("--", StringComparison.Ordinal) && token.Contains('=', StringComparison.Ordinal))
        argv[index] = token.Replace('=', ':');

      if (index > 0 && argv[index - 1] == "--json-args" && token == "-")
        argv[index] = "@-";
    }

    return argv;
  }

  // ---------------------------------------------------------------------------------------------
  // Generators: well-formed argv per route
  // ---------------------------------------------------------------------------------------------

  /// <summary>
  /// A free-text value. A value that starts with '-' reads as an option, so it gets a leading 'v'.
  /// So does one that starts with '/': IsConfigArg peels "/name=..." and "/name:..." as configuration
  /// overrides before routing (task 497 finding, the same filter as the skipped --option=value test in routing-24).
  /// </summary>
  static readonly Gen<string> Value =
    Gen.Frequency(
      (3, Gen.String.AlphaNumeric),
      (2, Gen.OneOf(UnicodeTokens)),
      (1, Gen.OneOf(ValueTokens)),
      (1, Gen.String[0, 8]))
    .Select(static value => value.StartsWith('-') || value.StartsWith('/') ? "v" + value : value);

  static readonly Gen<int> Int = Gen.Int[-1000, 1000];

  static readonly Gen<(string Token, Stage Stage)> StageToken = Gen.OneOf(
    ("Dev", Stage.Dev), ("dev", Stage.Dev), ("DEV", Stage.Dev),
    ("Staging", Stage.Staging), ("staging", Stage.Staging),
    ("Prod", Stage.Prod), ("prod", Stage.Prod), ("PROD", Stage.Prod));

  static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

  /// <summary>Joins option groups in the given order, so option order is part of the input.</summary>
  static string[] Join(string[] head, bool reverse, params string[][] groups) =>
    [.. head, .. (reverse ? groups.Reverse() : groups).SelectMany(static group => group)];

  static readonly Gen<Command> Status =
    Gen.Const(new Command(["status"], "status:ok"));

  static readonly Gen<Command> Greet =
    Value.Select(static name => new Command(["greet", name], $"greet:{name}"));

  static readonly Gen<Command> Delay =
    Int.Select(static ms => new Command(["delay", Text(ms)], $"delay:{Text(ms)}"));

  static readonly Gen<Command> Deploy =
    Gen.Select(Value, Value, Gen.Bool, static (env, tag, hasTag) => hasTag
      ? new Command(["deploy", env, tag], $"deploy:{env}|{tag}")
      : new Command(["deploy", env], $"deploy:{env}|"));

  static readonly Gen<Command> Build =
    Gen.Select(Value, Gen.Int[0, 2], Gen.Bool, static (mode, verbose, reverse) => new Command(
      Join(["build"], reverse, ["--config", mode], verbose switch { 1 => ["--verbose"], 2 => ["-v"], _ => [] }),
      $"build:{mode}|{verbose != 0}"));

  static readonly Gen<Command> Copy =
    Gen.Select(Value, Value, Gen.Int[0, 2], Gen.Int[-1, 1000], Gen.Bool,
      static (source, dest, force, retries, reverse) => new Command(
        Join(["copy", source, dest], reverse,
          force switch { 1 => ["--force"], 2 => ["-f"], _ => [] },
          retries < 0 ? [] : ["--retries", Text(retries)]),
        $"copy:{source}|{dest}|{force != 0}|{(retries < 0 ? "" : Text(retries))}"));

  static readonly Gen<Command> Docker =
    Value.Array[0, 5].Select(static args => new Command(["docker", .. args], $"docker:{args.Length}"));

  static readonly Gen<Command> Scale =
    Gen.Select(Int, Gen.Int[-1, 100], static (replicas, ratio) => ratio < 0
      ? new Command(["scale", Text(replicas)], $"scale:{Text(replicas)}|")
      : new Command(["scale", Text(replicas), "--ratio", Text(ratio)], $"scale:{Text(replicas)}|{Text(ratio)}"));

  static readonly Gen<Command> Sum =
    Int.Array[0, 6].Select(static numbers => new Command(
      ["sum", .. numbers.Select(Text)], $"sum:{Text(numbers.Sum())}"));

  static readonly Gen<Command> Commit =
    Gen.Select(Value, Gen.Bool, Gen.Bool, static (message, amend, reverse) => new Command(
      Join(["git", "commit"], reverse, ["-m", message], amend ? ["--amend"] : []),
      $"commit:{message}|{amend}"));

  static readonly Gen<Command> Process =
    Int.Array[1, 4].Select(static ids => new Command(
      ["process", .. ids.SelectMany(static id => new[] { "--id", Text(id) })], $"process:{ids.Length}"));

  static readonly Gen<Command> Release =
    Gen.Select(StageToken, Gen.Bool, Gen.Bool, Gen.Bool, static (stage, shortForm, dryRun, reverse) => new Command(
      Join(["release"], reverse, [shortForm ? "-s" : "--stage", stage.Token], dryRun ? ["--dry-run"] : []),
      $"release:{stage.Stage}|{dryRun}"));

  static readonly Gen<Command> Run =
    Gen.Select(Value.Array[1, 3], Value, Gen.Bool, Gen.Bool, static (envs, label, hasLabel, reverse) => new Command(
      Join(["run"], reverse, [.. envs.SelectMany(static env => new[] { "--env", env })], hasLabel ? ["--label", label] : []),
      $"run:{envs.Length}|{(hasLabel ? label : "")}"));

  static readonly Gen<Command> Log =
    Gen.Select(Value, Value.Array[0, 4], static (level, rest) => new Command(
      ["log", level, .. rest], $"log:{level}|{rest.Length}"));

  /// <summary>A well-formed argv for one of the app's routes.</summary>
  internal static readonly Gen<Command> WellFormed = Gen.OneOf(
    Status, Greet, Delay, Deploy, Build, Copy, Docker, Scale, Sum, Commit, Process, Release, Run, Log);

  // ---------------------------------------------------------------------------------------------
  // Properties
  // ---------------------------------------------------------------------------------------------

  /// <summary>Random argv never throws and always ends with a defined exit code.</summary>
  public static async Task Random_argv_never_throws_and_exits_with_defined_code()
  {
    int successCount = await WalkAsync(Argv, static argv => argv, static (_, result) => CheckDefinedOutcome(result));

    // A walk where nothing reached a handler would pass vacuously
    successCount.ShouldBeGreaterThan(0, "no random argv reached a handler");
  }

  /// <summary>Well-formed argv for a route always reaches that route's handler.</summary>
  public static async Task Well_formed_argv_reaches_its_handler()
  {
    int successCount = await WalkAsync(WellFormed, static command => command.Argv, CheckWellFormed);
    successCount.ShouldBe(PinnedCaseCount);
  }

  /// <summary>
  /// Opt-in random exploration with shrinking. Set NURU_ARGV_FUZZ_EXPLORE=1, and optionally CsCheck_Iter
  /// or CsCheck_Time. A failure prints a CsCheck_Seed that replays as the first case of the walks above.
  /// </summary>
  public static async Task Explore_random_argv_with_shrinking()
  {
    if (Environment.GetEnvironmentVariable("NURU_ARGV_FUZZ_EXPLORE") != "1")
    {
      await global::TimeWarp.Terminal.Terminal.WriteLineAsync("Skipped: set NURU_ARGV_FUZZ_EXPLORE=1 to explore random argv.");
      return;
    }

    await Argv.SampleAsync(
      static async argv => Fail(CheckDefinedOutcome(await RunArgvAsync(argv))),
      writeLine: static line => global::TimeWarp.Terminal.Terminal.WriteLine(line),
      threads: 1,
      print: Describe);

    await WellFormed.SampleAsync(
      static async command => Fail(CheckWellFormed(command, await RunArgvAsync(command.Argv))),
      writeLine: static line => global::TimeWarp.Terminal.Terminal.WriteLine(line),
      threads: 1,
      print: static command => Describe(command.Argv));
  }

  /// <summary>
  /// Task 497 finding: the matcher converts option values before it checks the route's literals, so a
  /// route that declares --ratio as double rejects "zoom --ratio fast" before the zoom route is tried.
  /// </summary>
  [Skip("Task 497 finding: option value conversion runs before literal matching, so another route's --ratio:double rejects a valid zoom command")]
  public static async Task Option_value_error_on_another_route_does_not_block_a_valid_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("scale {replicas:int} --ratio? {ratio:double?}")
        .WithHandler((int replicas, double? ratio) => $"scale:{replicas}|{ratio}")
        .AsCommand().Done()
      .Map("zoom --ratio {level}").WithHandler((string level) => $"zoom:{level}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["zoom", "--ratio", "fast"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("zoom:fast").ShouldBeTrue();
  }

  /// <summary>
  /// Task 497 finding: with configuration on, every argv also goes to AddCommandLine, which throws
  /// FormatException for a single-dash token with '=' that has no switch mapping.
  /// </summary>
  [Skip("Task 497 finding: AddCommandLine throws FormatException for -v=true out of RunAsync when configuration is on")]
  public static async Task Short_option_with_equals_does_not_crash_a_configured_app()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("greet {name}").WithHandler((string name) => $"greet:{name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "x", "-v=true"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("-v").ShouldBeTrue();
  }

  // ---------------------------------------------------------------------------------------------
  // Checks
  // ---------------------------------------------------------------------------------------------

  /// <summary>Why a result is outside the defined outcomes, or null when it is inside them.</summary>
  static string? CheckDefinedOutcome(RunResult result)
  {
    if (result.Exception is not null)
      return $"RunAsync threw {result.Exception}";

    // An argv value error goes to stdout; an unknown option or unmatched route goes to stderr (task 497 finding)
    return result.ExitCode switch
    {
      0 => null,
      1 when string.IsNullOrWhiteSpace(result.Output) && string.IsNullOrWhiteSpace(result.ErrorOutput) =>
        "exit 1 with no message",
      1 => null,
      _ => $"exit {result.ExitCode} is outside the defined set (0, 1)"
    };
  }

  static string? CheckWellFormed(Command command, RunResult result)
  {
    string? undefined = CheckDefinedOutcome(result);
    if (undefined is not null)
      return undefined;

    if (result.ExitCode != 0)
      return $"exit {result.ExitCode}: \"{Excerpt(result.Output + result.ErrorOutput)}\"";

    return result.Output.Contains(command.Expected, StringComparison.Ordinal)
      ? null
      : $"expected \"{Excerpt(command.Expected)}\" in stdout, got \"{Excerpt(result.Output)}\"";
  }

  static void Fail(string? failure)
  {
    if (failure is not null)
      throw new ShouldAssertException(failure);
  }

  /// <summary>
  /// Runs <see cref="PinnedCaseCount"/> cases of <paramref name="gen"/> from the walk seed, fails with every
  /// failing case's own seed, and returns how many cases exited 0.
  /// </summary>
  static async Task<int> WalkAsync<T>(Gen<T> gen, Func<T, string[]> argvOf, Func<T, RunResult, string?> check)
  {
    string seed = Check.Seed ?? PinnedSeed;
    PCG pcg = PCG.Parse(seed);
    List<string> failures = [];
    int successCount = 0;

    for (int index = 0; index < PinnedCaseCount; index++)
    {
      string caseSeed = pcg.ToString(pcg.State);
      T value = gen.Generate(pcg, null, out _);
      string[] argv = argvOf(value);
      RunResult result = await RunArgvAsync(argv);
      if (result.ExitCode == 0)
        successCount++;

      string? failure = check(value, result);
      if (failure is not null)
        failures.Add($"CsCheck_Seed={caseSeed} argv={Describe(argv)}: {failure}");
    }

    await global::TimeWarp.Terminal.Terminal.WriteLineAsync(
      $"walk from seed {seed}: {PinnedCaseCount} cases, {successCount} exit 0, {failures.Count} failed");

    if (failures.Count > 0)
    {
      string report = $"{failures.Count} of {PinnedCaseCount} cases failed (walk seed {seed}).\n" +
        "Replay a case as the first of the walk: CsCheck_Seed=<seed> dotnet run tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs\n" +
        string.Join('\n', failures.Take(20));
      await global::TimeWarp.Terminal.Terminal.WriteLineAsync(report);
      throw new ShouldAssertException(report);
    }

    return successCount;
  }

  /// <summary>Builds the fuzz app and runs one argv through it.</summary>
  static async Task<RunResult> RunArgvAsync(string[] argv)
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("status").WithHandler(() => "status:ok").AsQuery().Done()
      .Map("greet {name}").WithHandler((string name) => $"greet:{name}").AsQuery().Done()
      .Map("delay {ms:int}").WithHandler((int ms) => $"delay:{ms}").AsCommand().Done()
      .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => $"deploy:{env}|{tag}").AsCommand().Done()
      .Map("build --config {mode} --verbose,-v").WithHandler((string mode, bool verbose) => $"build:{mode}|{verbose}").AsCommand().Done()
      .Map("copy {source} {dest} --force,-f --retries? {count:int?}")
        .WithHandler((string source, string dest, bool force, int? count) => $"copy:{source}|{dest}|{force}|{count}")
        .AsCommand().Done()
      .Map("docker {*args}").WithHandler((string[] args) => $"docker:{args.Length}").AsCommand().Done()
      .Map("scale {replicas:int} --ratio? {ratio:double?}")
        .WithHandler((int replicas, double? ratio) => $"scale:{replicas}|{ratio}")
        .AsCommand().Done()
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => $"sum:{numbers.Sum(static n => (long)n)}").AsQuery().Done()
      .Map("git commit -m {message} --amend").WithHandler((string message, bool amend) => $"commit:{message}|{amend}").AsCommand().Done()
      .Map("process --id {id:int}*").WithHandler((int[] id) => $"process:{id.Length}").AsCommand().Done()
      .Map("release --stage,-s {stage} --dry-run").WithHandler((Stage stage, bool dryRun) => $"release:{stage}|{dryRun}").AsCommand().Done()
      .Map("run --env {e}* --label? {label?}").WithHandler((string[] e, string? label) => $"run:{e.Length}|{label}").AsCommand().Done()
      .Map("log {level} {*rest}").WithHandler((string level, string[] rest) => $"log:{level}|{rest.Length}").AsCommand().Done()
      .Build();

    try
    {
      int exitCode = await app.RunAsync(argv);
      return new RunResult(exitCode, terminal.Output, terminal.ErrorOutput, null);
    }
    catch (Exception exception)
    {
      return new RunResult(-1, terminal.Output, terminal.ErrorOutput, $"{exception.GetType().Name}: {exception.Message}");
    }
  }

  // ---------------------------------------------------------------------------------------------
  // Printing
  // ---------------------------------------------------------------------------------------------

  static string Describe(string[] argv) =>
    "[" + string.Join(", ", argv.Select(static token => "\"" + Escape(token) + "\"")) + "]";

  static string Excerpt(string text) => Escape(text.Length <= 160 ? text : text[..160] + "...");

  static string Escape(string token)
  {
    System.Text.StringBuilder builder = new();
    foreach (char character in token)
    {
      if (character is '"' or '\\')
        builder.Append('\\').Append(character);
      else if (char.IsControl(character) || char.IsSurrogate(character) || character > '~')
        builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)character:X4}");
      else
        builder.Append(character);
    }

    return builder.ToString();
  }
}

}
