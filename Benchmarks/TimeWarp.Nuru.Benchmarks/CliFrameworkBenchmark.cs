namespace TimeWarp.Nuru.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliFx;
using Cocona;
using ConsoleAppFramework;
using Spectre.Console.Cli;
using TimeWarp.Nuru.Benchmarks.Commands;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
public class CliFrameworkBenchmark
{
  private static readonly string[] Arguments = ["--str", "hello world", "-i", "13", "-b"];

  [Benchmark(Description = "TimeWarp.Nuru")]
  public async Task ExecuteWithNuru()
  {
    await NuruMediatorCommand.Execute(Arguments);
  }

  [Benchmark(Description = "TimeWarp.Nuru.Direct")]
  public async Task ExecuteWithNuruDirect()
  {
    await NuruDirectCommand.Execute(Arguments);
  }

  [Benchmark(Description = "Cocona.Lite")]
  public void ExecuteWithCoconaLite()
  {
    CoconaLiteApp.Run<CoconaCommand>(Arguments);
  }

  [Benchmark(Description = "Cocona")]
  public void ExecuteWithCocona()
  {
    CoconaApp.Run<CoconaCommand>(Arguments);
  }

  [Benchmark(Description = "ConsoleAppFramework v5", Baseline = true)]
  public unsafe void ExecuteConsoleAppFramework()
  {
    ConsoleApp.Run(Arguments, &ConsoleAppFrameworkCommand.Execute);
  }

  [Benchmark(Description = "CliFx")]
  public ValueTask<int> ExecuteWithCliFx()
  {
    return new CliApplicationBuilder().AddCommand(typeof(CliFxCommand)).Build().RunAsync(Arguments);
  }

  [Benchmark(Description = "System.CommandLine")]
  public int ExecuteWithSystemCommandLine()
  {
    return SystemCommandLineCommand.Execute(Arguments);
  }

  [Benchmark(Description = "Spectre.Console.Cli")]
  public void ExecuteSpectreConsoleCli()
  {
    var app = new CommandApp<SpectreConsoleCliCommand>();
    app.Run(Arguments);
  }

  [Benchmark(Description = "McMaster.Extensions.CommandLineUtils")]
  public int ExecuteWithMcMaster()
  {
    return McMaster.Extensions.CommandLineUtils.CommandLineApplication.Execute<McMasterCommand>(Arguments);
  }

  [Benchmark(Description = "CommandLineParser")]
  public void ExecuteWithCommandLineParser()
  {
    CommandLine.ParserResult<CommandLineParserCommand> result = new CommandLine.Parser()
        .ParseArguments<CommandLineParserCommand>(Arguments);

    if (result is CommandLine.Parsed<CommandLineParserCommand> parsed)
    {
      parsed.Value.Execute();
    }
  }

  [Benchmark(Description = "PowerArgs")]
  public void ExecuteWithPowerArgs()
  {
    // PowerArgs uses Windows-style /name:value format by default
    string[] powerArgsArguments = ["/str:hello world", "/int:13", "/bool"];
    PowerArgs.Args.InvokeMain<PowerArgsCommand>(powerArgsArguments);
  }
}
