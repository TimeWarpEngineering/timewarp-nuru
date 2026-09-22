#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression for kanban 470-005: Map<T>, Implements<T>, AddBehavior(typeof), and
// AddSingleton/AddHttpClient generic arguments must resolve types that live in a
// referenced assembly (GetSymbolInfo, then GetTypeInfo; reject TypeKind.Error).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen44ReferencedTypeArguments
{
  using System.Globalization;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Emit;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ReferencedTypeArgumentTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ReferencedTypeArgumentTests>();

    private static List<MetadataReference> BuildReferences()
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
        references.Add(MetadataReference.CreateFromFile(path));

      return references;
    }

    private static GeneratorDriverRunResult RunNuruGenerator(string source, params MetadataReference[] extraReferences)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "app.cs");
      List<MetadataReference> references = BuildReferences();
      references.AddRange(extraReferences);

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen44App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    private static PortableExecutableReference EmitLibrary(string source, string assemblyName)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: assemblyName + ".cs");
      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [tree],
        references: BuildReferences(),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      string path = Path.Combine(Path.GetTempPath(), $"{assemblyName}-{Guid.NewGuid():N}.dll");
      EmitResult emitResult;
      using (FileStream fileStream = new(path, FileMode.Create, FileAccess.Write))
      {
        emitResult = compilation.Emit(fileStream);
      }

      emitResult.Success.ShouldBeTrue(
        string.Join("\n", emitResult.Diagnostics.Select(static d => d.ToString())));

      return MetadataReference.CreateFromFile(path);
    }

    private const string Library = """
      using System;
      using System.Net.Http;
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      namespace M7Symbols;

      [NuruRoute("m7-lib")]
      public sealed class M7Command : ICommand<Unit>
      {
        public sealed class Handler : ICommandHandler<M7Command, Unit>
        {
          public ValueTask<Unit> Handle(M7Command command, CancellationToken cancellationToken)
          {
            _ = command;
            _ = cancellationToken;
            return ValueTask.FromResult<Unit>(default);
          }
        }
      }

      public sealed class M7Behavior : INuruBehavior
      {
        public ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed) => proceed();
      }

      public interface IM7Filter
      {
        string Token { get; set; }
      }

      public interface IM7Service;

      public sealed class M7Service : IM7Service;

      public interface IM7Client;

      public sealed class M7Client : IM7Client
      {
        public M7Client(HttpClient client)
        {
          _ = client;
        }
      }
      """;

    /// <summary>
    /// Types defined only in a referenced assembly must be emitted fully qualified.
    /// AddSingleton and AddHttpClient are left unbound (no DependencyInjection using)
    /// so extraction takes the syntactic type-argument path.
    /// </summary>
    public static async Task Should_emit_fully_qualified_names_for_referenced_type_arguments()
    {
      MetadataReference library = EmitLibrary(Library, "M7SymbolsLib");

      const string App = """
        using TimeWarp.Nuru;
        using M7Symbols;

        NuruApp app = NuruApp.CreateBuilder()
          .AddBehavior(typeof(M7Behavior))
          .ConfigureServices(static services =>
          {
            services.AddSingleton<IM7Service, M7Service>();
            services.AddHttpClient<IM7Client, M7Client>(static client => client.Timeout = System.TimeSpan.FromSeconds(5));
          })
          .Map<M7Command>()
          .Map("m7-filter {token}")
            .Implements<IM7Filter>(static x => x.Token = "ok")
            .WithHandler(static (string token) => token)
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App, library);
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] typeArgumentFailures =
      [
        .. result.Diagnostics.Where(static d =>
          d.Id == "NURU_S999" &&
          d.GetMessage(CultureInfo.InvariantCulture).Contains("requires a type argument", StringComparison.Ordinal))
      ];
      typeArgumentFailures.ShouldBeEmpty(
        string.Join("\n", result.Diagnostics.Select(static d => d.ToString())));

      string generated = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));

      generated.ShouldContain("global::M7Symbols.M7Behavior");
      generated.ShouldContain("global::M7Symbols.IM7Filter");
      generated.ShouldContain("global::M7Symbols.M7Service");
      generated.ShouldContain("__httpClient_IM7Client");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A Map type argument that does not exist must not be emitted as a broken name.
    /// </summary>
    public static async Task Should_not_emit_unresolvable_map_type_argument()
    {
      const string App = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<M7Missing.DoesNotExist>()
          .Map("m7-ok {value}")
            .WithHandler(static (string value) => value)
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync([]);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(App);
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] typeArgumentFailures =
      [
        .. result.Diagnostics.Where(static d =>
          d.Id == "NURU_S999" &&
          d.GetMessage(CultureInfo.InvariantCulture).Contains("requires a type argument", StringComparison.Ordinal))
      ];
      typeArgumentFailures.Length.ShouldBe(1);

      string generated = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));
      generated.ShouldNotContain("DoesNotExist");

      await Task.CompletedTask;
    }
  }
}
