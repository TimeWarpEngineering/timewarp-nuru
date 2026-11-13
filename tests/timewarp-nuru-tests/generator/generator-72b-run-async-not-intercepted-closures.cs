#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 547: NURU_I001 for RunAsync() calls inside closures and local functions: reassigned locals, nested lambdas,
// null-initialized locals, method groups, and extraction errors elsewhere in the compilation.
#endregion

#region Design
// Roslyn-hosted like generator-69, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. Assertions read the generator diagnostics and the compile errors of the output.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen72RunAsyncNotInterceptedClosures
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class RunAsyncNotInterceptedClosureTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RunAsyncNotInterceptedClosureTests>();

    private sealed record GeneratorRun(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileErrors);

    private static GeneratorRun Run(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // The analyzers assembly shares parsing types with timewarp-nuru (CS0433); a real app
        // loads it as an analyzer, not as a reference.
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen72App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      ImmutableArray<Diagnostic> errors = [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
      return new GeneratorRun(generatorDiagnostics, errors);
    }

    private static Diagnostic[] I001(GeneratorRun run) =>
      [.. run.GeneratorDiagnostics.Where(static d => d.Id == "NURU_I001")];

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d => d.ToString()));

    public static async Task Should_bind_a_closure_call_to_the_app_the_closure_assigns()
    {
      // The lambda assigns the captured local and then runs it. Its own interpretation binds that
      // call; the outer walk does not see the lambda's assignment, so it must not bind it too (CS9153).
      const string Source = """
        using System;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen72-first").WithHandler(() => "first").AsCommand().Done()
          .Build();
        Func<Task<int>> rebuild = async () =>
        {
          app = NuruApp.CreateBuilder()
            .Map("gen72-second").WithHandler(() => "second").AsCommand().Done()
            .Build();
          return await app.RunAsync(["gen72-second"]);
        };
        await app.RunAsync(["gen72-first"]);
        return await rebuild();
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      I001(run).ShouldBeEmpty(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_warn_for_a_nested_lambda_that_captures_a_reassigned_local()
    {
      // Review round 3 M5: make's Build() is its own interpretation and records only "b". The inner
      // lambda runs after the outer method assigns "c", so binding it to "b" ran the wrong app.
      const string Source = """
        using System;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder().Map("gen72-a").WithHandler(() => "a").AsCommand().Done().Build();
        Func<Func<Task<int>>> make = () =>
        {
          app = NuruApp.CreateBuilder().Map("gen72-b").WithHandler(() => "b").AsCommand().Done().Build();
          return () => app.RunAsync(["gen72-c"]);
        };
        Func<Task<int>> run = make();
        app = NuruApp.CreateBuilder().Map("gen72-c").WithHandler(() => "c").AsCommand().Done().Build();
        return await run();
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      Diagnostic[] i001 = I001(run);
      i001.Length.ShouldBe(1, Describe(run.GeneratorDiagnostics));
      i001[0].Location.GetLineSpan().StartLinePosition.Line.ShouldBe(8);

      await Task.CompletedTask;
    }

    public static async Task Should_warn_for_a_closure_call_before_the_closure_assigns_the_local()
    {
      // Review round 3 M5: the argument runs while app is still "a", but the lambda's interpretation
      // only knows its own "b" assignment, which comes after the call.
      const string Source = """
        using System;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder().Map("gen72-a").WithHandler(() => "a").AsCommand().Done().Build();
        Func<int> run = () =>
        {
          int code = Check(app.RunAsync(["gen72-a"]));
          app = NuruApp.CreateBuilder().Map("gen72-b").WithHandler(() => "b").AsCommand().Done().Build();
          return code;
        };
        return run();

        static int Check(Task<int> task) => task.GetAwaiter().GetResult();
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      Diagnostic[] i001 = I001(run);
      i001.Length.ShouldBe(1, Describe(run.GeneratorDiagnostics));
      i001[0].Location.GetLineSpan().StartLinePosition.Line.ShouldBe(7);

      await Task.CompletedTask;
    }

    public static async Task Should_bind_a_closure_call_when_the_local_starts_null()
    {
      // A null initializer never holds an app, so the one Build() assignment still traces the closure call
      const string Source = """
        using System;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp? app = null;
        app = NuruApp.CreateBuilder().Map("gen72-work").WithHandler(() => "work").AsCommand().Done().Build();
        Func<Task<int>> run = () => app!.RunAsync(["gen72-work"]);
        return await run();
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      I001(run).ShouldBeEmpty(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_bind_a_closure_call_when_the_local_starts_null_wrapped()
    {
      // null!, (null), default!, a cast of null and a later `app = null!;` before Build() hold no app either
      string[] nullWrites =
      [
        "NuruApp app = null!;",
        "NuruApp? app = (null);",
        "NuruApp app = default!;",
        "NuruApp? app = (NuruApp?)null;",
        "NuruApp? app = (NuruApp?)(default);",
        "NuruApp app = null!; app = (null)!;",
      ];

      foreach (string nullWrite in nullWrites)
      {
        string source = $$"""
          using System;
          using System.Threading.Tasks;
          using TimeWarp.Nuru;

          {{nullWrite}}
          app = NuruApp.CreateBuilder().Map("gen72-work").WithHandler(() => "work").AsCommand().Done().Build();
          Func<Task<int>> run = () => app!.RunAsync(["gen72-work"]);
          return await run();
          """;

        GeneratorRun run = Run(source);

        run.CompileErrors.ShouldBeEmpty($"{nullWrite}\n{Describe(run.CompileErrors)}");
        I001(run).ShouldBeEmpty($"{nullWrite}\n{Describe(run.GeneratorDiagnostics)}");
      }

      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_for_a_method_group()
    {
      // A method group conversion is not an invocation, so there is no call site to report
      const string Source = """
        using System;
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen72-work").WithHandler(() => "work").AsCommand().Done()
          .Build();
        Func<string[], Task<int>> run = app.RunAsync;
        return await run(["gen72-work"]);
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      I001(run).ShouldBeEmpty(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_not_add_a_warning_to_an_extraction_error()
    {
      const string Source = """
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .Map("gen72-work {").WithHandler(() => "work").AsCommand().Done()
          .Build();
        return await app.RunAsync(["gen72-work"]);
        """;

      GeneratorRun run = Run(Source);

      run.GeneratorDiagnostics.ShouldContain(static d => d.Severity == DiagnosticSeverity.Error, Describe(run.GeneratorDiagnostics));
      I001(run).ShouldBeEmpty(Describe(run.GeneratorDiagnostics));

      await Task.CompletedTask;
    }

    public static async Task Should_still_warn_elsewhere_when_another_app_has_an_extraction_error()
    {
      // Review round 1 M3: an extraction error suppressed every NURU_I001 in the compilation. Only
      // calls in the failed app's scope are quiet now; the parameter call in Runner still warns.
      const string Source = """
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp broken = NuruApp.CreateBuilder()
          .Map("gen72-work {").WithHandler(() => "work").AsCommand().Done()
          .Build();
        await broken.RunAsync(["gen72-work"]);
        return await Runner.Run();

        static class Runner
        {
          public static async Task<int> Run()
          {
            NuruApp valid = NuruApp.CreateBuilder()
              .Map("gen72-valid").WithHandler(() => "valid").AsCommand().Done()
              .Build();
            await valid.RunAsync(["gen72-valid"]);
            return await RunTarget(valid);
          }

          static Task<int> RunTarget(NuruApp target) => target.RunAsync(["gen72-valid"]);
        }
        """;

      GeneratorRun run = Run(Source);

      run.GeneratorDiagnostics.ShouldContain(static d => d.Severity == DiagnosticSeverity.Error, Describe(run.GeneratorDiagnostics));
      Diagnostic[] i001 = I001(run);
      i001.Length.ShouldBe(1, Describe(run.GeneratorDiagnostics));
      i001[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture).ShouldContain("'target'");

      await Task.CompletedTask;
    }
  }
}
