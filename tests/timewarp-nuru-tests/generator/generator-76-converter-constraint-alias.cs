#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 584: IRouteTypeConverter.ConstraintAlias in a route pattern ({at:point}) and an unknown
// type constraint. Before 584 the generator never read ConstraintAlias, so {at:point} fell through
// every emitter: the positional path left `at` undeclared (CS0103) and the option and --json-args
// paths assigned a string to the converter's target type (CS0029), with no Nuru diagnostic.
#endregion

#region Design
// Each case compiles an app in memory with Roslyn and runs the Nuru generator on it. The alias case
// requires zero errors from the generator and from the compile of its output. The unknown-constraint
// cases require NURU_S010 and name the subject (parameter or option) it reports. A converter whose
// ConstraintAlias is not a compile-time constant cannot be read, so its alias reports NURU_S010 too.
// Roslyn-hosted (the analyzers library collides with timewarp-nuru's parsing types, CS0433), so it
// runs in the standalone phase.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen76ConverterConstraintAlias
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class ConverterConstraintAliasTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConverterConstraintAliasTests>();

    private const string Usings = """
      using System;
      using System.Threading.Tasks;
      using TimeWarp.Nuru;

      """;

    private const string PointTypes = """
      public enum Gen76Shade { Light, Dark }

      public readonly record struct Gen76Point(int X, int Y);

      public sealed class Gen76PointConverter : IRouteTypeConverter
      {
        public Type TargetType => typeof(Gen76Point);
        public string? ConstraintAlias => "point";

        public bool TryConvert(string value, out object? result) =>
          (result = new Gen76Point(int.Parse(value.Split(',')[0]), int.Parse(value.Split(',')[1]))) is not null;
      }
      """;

    private sealed record CompileResult(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileDiagnostics,
      string Generated);

    private static CompileResult Compile(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(Usings + source, parseOptions, path: "app.cs");

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
        assemblyName: "Gen76ConverterAliasApp",
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
          .Where(static t => t.FilePath.Contains("NuruGenerator", StringComparison.Ordinal))
          .Select(static t => t.GetText().ToString()));
      return new CompileResult(generatorDiagnostics, output.GetDiagnostics(), generated);
    }

    // Errors carry the offending line so a failure names what the emitter wrote.
    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d =>
      {
        SyntaxTree? tree = d.Location.SourceTree;
        if (tree is null || !d.Location.IsInSource)
          return d.ToString();

        int line = d.Location.GetLineSpan().StartLinePosition.Line;
        return d + "\n    > " + tree.GetText().Lines[line].ToString().Trim();
      }));

    private static Diagnostic[] S010(CompileResult result) =>
      [.. result.GeneratorDiagnostics.Where(static d => d.Id == "NURU_S010")];

    /// <summary>
    /// {at:point} binds through the converter whose ConstraintAlias is "point" on the positional,
    /// optional positional, and option paths. SpotParser covers a getter-block alias read from a
    /// const, a converter name with no "Converter" suffix, and a namespaced TargetType.
    /// </summary>
    public static async Task Should_compile_routes_that_name_a_converter_by_its_alias()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .AddTypeConverter(new Gen76PointConverter())
          .AddTypeConverter(new Gen76Geo.SpotParser())
          .Map("point {at:point}").WithHandler((Gen76Point at) => at.X).AsQuery().Done()
          .Map("point-maybe {at:point?}").WithHandler((Gen76Point? at) => at?.X ?? 0).AsQuery().Done()
          .Map("point-option --at {at:point}").WithHandler((Gen76Point at) => at.Y).AsQuery().Done()
          .Map("point-type {at:Gen76Point}").WithHandler((Gen76Point at) => at.X).AsQuery().Done()
          .Map("spot {where:spot}").WithHandler((Gen76Geo.Spot where) => where.Name).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        namespace Gen76Geo
        {
          public sealed record Spot(string Name);

          public sealed class SpotParser : IRouteTypeConverter
          {
            private const string Alias = "spot";

            public Type TargetType { get { return typeof(Spot); } }
            public string? ConstraintAlias { get { return Alias; } }

            public bool TryConvert(string value, out object? result)
            {
              result = new Spot(value);
              return true;
            }
          }
        }

        """ + PointTypes;

      CompileResult result = Compile(Source);

      Diagnostic[] generatorErrors = [.. result.GeneratorDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];
      generatorErrors.ShouldBeEmpty(Describe(generatorErrors));

      Diagnostic[] compileErrors = [.. result.CompileDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)];
      compileErrors.ShouldBeEmpty(Describe(compileErrors));

      result.Generated.ShouldContain("new global::Gen76PointConverter()");
      result.Generated.ShouldContain("new global::Gen76Geo.SpotParser()");
      result.Generated.ShouldContain("global::Gen76Geo.Spot");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A positional constraint and an option constraint that name no built-in type, enum handler
    /// parameter, or registered converter each report NURU_S010 at the route.
    /// </summary>
    public static async Task Should_report_unknown_constraint_on_parameter_and_option()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .AddTypeConverter(new Gen76PointConverter())
          .Map("where {at:nowhere}").WithHandler((Gen76Point at) => at.X).AsQuery().Done()
          .Map("where-option --at {at:elsewhere}").WithHandler((Gen76Point at) => at.Y).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        """ + PointTypes;

      Diagnostic[] s010 = S010(Compile(Source));

      s010.Length.ShouldBe(2, Describe(s010));
      s010.ShouldAllBe(static d => d.Severity == DiagnosticSeverity.Error);
      s010.ShouldContain(static d => d.GetMessage(null).Contains("Parameter 'at'", StringComparison.Ordinal)
        && d.GetMessage(null).Contains("'nowhere'", StringComparison.Ordinal));
      s010.ShouldContain(static d => d.GetMessage(null).Contains("Option '--at'", StringComparison.Ordinal)
        && d.GetMessage(null).Contains("'elsewhere'", StringComparison.Ordinal));
      s010.ShouldAllBe(static d => d.Location.IsInSource);

      await Task.CompletedTask;
    }

    /// <summary>
    /// Built-in types, an enum handler parameter, a converter target type name, and a converter alias
    /// are all known constraints, so none reports NURU_S010.
    /// </summary>
    public static async Task Should_not_report_known_constraints()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .AddTypeConverter(new Gen76PointConverter())
          .Map("known {count:int} {when:DateTime?}").WithHandler((int count, DateTime? when) => count).AsQuery().Done()
          .Map("shade {shade:Gen76Shade} --also {other:Gen76Shade?}")
            .WithHandler((Gen76Shade shade, Gen76Shade? other) => shade).AsQuery().Done()
          .Map("by-type {at:Gen76Point}").WithHandler((Gen76Point at) => at.X).AsQuery().Done()
          .Map("by-alias {at:point} --to {to:point?}").WithHandler((Gen76Point at, Gen76Point? to) => at.X).AsQuery().Done()
          .Map("sum {*values:int}").WithHandler((int[] values) => values.Length).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        """ + PointTypes;

      Diagnostic[] s010 = S010(Compile(Source));
      s010.ShouldBeEmpty(Describe(s010));

      await Task.CompletedTask;
    }

    /// <summary>
    /// ConstraintAlias is read from source, so it must be a compile-time constant. A computed alias
    /// cannot be read: a route that uses it reports NURU_S010 rather than emitting broken code.
    /// </summary>
    public static async Task Should_report_alias_that_is_not_a_constant()
    {
      const string Source = """
        NuruApp app = NuruApp.CreateBuilder()
          .AddTypeConverter(new Gen76ComputedConverter())
          .Map("computed {at:computedalias}").WithHandler((Gen76Point at) => at.X).AsQuery().Done()
          .Build();

        return await app.RunAsync(args);

        public sealed class Gen76ComputedConverter : IRouteTypeConverter
        {
          public Type TargetType => typeof(Gen76Point);
          public string? ConstraintAlias => string.Concat("computed", "alias");

          public bool TryConvert(string value, out object? result)
          {
            result = new Gen76Point(0, 0);
            return true;
          }
        }

        public readonly record struct Gen76Point(int X, int Y);
        """;

      Diagnostic[] s010 = S010(Compile(Source));

      s010.Length.ShouldBe(1, Describe(s010));
      s010[0].GetMessage(null).ShouldContain("'computedalias'");

      await Task.CompletedTask;
    }
  }
}
