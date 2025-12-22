namespace TimeWarp.Nuru.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
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

    // Create a custom toolchain for .NET 10
    IToolchain net10Toolchain = CsProjCoreToolchain.From(
      new NetCoreAppSettings(
        targetFrameworkMoniker: "net10.0",
        runtimeFrameworkVersion: null,
        name: ".NET 10.0"
      )
    );

    config = config.AddJob(Job.Default
                     .WithStrategy(RunStrategy.ColdStart)
                     .WithLaunchCount(5)
                     .WithWarmupCount(0)
                     .WithIterationCount(1)
                     .WithInvocationCount(1)
                     .WithToolchain(net10Toolchain)
                     .DontEnforcePowerPlan());

    // Check for --cost flag to run the cost matrix benchmark
    if (args.Length > 0 && args[0] == "--cost")
    {
      // Run cost benchmark, passing remaining args to BenchmarkDotNet
      string[] remainingArgs = args.Length > 1 ? args[1..] : [];
      BenchmarkRunner.Run<NuruBuilderCostBenchmark>(config, remainingArgs);
    }
    else
    {
      // Default: run CLI framework comparison benchmark
      BenchmarkRunner.Run<CliFrameworkBenchmark>(config, args);
    }
  }
}
