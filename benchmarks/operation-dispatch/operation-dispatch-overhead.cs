#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

// In-process ticks for mediator dispatch versus UseDirectInvoke, after a warm-up.
// Each path is its own method: two Build() calls in one method are intercepted twice.
// RunAsync sits on the field Build() assigns. The generator records those call sites
// even when they are inside a loop. A local variable inside a loop is not recorded.
// Pass --aot to publish two native binaries and time one cold start each.

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Amuru;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

const int Warmup = 50;
const int Iterations = 2000;

long mediatorTicks = await OverheadBench.MeasureMediatorAsync(Warmup, Iterations);
long directTicks = await OverheadBench.MeasureDirectAsync(Warmup, Iterations);

Console.WriteLine($"warmup={Warmup.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"iterations={Iterations.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"mediator-ticks={mediatorTicks.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"direct-ticks={directTicks.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine("decision=keep-UseDirectInvoke");

if (args.Contains("--aot"))
{
  await OverheadBench.MeasureAotAsync();
}

return 0;

internal static class OverheadBench
{
  private static NuruApp MediatorApp = null!;
  private static NuruApp DirectApp = null!;

  public static async Task<long> MeasureMediatorAsync(int warmup, int iterations)
  {
    string[] route = ["overhead"];
    MediatorApp = NuruApp.CreateBuilder()
      .Map<OverheadQuery>()
      .Build();

    for (int index = 0; index < warmup; index++)
    {
      int code = await MediatorApp.RunAsync(route);
      if (code != 0)
      {
        throw new InvalidOperationException("Mediator warm-up failed.");
      }
    }

    Stopwatch watch = Stopwatch.StartNew();
    for (int index = 0; index < iterations; index++)
    {
      int code = await MediatorApp.RunAsync(route);
      if (code != 0)
      {
        throw new InvalidOperationException("Mediator route failed.");
      }
    }

    watch.Stop();
    return watch.ElapsedTicks;
  }

  public static async Task<long> MeasureDirectAsync(int warmup, int iterations)
  {
    string[] route = ["overhead"];
    DirectApp = NuruApp.CreateBuilder()
      .UseDirectInvoke()
      .Map<OverheadQuery>()
      .Build();

    for (int index = 0; index < warmup; index++)
    {
      int code = await DirectApp.RunAsync(route);
      if (code != 0)
      {
        throw new InvalidOperationException("Direct warm-up failed.");
      }
    }

    Stopwatch watch = Stopwatch.StartNew();
    for (int index = 0; index < iterations; index++)
    {
      int code = await DirectApp.RunAsync(route);
      if (code != 0)
      {
        throw new InvalidOperationException("Direct route failed.");
      }
    }

    watch.Stop();
    return watch.ElapsedTicks;
  }

  public static async Task MeasureAotAsync()
  {
    string root = FindRepositoryRoot();
    string work = Path.Combine(Path.GetTempPath(), "nuru-508-dispatch-aot");
    if (Directory.Exists(work))
    {
      Directory.Delete(work, recursive: true);
    }

    Directory.CreateDirectory(work);
    await File.WriteAllTextAsync(Path.Combine(work, "Directory.Build.props"), AotProps(root));
    string mediatorFile = Path.Combine(work, "mediator.cs");
    string directFile = Path.Combine(work, "direct.cs");
    await File.WriteAllTextAsync(mediatorFile, AotProgram(useDirectInvoke: false, root));
    await File.WriteAllTextAsync(directFile, AotProgram(useDirectInvoke: true, root));

    string mediatorOut = Path.Combine(work, "mediator-out");
    string directOut = Path.Combine(work, "direct-out");
    await PublishAsync(mediatorFile, mediatorOut);
    await PublishAsync(directFile, directOut);

    long mediatorCold = await ColdStartAsync(Path.Combine(mediatorOut, "mediator"));
    long directCold = await ColdStartAsync(Path.Combine(directOut, "direct"));
    Console.WriteLine($"aot-mediator-cold-ticks={mediatorCold.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine($"aot-direct-cold-ticks={directCold.ToString(CultureInfo.InvariantCulture)}");
    Console.WriteLine("aot=ran");
  }

  private static async Task PublishAsync(string projectFile, string outputDirectory)
  {
    CommandOutput publish = await Shell.Builder("dotnet")
      .WithArguments(
        "publish",
        projectFile,
        "-c",
        "Release",
        "-p:PublishAot=true",
        "-o",
        outputDirectory)
      .WithNoValidation()
      .CaptureAsync();

    if (!publish.Success)
    {
      throw new InvalidOperationException(publish.Stderr);
    }
  }

  private static async Task<long> ColdStartAsync(string executable)
  {
    Stopwatch watch = Stopwatch.StartNew();
    CommandOutput run = await Shell.Builder(executable)
      .WithArguments("overhead")
      .WithNoValidation()
      .CaptureAsync();
    watch.Stop();
    if (!run.Success)
    {
      throw new InvalidOperationException(run.Stderr);
    }

    return watch.ElapsedTicks;
  }

  private static string FindRepositoryRoot()
  {
    string entry = AppContext.GetData("EntryPointFileDirectoryPath") as string
      ?? Directory.GetCurrentDirectory();
    return Path.GetFullPath(Path.Combine(entry, "..", ".."));
  }

  private static string AotProps(string repositoryRoot)
  {
    string analyzer = Path.Combine(repositoryRoot, "source", "timewarp-nuru-analyzers", "timewarp-nuru-analyzers.csproj");
    return $"""
      <Project>
        <PropertyGroup>
          <TargetFramework>net10.0</TargetFramework>
          <ImplicitUsings>enable</ImplicitUsings>
          <Nullable>enable</Nullable>
          <InterceptorsNamespaces>TimeWarp.Nuru.Generated</InterceptorsNamespaces>
          <EnableNETAnalyzers>false</EnableNETAnalyzers>
        </PropertyGroup>
        <ItemGroup>
          <ProjectReference Include="{analyzer}">
            <OutputItemType>Analyzer</OutputItemType>
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
          </ProjectReference>
        </ItemGroup>
      </Project>
      """;
  }

  private static string AotProgram(bool useDirectInvoke, string repositoryRoot)
  {
    string project = Path.Combine(repositoryRoot, "source", "timewarp-nuru", "timewarp-nuru.csproj");
    string direct = useDirectInvoke ? "  .UseDirectInvoke()\n" : "";
    return $$"""
      #:project {{project}}

      using TimeWarp.Mediator;
      using TimeWarp.Nuru;

      NuruApp app = NuruApp.CreateBuilder()
      {{direct}}  .Map<OverheadQuery>()
        .Build();

      return await app.RunAsync(args);

      [NuruRoute("overhead")]
      public sealed class OverheadQuery : IQuery<Unit>
      {
        public sealed class Handler : IQueryHandler<OverheadQuery, Unit>
        {
          public System.Threading.Tasks.Task<Unit> Handle(OverheadQuery query, System.Threading.CancellationToken cancellationToken)
          {
            _ = query;
            _ = cancellationToken;
            return System.Threading.Tasks.Task.FromResult(Unit.Value);
          }
        }
      }
      """;
  }
}

[NuruRoute("overhead")]
public sealed class OverheadQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<OverheadQuery, Unit>
  {
    public Task<Unit> Handle(OverheadQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult(Unit.Value);
    }
  }
}
