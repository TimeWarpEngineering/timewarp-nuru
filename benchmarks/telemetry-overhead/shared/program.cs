// Telemetry overhead benchmark app (task 520).
// bench-telemetry-off and bench-telemetry-on both compile this file. The on project
// defines BENCH_TELEMETRY, which adds UseTelemetry(); nothing else differs.
// The command is the same trivial one in every case: `greet --name x` writes one line.
// With NURU_BENCH_TIMELINE=1 the app also writes a timeline line to stderr (see bench-timeline.cs).

using TimeWarp.Nuru;

BenchTimeline.Start();

NuruApp app = NuruApp.CreateBuilder()
#if BENCH_TELEMETRY
  .UseTelemetry()
#endif
  .Map("greet --name {name}")
    .WithHandler((string name) =>
    {
      BenchTimeline.MarkHandler();
      return $"Hello, {name}";
    })
    .AsCommand()
    .Done()
  .Build();

int exitCode = await app.RunAsync(args);

BenchTimeline.MarkEnd();
BenchTimeline.Report();
return exitCode;
