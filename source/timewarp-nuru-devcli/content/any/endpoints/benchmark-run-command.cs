#region Purpose
// `dev benchmark run <name> --base <ref>` (kanban task 578): build and run the named
// benchmark at the base ref and at the working tree in the same run, then print one JSON
// verdict (win / noise / regression) on stdout.
#endregion
#region Design
// Steps: find the benchmark in DevBenchmarks.All(); resolve the base ref and read the
// head revision (HEAD plus a dirty flag: head is the working tree, uncommitted changes
// included); check the base out into a temporary worktree (BenchmarkBaseCheckout);
// empty artifacts/benchmarks/<name>/{base,head} for the two builds; run
// DevBenchmarkRunner; print the result.
//
// stdout carries only the JSON result (DevBenchmarkResult.ToJson), so a caller can pipe
// it to jq. Progress and the one-line human summary go to stderr.
//
// Exit codes: 0 when a verdict was produced, whatever it is; the verdict field is the
// answer, and a perf sweep reads it. 1 when there is no verdict: unknown name, missing
// --base, a ref that names no commit, a failed build, a failing sample. Nothing is
// written to stdout then.
//
// No stored number is read or written: both sides run in this process on this machine.
#endregion

namespace DevCli;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>Run a named benchmark at a base ref and at the working tree and print the verdict.</summary>
[NuruRoute("run", Description = "Run a named benchmark at --base and at the working tree; print a JSON verdict")]
internal sealed class BenchmarkRunCommand : BenchmarkGroup, ICommand<Unit>
{
  [Parameter(Description = "Benchmark name from `dev benchmark list`")]
  public string Name { get; set; } = string.Empty;

  [Option("base", "b", Description = "Git ref to compare against (branch, tag or SHA; not fetched)")]
  public string? Base { get; set; }

  [Option("samples", "n", Description = "Samples kept per side (default 30)")]
  public int Samples { get; set; } = DevBenchmarkRunOptions.DefaultSamples;

  [Option("warmup", "w", Description = "Discarded warmup samples per side (default 3)")]
  public int Warmup { get; set; } = DevBenchmarkRunOptions.DefaultWarmup;

  [Option("noise-floor", Description = "Smallest noise band as a fraction (default 0.02 = 2%)")]
  public double NoiseFloor { get; set; } = BenchmarkComparison.DefaultNoiseFloor;

  internal sealed class Handler : IRequestHandler<BenchmarkRunCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BenchmarkRunCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      IReadOnlyList<IDevBenchmark> benchmarks = DevBenchmarks.All();
      IDevBenchmark? benchmark = DevBenchmarks.Find(benchmarks, command.Name);
      if (benchmark is null)
      {
        string known = benchmarks.Count == 0
          ? "This repo registers no benchmarks."
          : "Known: " + string.Join(", ", benchmarks.Select(candidate => candidate.Name));
        return Fail($"Unknown benchmark '{command.Name}'. {known}");
      }

      if (string.IsNullOrWhiteSpace(command.Base))
      {
        return Fail("Pass --base <git-ref>: the ref to compare the working tree against (for example origin/master).");
      }

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        return Fail("Could not find the repository root.");
      }

      DevBenchmarkRunOptions options = new(command.Samples, command.Warmup, command.NoiseFloor);
      try
      {
        DevBenchmarkRevision head = await BenchmarkBaseCheckout.ReadHeadAsync(repoRoot, cancellationToken).ConfigureAwait(false);
        await using BenchmarkBaseCheckout checkout = await BenchmarkBaseCheckout
          .CreateAsync(repoRoot, command.Base.Trim(), cancellationToken)
          .ConfigureAwait(false);
        DevBenchmarkRevision baseRevision = new(command.Base.Trim(), checkout.Commit, Dirty: false);

        string output = Path.Combine(repoRoot, "artifacts", "benchmarks", benchmark.Name);
        DevBenchmarkTree baseTree = new("base", checkout.Root, EmptyDirectory(Path.Combine(output, "base")));
        DevBenchmarkTree headTree = new("head", repoRoot, EmptyDirectory(Path.Combine(output, "head")));

        BenchmarkComparison comparison = await DevBenchmarkRunner
          .RunAsync(benchmark, baseTree, headTree, options, line => Terminal.WriteErrorLine(line), cancellationToken)
          .ConfigureAwait(false);

        DevBenchmarkResult result = new(benchmark.Name, benchmark.Unit, baseRevision, head, comparison);
        Terminal.WriteErrorLine(result.ToSummaryLine());
        Terminal.WriteLine(result.ToJson());
      }
      catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or IOException)
      {
        return Fail(exception.Message);
      }

      return Unit.Value;
    }

    private Unit Fail(string message)
    {
      Terminal.WriteErrorLine($"dev benchmark run: {message}".Red());
      Environment.ExitCode = 1;
      return Unit.Value;
    }

    private static string EmptyDirectory(string path)
    {
      if (Directory.Exists(path))
      {
        Directory.Delete(path, recursive: true);
      }

      Directory.CreateDirectory(path);
      return path;
    }
  }
}
