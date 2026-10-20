#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression for kanban 508-013: [Operation] endpoints declared in a referenced assembly.
// Map<T>() on a referenced type and [assembly: NuruEndpointAssembly(typeof(X))] opt in;
// a referenced assembly adds no route by itself.
#endregion

#region Design
// Roslyn-hosted: the contracts library is a real emitted DLL, so the generator sees only
// metadata symbols (no initializer syntax, no private members). Each case also checks that the
// generated code compiles, which catches an inaccessible type or member in the emitted code.
// One case references the library as a CompilationReference, the IDE project-reference shape,
// where symbols carry syntax from another compilation.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen59ReferencedOperationEndpoints
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Emit;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ReferencedOperationEndpointTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ReferencedOperationEndpointTests>();

    private const string Library = """
      using System.ComponentModel;
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Contracts;
      using TimeWarp.Mediator;
      using TimeWarp.Nuru;

      namespace RefContracts;

      [Operation("ref-add", Description = "Add two numbers")]
      public sealed class RefAddQuery : IQuery<int>
      {
        [Description("Left operand")]
        public required int X { get; set; }

        public int Y { get; set; } = 5;

        [DefaultValue(3)]
        public int Precision { get; init; } = 3;

        [DefaultValue(true)]
        public bool Round { get; set; } = true;

        public bool Verbose { get; set; }

        public string? Label { get; set; }

        public sealed class Handler : IQueryHandler<RefAddQuery, int>
        {
          public Task<int> Handle(RefAddQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(query.X + query.Y);
        }
      }

      [Operation("ref-hidden")]
      public sealed class RefHiddenHandlerQuery : IQuery<string>
      {
        internal sealed class Handler : IQueryHandler<RefHiddenHandlerQuery, string>
        {
          public Task<string> Handle(RefHiddenHandlerQuery query, CancellationToken cancellationToken) =>
            Task.FromResult("hidden");
        }
      }

      [Operation("ref-record")]
      public sealed record RefRecordQuery(string Name, int Count = 7) : IQuery<string>;

      public sealed class RefRecordHandler : IQueryHandler<RefRecordQuery, string>
      {
        public Task<string> Handle(RefRecordQuery query, CancellationToken cancellationToken) =>
          Task.FromResult(query.Name);
      }

      [Operation("ref-note")]
      public sealed record RefNoteQuery(string Title) : IQuery<string>
      {
        public RefNoteQuery() : this("untitled") { }
      }

      public sealed class RefNoteHandler : IQueryHandler<RefNoteQuery, string>
      {
        public Task<string> Handle(RefNoteQuery query, CancellationToken cancellationToken) =>
          Task.FromResult(query.Title);
      }

      [Operation("ref-internal")]
      internal sealed class RefInternalQuery : IQuery<string>
      {
        public sealed class Handler : IQueryHandler<RefInternalQuery, string>
        {
          public Task<string> Handle(RefInternalQuery query, CancellationToken cancellationToken) =>
            Task.FromResult("internal");
        }
      }

      [Operation("ref-not-an-endpoint")]
      public sealed class RefNoKind
      {
      }

      public static class RefMarker
      {
      }
      """;

    private static readonly Lazy<MetadataReference> LibraryReference = new(() => EmitLibrary(Library, "RefContractsLib"));

    private static readonly Lazy<MetadataReference> LibraryCompilationReference =
      new(() => CompileLibrary(Library, "RefContractsLib").ToMetadataReference());

    public static async Task Should_extract_a_referenced_operation_mapped_with_map_of_t()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;
        using RefContracts;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<RefAddQuery>()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"ref-add\"");
      output.Generated.ShouldContain("global::RefContracts.RefAddQuery");
      output.Generated.ShouldContain("global::RefContracts.RefAddQuery.Handler");
      output.Generated.ShouldContain("Add two numbers");
      output.Generated.ShouldContain("Left operand");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Metadata has no initializer syntax. An omitted member keeps its compiled initializer,
    /// [DefaultValue] feeds capabilities only, and `required` still makes an option required.
    /// </summary>
    public static async Task Should_keep_compiled_initializers_for_referenced_defaults()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;
        using RefContracts;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<RefAddQuery>()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();

      // Omitted members are assigned only when supplied, so the compiled initializer stands.
      output.Generated.ShouldContain("__command.Y = ");
      output.Generated.ShouldNotContain("Y = 5");
      output.Generated.ShouldContain("Precision = ");
      output.Generated.ShouldContain("__commandDefaults.Precision");

      // Capabilities: [DefaultValue] shows, an unknown initializer does not.
      output.Generated.ShouldContain("\"3\"");
      output.Generated.ShouldNotContain("\"5\"");

      // [DefaultValue(true)] bool is a value option, not a flag that could only set true.
      output.Generated.ShouldContain("--Round");
      await Task.CompletedTask;
    }

    public static async Task Should_take_a_referenced_record_parameter_default()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;
        using RefContracts;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<RefRecordQuery>()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"ref-record\"");
      output.Generated.ShouldContain("new(");
      output.Generated.ShouldContain("\"7\"");
      await Task.CompletedTask;
    }

    /// <summary>
    /// An internal nested Handler in another assembly cannot be named, so the endpoint falls back
    /// to handler lookup and reports NURU_H008 at the Map&lt;T&gt;() call.
    /// </summary>
    public static async Task Should_not_emit_an_inaccessible_nested_handler()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;
        using RefContracts;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<RefHiddenHandlerQuery>()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"ref-hidden\"");
      output.Generated.ShouldNotContain("RefHiddenHandlerQuery.Handler");
      output.Diagnostics.Any(static d => d.Id == "NURU_H008").ShouldBeTrue();
      await Task.CompletedTask;
    }

    /// <summary>
    /// A record parameter with no default is required only when the generated code calls its
    /// constructor. With a parameterless constructor the member is optional and the chained
    /// constructor's value stands.
    /// </summary>
    public static async Task Should_require_a_record_parameter_only_on_the_called_constructor()
    {
      Output record = Generate(MapApp("RefRecordQuery"));

      record.Errors.ShouldBeEmpty();
      record.Generated.ShouldContain("ref-record --Name {name} [--Count {count}]");
      record.Generated.ShouldContain("Missing required value 'Name'");

      foreach (Output note in new[] { Generate(MapApp("RefNoteQuery")), Generate(MapApp("RefNoteQuery"), LibraryCompilationReference.Value) })
      {
        note.Errors.ShouldBeEmpty();
        note.Generated.ShouldContain("ref-note [--Title {title}]");
        note.Generated.ShouldContain("__commandDefaults.Title");
        note.Generated.ShouldNotContain("Missing required value 'Title'");
      }

      await Task.CompletedTask;
    }

    /// <summary>
    /// An IDE project reference is a CompilationReference: attribute syntax exists but belongs to
    /// another compilation. Diagnostics use the Map&lt;T&gt;() call, and the generator does not throw.
    /// </summary>
    public static async Task Should_report_at_the_map_call_for_a_compilation_reference()
    {
      Output output = Generate(MapApp("RefHiddenHandlerQuery"), LibraryCompilationReference.Value);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"ref-hidden\"");
      Diagnostic h008 = output.Diagnostics.Single(static d => d.Id == "NURU_H008");
      h008.Location.SourceTree?.FilePath.ShouldBe("app.cs");
      await Task.CompletedTask;
    }

    private static string MapApp(string typeName) => $$"""
      using TimeWarp.Nuru;
      using RefContracts;

      NuruApp app = NuruApp.CreateBuilder()
        .Map<{{typeName}}>()
        .Build();

      return await app.RunAsync(args);
      """;

    public static async Task Should_discover_a_named_assembly_with_discover_endpoints()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;
        using RefContracts;

        [assembly: NuruEndpointAssembly(typeof(RefMarker))]

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"ref-add\"");
      output.Generated.ShouldContain("\"ref-record\"");
      output.Generated.ShouldContain("\"ref-hidden\"");
      output.Generated.ShouldNotContain("ref-internal");
      output.Generated.ShouldNotContain("ref-not-an-endpoint");
      await Task.CompletedTask;
    }

    public static async Task Should_not_discover_a_referenced_assembly_without_opt_in()
    {
      Output output = Generate("""
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Map("local").WithHandler(static () => "local").AsQuery().Done()
          .Build();

        return await app.RunAsync(args);
        """);

      output.Errors.ShouldBeEmpty();
      output.Generated.ShouldContain("\"local\"");
      output.Generated.ShouldNotContain("ref-add");
      output.Generated.ShouldNotContain("RefContracts");
      await Task.CompletedTask;
    }

    /// <summary>
    /// A type found only through Map&lt;T&gt;() belongs to that app. Another app's
    /// DiscoverEndpoints() in the same compilation does not pick it up.
    /// </summary>
    public static async Task Should_keep_a_mapped_referenced_endpoint_out_of_discovery()
    {
      Output discovering = Generate(TwoApps(".DiscoverEndpoints()"));
      Output plain = Generate(TwoApps(""));

      discovering.Errors.ShouldBeEmpty();
      plain.Errors.ShouldBeEmpty();
      discovering.Generated.ShouldContain("\"local\"");

      // Only the mapping app routes ref-add, so the count matches an app that never discovers.
      CountOccurrences(discovering.Generated, "__JsonRouteWords_").ShouldBe(CountOccurrences(plain.Generated, "__JsonRouteWords_"));
      CountOccurrences(discovering.Generated, "[\"ref-add\"]").ShouldBe(1);
      await Task.CompletedTask;
    }

    private static string TwoApps(string discovery) => $$"""
      using System.Threading.Tasks;
      using TimeWarp.Nuru;
      using RefContracts;

      return await Apps.Mapped(args) + await Apps.Other(args);

      public static class Apps
      {
        public static async Task<int> Mapped(string[] args)
        {
          NuruApp app = NuruApp.CreateBuilder()
            .Map<RefAddQuery>()
            .Build();
          return await app.RunAsync(args);
        }

        public static async Task<int> Other(string[] args)
        {
          NuruApp app = NuruApp.CreateBuilder()
            {{discovery}}
            .Map("local").WithHandler(static () => "local").AsQuery().Done()
            .Build();
          return await app.RunAsync(args);
        }
      }
      """;

    private static int CountOccurrences(string text, string value)
    {
      int count = 0;
      int index = text.IndexOf(value, StringComparison.Ordinal);
      while (index >= 0)
      {
        count++;
        index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
      }

      return count;
    }

    private sealed record Output(string Generated, ImmutableArray<Diagnostic> Diagnostics, string[] Errors);

    private static Output Generate(string app, MetadataReference? library = null)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(app, path: "app.cs");
      List<MetadataReference> references = BuildReferences();
      references.Add(library ?? LibraryReference.Value);

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen59App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      GeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator())
        .RunGeneratorsAndUpdateCompilation(compilation, out Compilation updated, out ImmutableArray<Diagnostic> generatorDiagnostics);
      GeneratorDriverRunResult result = driver.GetRunResult();
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      string generated = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(static g => g.SourceText.ToString()));

      // CS9137 is this harness: it does not enable the interceptors feature.
      string[] errors =
      [
        .. generatorDiagnostics.Concat(updated.GetDiagnostics())
          .Where(static d => d.Severity == DiagnosticSeverity.Error && d.Id != "CS9137")
          .Select(static d => d.ToString())
      ];

      return new Output(generated, generatorDiagnostics, errors);
    }

    // One shared set, so a CompilationReference library binds to the same framework assemblies
    // as the app instead of being retargeted.
    private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences = new(static () =>
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      return
      [
        .. tpa.Split(Path.PathSeparator)
          .Where(static path => path.Length > 0)
          .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
      ];
    });

    private static List<MetadataReference> BuildReferences() => [.. PlatformReferences.Value];

    private static CSharpCompilation CompileLibrary(string source, string assemblyName)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: assemblyName + ".cs");
      return CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [tree],
        references: BuildReferences(),
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static PortableExecutableReference EmitLibrary(string source, string assemblyName)
    {
      CSharpCompilation compilation = CompileLibrary(source, assemblyName);

      using MemoryStream stream = new();
      EmitResult emitResult = compilation.Emit(stream);
      emitResult.Success.ShouldBeTrue(
        string.Join("\n", emitResult.Diagnostics.Select(static d => d.ToString())));

      stream.Position = 0;
      return MetadataReference.CreateFromStream(stream);
    }
  }
}
