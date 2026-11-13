#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 573: the TimeWarp.Mediator generator registers a nested [NuruRoute] Handler as
// AddScoped<Handler>(services), so no `new Handler(...)` appears and an internal handler trips
// CA1812 in a consumer (TimeWarp.Nuru.DevCli ships its endpoints as source). Pins that the Nuru
// generator swaps each such registration for an explicit constructor factory, and skips the
// handlers the container could construct differently.
#endregion

#region Design
// Roslyn-hosted like generator-72, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. Only the Nuru generator runs, so the assertions read the generated text; the
// generated mediator's AddGeneratedMediator is not in this compilation. The CA1812 outcome itself
// is gated by tests/devcli-consumer-check, which compiles the DevCli content with CA1812 on.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen73NestedHandlerFactories
{
  using System.Collections.Generic;
  using System.Linq;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Diagnostics;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class NestedHandlerFactoryTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NestedHandlerFactoryTests>();

    private const string Resolve = "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService";

    private const string Source = """
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Mediator;
      using TimeWarp.Nuru;
      using TimeWarp.Terminal;

      NuruApp app = NuruApp.CreateBuilder()
        .Map<T573Simple>()
        .Map<T573NoDependencies>()
        .Map<T573TwoConstructors>()
        .Map<T573OptionalArgument>()
        .Build();

      await app.RunAsync([]);

      [NuruRoute("t573-simple")]
      internal sealed class T573Simple : ICommand<Unit>
      {
        internal sealed class Handler : IRequestHandler<T573Simple, Unit>
        {
          private readonly ITerminal Terminal;

          public Handler(ITerminal terminal, IMediator mediator)
          {
            Terminal = terminal;
            _ = mediator;
          }

          public ValueTask<Unit> Handle(T573Simple command, CancellationToken cancellationToken)
          {
            Terminal.WriteLine("simple");
            return ValueTask.FromResult(Unit.Value);
          }
        }
      }

      [NuruRoute("t573-none")]
      internal sealed class T573NoDependencies : ICommand<Unit>
      {
        internal sealed class Handler : IRequestHandler<T573NoDependencies, Unit>
        {
          public ValueTask<Unit> Handle(T573NoDependencies command, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Unit.Value);
        }
      }

      [NuruRoute("t573-two")]
      internal sealed class T573TwoConstructors : ICommand<Unit>
      {
        internal sealed class Handler : IRequestHandler<T573TwoConstructors, Unit>
        {
          public Handler() { }

          public Handler(ITerminal terminal) { _ = terminal; }

          public ValueTask<Unit> Handle(T573TwoConstructors command, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Unit.Value);
        }
      }

      [NuruRoute("t573-optional")]
      internal sealed class T573OptionalArgument : ICommand<Unit>
      {
        internal sealed class Handler : IRequestHandler<T573OptionalArgument, Unit>
        {
          public Handler(ITerminal terminal, int retries = 3) { _ = terminal; _ = retries; }

          public ValueTask<Unit> Handle(T573OptionalArgument command, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Unit.Value);
        }
      }
      """;

    public static async Task Should_register_a_single_constructor_handler_with_an_explicit_factory()
    {
      string generated = Generate(mediatorGenerator: true);

      generated.ShouldContain("private static void __UseHandlerFactory<THandler>");
      generated.ShouldContain(
        "__UseHandlerFactory<global::T573Simple.Handler>(services, static sp => new global::T573Simple.Handler(" +
        $"{Resolve}<global::TimeWarp.Terminal.ITerminal>(sp), {Resolve}<global::TimeWarp.Mediator.IMediator>(sp)));");
      generated.ShouldContain(
        "__UseHandlerFactory<global::T573NoDependencies.Handler>(services, static _ => new global::T573NoDependencies.Handler());");
      await Task.CompletedTask;
    }

    public static async Task Should_register_the_factories_after_the_generated_mediator()
    {
      string generated = Generate(mediatorGenerator: true);

      int mediator = generated.IndexOf("AddGeneratedMediator(services);", StringComparison.Ordinal);
      int factory = generated.IndexOf("__UseHandlerFactory<global::T573Simple.Handler>(services", StringComparison.Ordinal);
      mediator.ShouldBeGreaterThan(-1);
      factory.ShouldBeGreaterThan(mediator);
      await Task.CompletedTask;
    }

    public static async Task Should_leave_handlers_the_container_could_construct_differently()
    {
      string generated = Generate(mediatorGenerator: true);

      generated.ShouldNotContain("__UseHandlerFactory<global::T573TwoConstructors.Handler>");
      generated.ShouldNotContain("__UseHandlerFactory<global::T573OptionalArgument.Handler>");
      await Task.CompletedTask;
    }

    public static async Task Should_emit_no_factories_without_a_generated_mediator()
    {
      string generated = Generate(mediatorGenerator: false);

      generated.ShouldNotContain("__UseHandlerFactory");
      await Task.CompletedTask;
    }

    private static string Generate(bool mediatorGenerator)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(Source);
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen73NestedHandlerFactories",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      Dictionary<string, string> config = new()
      {
        ["build_property.TimeWarpMediatorGenerator"] = mediatorGenerator ? "true" : "false"
      };

      GeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator())
        .WithUpdatedAnalyzerConfigOptions(new MapConfig(config))
        .RunGenerators(compilation);

      return string.Join(
        "\n",
        driver.GetRunResult().Results
          .SelectMany(result => result.GeneratedSources)
          .Select(source => source.SourceText.ToString()));
    }

    private sealed class MapConfig : AnalyzerConfigOptionsProvider
    {
      private readonly MapOptions Options;

      public MapConfig(IReadOnlyDictionary<string, string> values)
      {
        Options = new MapOptions(values);
      }

      public override AnalyzerConfigOptions GlobalOptions => Options;

      public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Options;

      public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Options;

      private sealed class MapOptions : AnalyzerConfigOptions
      {
        private readonly IReadOnlyDictionary<string, string> Values;

        public MapOptions(IReadOnlyDictionary<string, string> values)
        {
          Values = values;
        }

        public override bool TryGetValue(string key, out string value) =>
          Values.TryGetValue(key, out value!);
      }
    }
  }
}
