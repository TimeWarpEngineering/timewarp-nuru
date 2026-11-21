namespace TimeWarp.Nuru.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

class Program
{
  static void Main(string[] args)
  {
    ManualConfig config = DefaultConfig.Instance
                              .WithSummaryStyle(SummaryStyle.Default
                              .WithTimeUnit(TimeUnit.Millisecond))
                              .WithOptions(ConfigOptions.DisableOptimizationsValidator);

    config = config.AddDiagnoser(MemoryDiagnoser.Default);
    config = config.AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayLockContentionWhenZero: false, displayCompletedWorkItemCountWhenZero: false)));

    // Jobs run on the host runtime (net11.0); BenchmarkDotNet 0.16 dropped CsProjCoreToolchain.From.
    // Cold: the first call in a fresh process, JIT included (what a non-AOT CLI pays per launch).
    config = config.AddJob(Job.Default
                     .WithStrategy(RunStrategy.ColdStart)
                     .WithLaunchCount(5)
                     .WithWarmupCount(0)
                     .WithIterationCount(1)
                     .WithInvocationCount(1)
                     .DontEnforcePowerPlan()
                     .WithId("Cold"));

    // Warm: steady-state cost of one parse and dispatch after JIT and warm-up (task 501).
    config = config.AddJob(Job.Default
                     .WithStrategy(RunStrategy.Throughput)
                     .DontEnforcePowerPlan()
                     .WithId("Warm"));

    // Run CLI framework comparison benchmark
    BenchmarkRunner.Run<CliFrameworkBenchmark>(config, args);
  }
}
