namespace TimeWarp.Nuru.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
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

    // Use InProcess toolchain to avoid csproj name mismatch with kebab-case naming
    config = config.AddJob(Job.Default
                     .WithStrategy(RunStrategy.ColdStart)
                     .WithLaunchCount(1)
                     .WithWarmupCount(0)
                     .WithIterationCount(1)
                     .WithInvocationCount(1)
                     .WithToolchain(InProcessEmitToolchain.Instance)
                     .DontEnforcePowerPlan());

    BenchmarkRunner.Run<CliFrameworkBenchmark>(config, args);
  }
}
