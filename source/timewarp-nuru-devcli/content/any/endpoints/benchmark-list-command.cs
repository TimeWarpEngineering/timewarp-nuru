#region Purpose
// `dev benchmark list` (kanban task 578): print the named benchmarks this repo offers.
#endregion
#region Design
// One line per benchmark: the name, a tab, the unit in brackets and the description, in
// the order the repo registered them. --json prints an array of {name, description,
// unit}. A repo with no benchmarks prints nothing (or `[]`) and exits 0: an empty list
// means the repo does not take part in a perf sweep, which is not an error.
#endregion

namespace DevCli;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>List the named benchmarks this repo offers.</summary>
[NuruRoute("list", Description = "List this repo's named benchmarks (empty: the repo is not in a perf sweep)")]
internal sealed class BenchmarkListCommand : BenchmarkGroup, ICommand<Unit>
{
  [Option("json", Description = "Print a JSON array of {name, description, unit}")]
  public bool Json { get; set; }

  internal sealed class Handler : IRequestHandler<BenchmarkListCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(BenchmarkListCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      IReadOnlyList<IDevBenchmark> benchmarks = DevBenchmarks.All();
      if (command.Json)
      {
        Terminal.WriteLine(DevBenchmarkResult.ListToJson(benchmarks));
        return Unit.ValueTask;
      }

      foreach (IDevBenchmark benchmark in benchmarks)
      {
        Terminal.WriteLine(DevBenchmarks.FormatListLine(benchmark));
      }

      return Unit.ValueTask;
    }
  }
}
