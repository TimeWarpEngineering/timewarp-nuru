#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-012 (M8): IsServiceType must be decided via the
// ITypeSymbol overload (TypeKind.Interface + known-service checks) at the two symbol-based
// call sites in handler-extractor.cs (ExtractFromMethodSymbol / ExtractFromMethodSymbolAsMethod),
// not the old "shortName[0]=='I' && char.IsUpper(shortName[1])" name heuristic.
// A user-defined service interface (IData) must still bind as service-provider injection,
// while an ordinary route parameter (string name) in the same handler must still bind
// as a route parameter.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen35IDataService
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class IDataServiceTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<IDataServiceTests>();

    private static GeneratorDriverRunResult RunNuruGenerator(string source)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "IDataServiceRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// (IData svc, string name): IData is a user interface and must bind as a service
    /// (service-provider injection), while "name" still binds as a route parameter.
    /// If IData were misclassified as a route parameter, extraction would emit NURU_H005
    /// (no route segment named "svc") since the route pattern only has {name}.
    /// </summary>
    public static async Task Should_bind_user_interface_as_service_and_keep_route_parameter()
    {
      const string Source = """
        using TimeWarp.Nuru;
        using Microsoft.Extensions.DependencyInjection;

        public interface IData
        {
          string Get();
        }

        public sealed class DataImpl : IData
        {
          public string Get() => "data";
        }

        NuruApp app = NuruApp.CreateBuilder(args)
          .ConfigureServices(services => services.AddSingleton<IData, DataImpl>())
          .Map("m8-idata {name}")
            .WithHandler((IData svc, string name) => $"{name}: {svc.Get()}")
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      // Bug repro: if IData were misclassified as a route parameter, this would fire
      // (the route pattern has no {svc} segment).
      Diagnostic[] h005 = [.. result.Diagnostics.Where(d => d.Id == "NURU_H005")];
      h005.ShouldBeEmpty();

      Diagnostic[] errors = [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)];
      errors.ShouldBeEmpty();

      string generatedSource = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));

      // Service injection: svc is bound from the statically-resolved DataImpl instance,
      // typed as the service interface, not converted from a positional route argument.
      generatedSource.ShouldContain("global::IData svc = __svc_DataImpl;");

      // Route parameter: name is still bound positionally from routeArgs.
      generatedSource.ShouldContain("string name = __positionalArgs_0[1];");

      await Task.CompletedTask;
    }
  }
}
