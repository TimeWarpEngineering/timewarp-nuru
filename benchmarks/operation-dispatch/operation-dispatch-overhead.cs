#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

// In-process cost of mediator dispatch versus UseDirectInvoke.
// Each path is its own method: two Build() calls in one method are intercepted twice.
// RunAsync sits on the field Build() assigns. A delegate route runs first so shared
// Nuru code is JIT-compiled before either first call; each first call then holds only
// its own path's JIT. Per-call cost is nanoseconds after warm-up.
// Startup is the --aot comparison: two native binaries, three alternating cold starts.

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Amuru;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

const int Warmup = 50;
const int Iterations = 2000;

await OverheadBench.PrimeSharedAsync();
OverheadBench.Sample mediator = await OverheadBench.MeasureMediatorAsync(Warmup, Iterations);
OverheadBench.Sample direct = await OverheadBench.MeasureDirectAsync(Warmup, Iterations);
long mediatorPerCall = OverheadBench.ToNanoseconds(mediator.LoopTicks) / Iterations;
long directPerCall = OverheadBench.ToNanoseconds(direct.LoopTicks) / Iterations;
string decision = directPerCall <= mediatorPerCall ? "keep-UseDirectInvoke" : "drop-UseDirectInvoke";

Console.WriteLine($"warmup={Warmup.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"iterations={Iterations.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"mediator-first-us={OverheadBench.ToMicroseconds(mediator.FirstTicks).ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"direct-first-us={OverheadBench.ToMicroseconds(direct.FirstTicks).ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"mediator-per-call-ns={mediatorPerCall.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"direct-per-call-ns={directPerCall.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"decision={decision}");

if (args.Contains("--aot"))
{
  await OverheadBench.MeasureAotAsync();
}

return 0;

internal static class OverheadBench
{
  private static NuruApp PrimeApp = null!;
  private static NuruApp MediatorApp = null!;
  private static NuruApp DirectApp = null!;

  internal readonly record struct Sample(long FirstTicks, long LoopTicks);

  public static long ToMicroseconds(long ticks) =>
    ticks * 1_000_000 / Stopwatch.Frequency;

  public static long ToNanoseconds(long ticks) =>
    (long)(ticks * (1_000_000_000.0 / Stopwatch.Frequency));

  // Uses neither dispatch path, so it warms routing and the terminal for both.
  public static async Task PrimeSharedAsync()
  {
    PrimeApp = NuruApp.CreateBuilder()
      .Map("prime").WithHandler(() => { })
      .AsCommand().Done()
      .Build();

    int code = await PrimeApp.RunAsync(["prime"]);
    if (code != 0)
    {
      throw new InvalidOperationException("Prime call failed.");
    }
  }

  public static async Task<Sample> MeasureMediatorAsync(int warmup, int iterations)
  {
    string[] route = ["overhead"];
    MediatorApp = NuruApp.CreateBuilder()
      .Map<OverheadQuery>()
      .Build();

    Stopwatch first = Stopwatch.StartNew();
    int firstCode = await MediatorApp.RunAsync(route);
    first.Stop();
    if (firstCode != 0)
    {
      throw new InvalidOperationException("Mediator first call failed.");
    }

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
    return new Sample(first.ElapsedTicks, watch.ElapsedTicks);
  }

  public static async Task<Sample> MeasureDirectAsync(int warmup, int iterations)
  {
    string[] route = ["overhead"];
    DirectApp = NuruApp.CreateBuilder()
      .UseDirectInvoke()
      .Map<OverheadQuery>()
      .Build();

    Stopwatch first = Stopwatch.StartNew();
    int firstCode = await DirectApp.RunAsync(route);
    first.Stop();
    if (firstCode != 0)
    {
      throw new InvalidOperationException("Direct first call failed.");
    }

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
    return new Sample(first.ElapsedTicks, watch.ElapsedTicks);
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

    List<long> mediatorCold = [];
    List<long> directCold = [];
    string mediatorExe = Path.Combine(mediatorOut, "mediator");
    string directExe = Path.Combine(directOut, "direct");
    for (int sample = 0; sample < 3; sample++)
    {
      if (sample % 2 == 0)
      {
        mediatorCold.Add(await ColdStartAsync(mediatorExe));
        directCold.Add(await ColdStartAsync(directExe));
      }
      else
      {
        directCold.Add(await ColdStartAsync(directExe));
        mediatorCold.Add(await ColdStartAsync(mediatorExe));
      }
    }

    Console.WriteLine($"aot-mediator-cold-ms={string.Join(',', mediatorCold)}");
    Console.WriteLine($"aot-direct-cold-ms={string.Join(',', directCold)}");
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

    return watch.ElapsedTicks * 1000 / Stopwatch.Frequency;
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
