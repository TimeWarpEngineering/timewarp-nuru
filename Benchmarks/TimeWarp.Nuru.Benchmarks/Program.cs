using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using Perfolizer.Horology;

namespace TimeWarp.Nuru.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
                                  .WithSummaryStyle(SummaryStyle.Default
                                  .WithTimeUnit(TimeUnit.Millisecond))
                                  .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        config = config.AddDiagnoser(MemoryDiagnoser.Default);
        config = config.AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayLockContentionWhenZero: false, displayCompletedWorkItemCountWhenZero: false)));

        config = config.AddJob(Job.Default
                         .WithStrategy(RunStrategy.ColdStart)
                         .WithLaunchCount(1)
                         .WithWarmupCount(0)
                         .WithIterationCount(1)
                         .WithInvocationCount(1)
                         .WithToolchain(CsProjCoreToolchain.NetCoreApp90)
                         .DontEnforcePowerPlan());

        BenchmarkRunner.Run<CliFrameworkBenchmark>(config, args);
    }
}