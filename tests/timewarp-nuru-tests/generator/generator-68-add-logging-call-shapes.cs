#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Kanban 546: services.AddLogging() with no configure delegate is valid Microsoft.Extensions.Logging
// setup (a factory with no providers). The generator only recognized AddLogging(b => ...), so the
// parameterless call left handlers on a NullLogger and NURU_H007 warned. These tests cover each
// AddLogging call shape: which ones count as logging configured, and that the app compiles.
#endregion

#region Design
// Roslyn-hosted like generator-67, so the CI multi-mode assembly excludes it (CS0433) and it runs
// standalone. The parameterless test also emits the compiled app, loads it, and runs one command,
// so it checks the logger's runtime type, not just the generated text. The app records the injected
// types in a static probe the test reads by reflection, which keeps console output out of it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen68AddLoggingCallShapes
{
  using System.Collections.Immutable;
  using System.Reflection;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class AddLoggingCallShapesTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AddLoggingCallShapesTests>();

    private static readonly string[] LogCommandArgs = ["gen68-log"];

    private sealed record GeneratorRun(
      ImmutableArray<Diagnostic> GeneratorDiagnostics,
      ImmutableArray<Diagnostic> CompileErrors,
      string GeneratedSource,
      Compilation Output);

    private static GeneratorRun Run(string configureServices)
    {
      string source = $$"""
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices({{configureServices}})
          .Map("gen68-log").WithHandler((ILogger<Gen68Category> logger, ILoggerFactory factory) =>
          {
            Gen68Probe.LoggerType = logger.GetType().Name;
            Gen68Probe.FactoryType = factory.GetType().Name;
            return "ok";
          }).AsCommand().Done()
          .Build();
        return await app.RunAsync(args);

        static void ConfigureLogging(ILoggingBuilder builder) => builder.SetMinimumLevel(LogLevel.Warning);

        public sealed class Gen68Category;

        public static class Gen68Probe
        {
          public static string? LoggerType { get; set; }
          public static string? FactoryType { get; set; }
        }
        """;

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
        assemblyName: "Gen68App",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(
        generators: [new NuruGenerator().AsSourceGenerator()],
        parseOptions: parseOptions);
      _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);

      string generated = string.Join("\n", output.SyntaxTrees.Where(t => t != tree).Select(static t => t.ToString()));
      ImmutableArray<Diagnostic> errors = [.. output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error)];
      return new GeneratorRun(generatorDiagnostics, errors, generated, output);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
      string.Join("\n", diagnostics.Select(static d => d.ToString()));

    private static void ShouldConfigureLogging(GeneratorRun run)
    {
      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = __loggerFactory.CreateLogger<global::Gen68Category>();");
      run.GeneratedSource.ShouldContain("factory = __loggerFactory;");
      run.GeneratorDiagnostics.Any(static d => d.Id == "NURU_H007").ShouldBeFalse(Describe(run.GeneratorDiagnostics));
    }

    private static void ShouldNotConfigureLogging(GeneratorRun run)
    {
      run.CompileErrors.ShouldBeEmpty(Describe(run.CompileErrors));
      run.GeneratedSource.ShouldContain("logger = global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<global::Gen68Category>();");
      run.GeneratorDiagnostics.Count(static d => d.Id == "NURU_H007").ShouldBe(1, Describe(run.GeneratorDiagnostics));
    }

    public static async Task Should_give_delegate_a_real_logger_from_parameterless_add_logging()
    {
      GeneratorRun run = Run("s => s.AddLogging()");

      ShouldConfigureLogging(run);

      // The factory has no providers: nothing is emitted inside the LoggerFactory.Create lambda.
      run.GeneratedSource.ShouldContain("LoggerFactory.Create(builder =>\n    {\n    });");

      // Run the compiled app: the delegate gets the factory's Logger<T>, not a NullLogger.
      using MemoryStream image = new();
      Microsoft.CodeAnalysis.Emit.EmitResult emitResult = run.Output.Emit(image);
      emitResult.Success.ShouldBeTrue(Describe(emitResult.Diagnostics));

      Assembly assembly = Assembly.Load(image.ToArray());
      object? exitCode = assembly.EntryPoint!.Invoke(null, [LogCommandArgs]);
      exitCode.ShouldBe(0);

      Type probe = assembly.GetType("Gen68Probe")!;
      probe.GetProperty("LoggerType")!.GetValue(null).ShouldBe("Logger`1");
      // LoggerFactory.Create returns an internal wrapper type, so only rule out the null factory.
      probe.GetProperty("FactoryType")!.GetValue(null).ShouldNotBe("NullLoggerFactory");

      await Task.CompletedTask;
    }

    public static async Task Should_configure_logging_for_parameterless_add_logging_in_block_body()
    {
      ShouldConfigureLogging(Run("services => { services.AddLogging(); }"));

      await Task.CompletedTask;
    }

    public static async Task Should_configure_logging_for_static_extension_call()
    {
      ShouldConfigureLogging(Run("s => LoggingServiceCollectionExtensions.AddLogging(s)"));

      await Task.CompletedTask;
    }

    public static async Task Should_configure_logging_for_static_extension_call_with_lambda()
    {
      GeneratorRun run = Run("s => LoggingServiceCollectionExtensions.AddLogging(s, b => b.SetMinimumLevel(LogLevel.Warning))");

      ShouldConfigureLogging(run);
      run.GeneratedSource.ShouldContain("b.SetMinimumLevel(LogLevel.Warning);");

      await Task.CompletedTask;
    }

    public static async Task Should_configure_logging_for_named_configure_argument()
    {
      GeneratorRun run = Run("s => s.AddLogging(configure: b => b.SetMinimumLevel(LogLevel.Warning))");

      ShouldConfigureLogging(run);
      run.GeneratedSource.ShouldContain("b.SetMinimumLevel(LogLevel.Warning);");

      await Task.CompletedTask;
    }

    public static async Task Should_configure_logging_for_anonymous_method()
    {
      GeneratorRun run = Run("s => s.AddLogging(delegate (ILoggingBuilder lb) { lb.SetMinimumLevel(LogLevel.Warning); })");

      ShouldConfigureLogging(run);
      run.GeneratedSource.ShouldContain("lb.SetMinimumLevel(LogLevel.Warning);");

      await Task.CompletedTask;
    }

    public static async Task Should_warn_for_method_group_configure_delegate()
    {
      // A method group may not be reachable from the generated class, and an empty factory would
      // drop its providers, so it stays unsupported and NURU_H007 says so.
      ShouldNotConfigureLogging(Run("s => s.AddLogging(ConfigureLogging)"));

      await Task.CompletedTask;
    }

    public static async Task Should_warn_without_add_logging()
    {
      ShouldNotConfigureLogging(Run("s => s.AddSingleton<Gen68Category>()"));

      await Task.CompletedTask;
    }
  }
}
