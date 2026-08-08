#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-012 (M8): DispatchAddTypeConverter's fallback (when
// GetSymbolInfo(objectCreation.Type) does not resolve a symbol) must try SemanticModel.
// GetTypeInfo() before giving up, and — if the type is genuinely unresolvable — must emit
// NURU_S009 rather than fall back to unqualified syntax text (objectCreation.Type.ToString()),
// which would emit a broken, namespace-less type name into generated code.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen36TypeConverterFqn
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class TypeConverterFqnTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TypeConverterFqnTests>();

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
        assemblyName: "TypeConverterFqnRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// A converter type qualified by a namespace with no "using" for it (only reachable
    /// via the qualified name "Gen36Converters.EmailConverter") must be emitted into
    /// generated code fully-qualified (global::...), not as raw unqualified syntax text.
    /// </summary>
    public static async Task Should_emit_fully_qualified_converter_type_name()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .AddTypeConverter(new Gen36Converters.EmailConverter())
          .Map("m8-typeconverter-fqn {value:email}")
            .WithHandler((EmailAddress value) => value.ToString())
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);

        public record EmailAddress(string Value)
        {
          public override string ToString() => Value;
        }

        namespace Gen36Converters
        {
          public class EmailConverter : IRouteTypeConverter
          {
            public Type TargetType => typeof(EmailAddress);
            public string? ConstraintAlias => "email";

            public bool TryConvert(string value, out object? result)
            {
              result = new EmailAddress(value);
              return true;
            }
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] s009 = [.. result.Diagnostics.Where(d => d.Id == "NURU_S009")];
      s009.ShouldBeEmpty();

      string generatedSource = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));

      generatedSource.ShouldContain("new global::Gen36Converters.EmailConverter()");

      await Task.CompletedTask;
    }

    /// <summary>
    /// A converter type that cannot be resolved at all (neither GetSymbolInfo nor
    /// GetTypeInfo succeed — the namespace/type genuinely does not exist) must produce
    /// NURU_S009 and must NOT leak an unqualified/broken type name into generated code.
    /// </summary>
    public static async Task Should_emit_diagnostic_for_unresolvable_converter_type()
    {
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .AddTypeConverter(new Gen36DoesNotExist.MissingConverter())
          .Map("m8-typeconverter-unresolvable {value}")
            .WithHandler((string value) => value)
            .AsQuery()
          .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      Diagnostic[] s009 = [.. result.Diagnostics.Where(d => d.Id == "NURU_S009")];
      s009.Length.ShouldBe(1);

      string generatedSource = string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));

      // The unresolved type must never be emitted, qualified or not.
      generatedSource.ShouldNotContain("MissingConverter");

      await Task.CompletedTask;
    }
  }
}
