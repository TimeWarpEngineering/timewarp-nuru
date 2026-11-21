#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 501: generated startup does only the work the run needs. A plain app builds no
// configuration root, and ExecuteRouteAsync no longer holds the --json-args reader, the
// --json-args selection or the NuruApp.InvokeAsync switch inline, so a plain argv run does not
// JIT-compile them or load System.Text.Json.
#endregion

#region Design
// Roslyn-hosted like generator-69, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. Each test compiles the generated source and reads its text.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen80StartupWorkOmission
{
  using System.Collections.Immutable;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class StartupWorkOmissionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<StartupWorkOmissionTests>();

    private const string MinimalMarker = "Minimal configuration for service initialization";

    private sealed record GeneratorRun(ImmutableArray<Diagnostic> CompileErrors, string GeneratedSource);

    private static GeneratorRun Run(string source)
    {
      CSharpParseOptions parseOptions = CSharpParseOptions.Default
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "TimeWarp.Nuru.Generated")]);
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "app.cs");

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        // The analyzers assembly shares parsing types with timewarp-nuru (CS0433).
        if (Path.GetFileNameWithoutExtension(path) == "TimeWarp.Nuru.Analyzers")
          continue;
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen80App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);

      string generated = string.Join("\n", output.SyntaxTrees.Where(t => t != tree).Select(static t => t.ToString()));
      ImmutableArray<Diagnostic> errors = [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
      return new GeneratorRun(errors, generated);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d => d.ToString()));

    // The text of the generated ExecuteRouteAsync method, up to its closing brace.
    private static string ExecuteRouteAsyncBody(string generated)
    {
      int start = generated.IndexOf("private static async Task<int> ExecuteRouteAsync", StringComparison.Ordinal);
      start.ShouldBeGreaterThanOrEqualTo(0);
      int end = generated.IndexOf("\n  }\n", start, StringComparison.Ordinal);
      end.ShouldBeGreaterThan(start);
      return generated[start..end];
    }

    private const string PlainApp = """
      using TimeWarp.Nuru;

      NuruApp app = NuruApp.CreateBuilder()
        .Map("--str {str} -i {i:int} -b")
          .WithHandler((string str, int i) => { })
          .AsQuery()
          .Done()
        .Build();

      return await app.RunAsync(args);
      """;

    public static async Task Plain_app_should_build_no_configuration()
    {
      GeneratorRun run = Run(PlainApp);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldNotContain("ConfigurationBuilder()");
      run.GeneratedSource.ShouldNotContain(MinimalMarker);
      run.GeneratedSource.ShouldContain("EnsureServicesInitialized(app, null);");
      await Task.CompletedTask;
    }

    public static async Task Runtime_dependency_injection_should_keep_the_blank_root()
    {
      // A runtime container can hand IConfiguration to any type it builds.
      const string Source = """
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .UseMicrosoftDependencyInjection()
          .Map("ping")
            .WithHandler(() => "pong")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain(MinimalMarker);
      run.GeneratedSource.ShouldNotContain("EnsureServicesInitialized(app, null)");
      await Task.CompletedTask;
    }

    public static async Task Json_args_and_invocation_dispatch_should_run_in_their_own_methods()
    {
      GeneratorRun run = Run(PlainApp);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      string body = ExecuteRouteAsyncBody(run.GeneratedSource);
      body.ShouldNotContain("JsonDocument.Parse");
      body.ShouldNotContain("__jsonMap");
      body.ShouldNotContain("switch (__invocation.Request)");
      body.ShouldContain("__ReadJsonArgsAsync(app, __jsonArgsValue, fromRepl, cancellationToken)");
      body.ShouldContain("return await __DispatchInvocationAsync(app, args, routeArgs, cancellationToken, fromRepl, null, __invocation)");
      body.ShouldContain("return await __DispatchJsonArgsAsync(app, args, routeArgs, cancellationToken, fromRepl, null, __jsonDoc)");
      run.GeneratedSource.ShouldContain("private static async Task<int> __DispatchJsonArgsAsync");
      run.GeneratedSource.ShouldContain("private static async Task<int> __DispatchInvocationAsync");
      await Task.CompletedTask;
    }

    public static async Task Configuration_read_by_a_dispatch_method_should_be_passed()
    {
      // AddConfiguration builds the full root; both dispatch methods receive it.
      const string Source = """
        using Microsoft.Extensions.Configuration;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .AddConfiguration()
          .Map("show")
            .WithHandler((IConfiguration configuration) => configuration["k"] ?? "")
            .AsQuery()
            .Done()
          .Build();

        return await app.RunAsync(args);
        """;

      GeneratorRun run = Run(Source);

      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      string body = ExecuteRouteAsyncBody(run.GeneratedSource);
      body.ShouldContain("fromRepl, configuration, __invocation)");
      body.ShouldContain("fromRepl, configuration, __jsonDoc)");
      await Task.CompletedTask;
    }
  }
}
