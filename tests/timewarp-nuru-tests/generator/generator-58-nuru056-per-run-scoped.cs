#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

// NURU056: a singleton that injects a Scoped service which injects a session- or command-scoped
// service outlives that Scoped instance, the same as a singleton injecting the session or command
// service directly (508-007 review M1). A Scoped service with no per-run dependency stays allowed.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen58Nuru056PerRunScoped
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class Nuru056PerRunScopedTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Nuru056PerRunScopedTests>();

    private static List<Diagnostic> Nuru056(string registrations)
    {
      string source = $$"""
        using Microsoft.Extensions.DependencyInjection;
        using TimeWarp.Nuru;

        public interface IToken { }
        public sealed class Token : IToken { }
        public interface IHolder { }
        public sealed class Holder(IToken token) : IHolder { public IToken Token { get; } = token; }
        public interface IPlain { }
        public sealed class Plain : IPlain { }
        public interface IRoot { }
        public sealed class Root(IHolder holder) : IRoot { public IHolder Holder { get; } = holder; }
        public sealed class PlainRoot(IPlain plain) : IRoot { public IPlain Plain { get; } = plain; }

        NuruApp app = NuruApp.CreateBuilder()
          .ConfigureServices(services =>
          {
            {{registrations}}
          })
          .Map("go").WithHandler((IRoot root) => "go").AsQuery().Done()
          .Build();

        app.RunAsync([]);
        """;

      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        if (path.Length > 0)
          references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "Gen58Nuru056",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new NuruGenerator())
        .RunGenerators(compilation)
        .GetRunResult();

      // The run result's diagnostics already hold every generator's diagnostics.
      return [.. result.Diagnostics.Where(diagnostic => diagnostic.Id == "NURU056")];
    }

    public static async Task Should_warn_when_a_singleton_injects_a_scoped_service_over_a_command_service()
    {
      List<Diagnostic> diagnostics = Nuru056("""
        services.AddCommandScoped<IToken, Token>();
        services.AddScoped<IHolder, Holder>();
        services.AddSingleton<IRoot, Root>();
        """);

      diagnostics.Count.ShouldBe(1);
      diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Warning);
      string message = diagnostics[0].GetMessage(CultureInfo.InvariantCulture);
      message.ShouldContain("'Root' (Singleton lifetime)");
      message.ShouldContain("'Holder' (Scoped lifetime)");
      await Task.CompletedTask;
    }

    public static async Task Should_warn_when_the_scoped_service_injects_a_session_service()
    {
      List<Diagnostic> diagnostics = Nuru056("""
        services.AddSessionScoped<IToken, Token>();
        services.AddScoped<IHolder, Holder>();
        services.AddSingleton<IRoot, Root>();
        """);

      diagnostics.Count.ShouldBe(1);
      diagnostics[0].GetMessage(CultureInfo.InvariantCulture).ShouldContain("'Holder' (Scoped lifetime)");
      await Task.CompletedTask;
    }

    public static async Task Should_not_warn_when_the_scoped_service_has_no_per_run_dependency()
    {
      List<Diagnostic> diagnostics = Nuru056("""
        services.AddScoped<IPlain, Plain>();
        services.AddSingleton<IRoot, PlainRoot>();
        """);

      diagnostics.ShouldBeEmpty();
      await Task.CompletedTask;
    }
  }
}
