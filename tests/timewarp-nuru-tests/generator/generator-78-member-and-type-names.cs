#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Task 591-006: an endpoint property named Type must not splice @ into a longer local,
// and a handler that returns an enum must emit the global:: name so two same-named enums compile.
#endregion

#region Design
// Each case compiles an app in memory and runs the Nuru generator. The analyzers assembly is left
// out of the references (CS0433). The keyword case requires the generated source to keep @type as
// its own identifier and to compile. The collision case imports both namespaces and requires the
// handler signature, the result local, and the render cast to use global:: names.
// Roslyn-hosted, so the CI multi-mode assembly excludes it. Standalone runs it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen78MemberAndTypeNames
{
  using System.Collections.Immutable;
  using System.Text.RegularExpressions;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  /// <summary>
  /// Keyword member names and same-named enum return types in generated code.
  /// </summary>
  [TestTag("generator")]
  public class MemberAndTypeNameTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MemberAndTypeNameTests>();

    // A short Mode used as a type. global::Ns1.Mode is preceded by '.'. A property named Mode
    // (the route parameter) is followed by '{', so it is not a type use.
    private static readonly Regex ShortMode = new(
      @"(?<![:.\w])Mode(?=\s+(?!{)|\)|>|,)",
      RegexOptions.CultureInvariant);

    private sealed record CompileResult(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileDiagnostics,
      string Generated);

    private static CompileResult Compile(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Preview)
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // A real app loads the analyzers assembly as an analyzer, not as a reference (CS0433).
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;

        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen78MemberAndTypeNames",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      string generated = string.Join(
        "\n",
        output.SyntaxTrees
          .Where(static syntaxTree => syntaxTree.FilePath.Contains("NuruGenerator", StringComparison.Ordinal))
          .Select(static syntaxTree => syntaxTree.GetText().ToString()));
      return new CompileResult(generatorDiagnostics, output.GetDiagnostics(), generated);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static diagnostic =>
      {
        SyntaxTree? tree = diagnostic.Location.SourceTree;
        if (tree is null || !diagnostic.Location.IsInSource)
          return diagnostic.ToString();

        int line = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
        return diagnostic + "\n    > " + tree.GetText().Lines[line].ToString().Trim();
      }));

    private static void ShouldCompile(CompileResult result)
    {
      Diagnostic[] generatorErrors = [.. result.GeneratorDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
      generatorErrors.ShouldBeEmpty(Describe(generatorErrors));

      Diagnostic[] compileErrors = [.. result.CompileDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
      compileErrors.ShouldBeEmpty(Describe(compileErrors));
    }

    /// <summary>
    /// An endpoint property named Type is a keyword local. The generated file keeps @type whole
    /// and builds converter, parse, and repeated-element locals from the bare name.
    /// </summary>
    public static async Task Should_compile_endpoint_property_named_type()
    {
      const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<Gen78KindCommand>()
          .Map<Gen78CountCommand>()
          .Map<Gen78ManyCommand>()
          .Build();

        return await app.RunAsync(args);

        public enum Gen78Kind { One, Two }

        [NuruRoute("kind")]
        public sealed class Gen78KindCommand : ICommand<int>
        {
          [Option("type")]
          public Gen78Kind Type { get; set; }

          public sealed class Handler : IRequestHandler<Gen78KindCommand, int>
          {
            public ValueTask<int> Handle(Gen78KindCommand command, CancellationToken cancellationToken)
              => ValueTask.FromResult((int)command.Type);
          }
        }

        [NuruRoute("count")]
        public sealed class Gen78CountCommand : ICommand<int>
        {
          [Option("type")]
          public int? Type { get; set; }

          public sealed class Handler : IRequestHandler<Gen78CountCommand, int>
          {
            public ValueTask<int> Handle(Gen78CountCommand command, CancellationToken cancellationToken)
              => ValueTask.FromResult(command.Type ?? 0);
          }
        }

        [NuruRoute("many")]
        public sealed class Gen78ManyCommand : ICommand<int>
        {
          [Option("type")]
          public Gen78Kind[] Type { get; set; } = [];

          public sealed class Handler : IRequestHandler<Gen78ManyCommand, int>
          {
            public ValueTask<int> Handle(Gen78ManyCommand command, CancellationToken cancellationToken)
              => ValueTask.FromResult(command.Type.Length);
          }
        }
        """;

      CompileResult result = Compile(Source);
      ShouldCompile(result);

      result.Generated.ShouldContain("@type");
      result.Generated.ShouldContain("__enumConverter_type_");
      result.Generated.ShouldContain("__type_parsed_");
      result.Generated.ShouldContain("__i_type_");
      result.Generated.ShouldNotContain("_@");
      result.Generated.ShouldNotContain("@type_");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Two enums with the same simple name are both in scope. Handler signatures, result locals,
    /// and render casts use the global:: name.
    /// </summary>
    public static async Task Should_qualify_handler_return_types_when_enum_names_collide()
    {
      const string Source = """
        global using Ns1;
        global using Ns2;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("one --mode {mode}").WithHandler((global::Ns1.Mode mode) => mode).AsQuery().Done()
          .Map("two --kind {kind}").WithHandler((global::Ns2.Mode kind) => kind).AsQuery().Done()
          .Map("three --mode {mode}").WithHandler(Gen78Handlers.ReturnMode).AsQuery().Done()
          .Map("four --mode {mode}").WithHandler(async (global::Ns1.Mode mode) => mode).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        namespace Ns1 { public enum Mode { Fast, Slow } }
        namespace Ns2 { public enum Mode { Red, Blue } }

        public static class Gen78Handlers
        {
          public static global::Ns1.Mode ReturnMode(global::Ns1.Mode mode) => mode;
        }
        """;

      CompileResult result = Compile(Source);
      ShouldCompile(result);

      result.Generated.ShouldContain("global::Ns1.Mode result");
      result.Generated.ShouldContain("global::Ns2.Mode result");
      result.Generated.ShouldContain("global::System.Threading.Tasks.Task<global::Ns1.Mode>");
      // global::Ns1.Mode contains the letters Mode. A short name is Mode not preceded by '.' or ':'.
      List<string> shortNames = [];
      foreach (string line in result.Generated.Split('\n'))
      {
        if (ShortMode.IsMatch(line))
          shortNames.Add(line.Trim());
      }

      shortNames.ShouldBeEmpty(string.Join("\n", shortNames));

      await Task.CompletedTask;
    }

    /// <summary>
    /// Bug 442: an async handler that returns Unit emits global::TimeWarp.Mediator.Unit.
    /// The same path now qualifies every return type, so Unit is not a special case.
    /// </summary>
    public static async Task Should_qualify_unit_return_type()
    {
      const string Source = """
        using System.Threading.Tasks;
        using TimeWarp.Mediator;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("ping").WithHandler(async () => { await Task.Yield(); return Unit.Value; }).AsCommand().Done()
          .Build();

        return await app.RunAsync(args);
        """;

      CompileResult result = Compile(Source);
      ShouldCompile(result);
      result.Generated.ShouldContain("global::TimeWarp.Mediator.Unit result");
      result.Generated.ShouldNotContain("\n    Unit result");

      await Task.CompletedTask;
    }
  }
}
