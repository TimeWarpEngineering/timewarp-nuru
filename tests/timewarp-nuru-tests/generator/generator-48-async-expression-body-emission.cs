#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 474: the delegate emitter prepended `await` to every async
// expression body, emitting `=> await await s.Send(q)` for `async s => await s.Send(q)`.
// An async lambda's expression body must be emitted verbatim; only a non-async
// Task-returning expression body is awaited by the generated local function.
// generator-47 proves the same shapes compile and run end to end.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen48AsyncExpressionBodyEmission
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class AsyncExpressionBodyEmissionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AsyncExpressionBodyEmissionTests>();

    private static string GenerateForHandler(string handler, string terminal = ".AsQuery()")
    {
      string source = $$"""
        using System.Threading.Tasks;
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder(args)
          .Map("run {name}")
            .WithHandler({{handler}})
            {{terminal}}
          .Done()
          .Build();

        return await app.RunAsync(args);

        public sealed record Q(string Name) : global::TimeWarp.Mediator.IQuery<string>;
        """;

      SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      List<MetadataReference> references = [];
      foreach (string path in tpa.Split(Path.PathSeparator))
      {
        references.Add(MetadataReference.CreateFromFile(path));
      }

      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: "AsyncExpressionBodyEmissionRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new NuruGenerator())
        .RunGenerators(compilation)
        .GetRunResult();

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();
      return string.Join(
        "\n",
        result.Results[0].GeneratedSources.Select(g => g.SourceText.ToString()));
    }

    public static async Task Should_emit_isender_await_body_verbatim()
    {
      string generated = GenerateForHandler(
        "async (string name, global::TimeWarp.Mediator.ISender s) => await s.Send(new Q(name))");

      generated.ShouldContain(") => await s.Send(new Q(name));");
      generated.ShouldNotContain("await await");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_configure_await_body_verbatim()
    {
      string generated = GenerateForHandler(
        "async (string name) => await Task.FromResult(name).ConfigureAwait(false)");

      generated.ShouldContain(") => await Task.FromResult(name).ConfigureAwait(false);");
      generated.ShouldNotContain("await await");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_parenthesized_await_body_verbatim()
    {
      string generated = GenerateForHandler(
        "async (string name) => (await Task.FromResult(name)).ToUpperInvariant()");

      generated.ShouldContain(") => (await Task.FromResult(name)).ToUpperInvariant();");
      generated.ShouldNotContain("await (await");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_nested_await_body_verbatim()
    {
      string generated = GenerateForHandler(
        "async (string name) => await (await Task.FromResult(Task.FromResult(name)))");

      generated.ShouldContain(") => await (await Task.FromResult(Task.FromResult(name)));");
      generated.ShouldNotContain("await await");

      await Task.CompletedTask;
    }

    public static async Task Should_not_await_non_awaitable_async_body()
    {
      string generated = GenerateForHandler(
        "async (string name) => name.ToUpperInvariant()");

      generated.ShouldContain(") => name.ToUpperInvariant();");
      generated.ShouldNotContain("await name.ToUpperInvariant()");

      await Task.CompletedTask;
    }

    public static async Task Should_emit_void_async_await_body_verbatim()
    {
      string generated = GenerateForHandler(
        "async (string name) => await Task.Delay(name.Length)",
        ".AsCommand()");

      generated.ShouldContain(") => await Task.Delay(name.Length);");
      generated.ShouldNotContain("await await");

      await Task.CompletedTask;
    }

    public static async Task Should_still_await_non_async_task_returning_body()
    {
      string generated = GenerateForHandler(
        "(string name) => Task.FromResult(name)");

      generated.ShouldContain(") => await Task.FromResult(name);");

      await Task.CompletedTask;
    }

    public static async Task Should_still_await_non_async_void_task_body()
    {
      string generated = GenerateForHandler(
        "(string name) => Task.Delay(name.Length)",
        ".AsCommand()");

      generated.ShouldContain(") => await Task.Delay(name.Length);");

      await Task.CompletedTask;
    }

    public static async Task Should_leave_async_block_body_unchanged()
    {
      string generated = GenerateForHandler(
        "async (string name) => { return await Task.FromResult(name); }");

      generated.ShouldContain("return await Task.FromResult(name);");
      generated.ShouldNotContain("await await");

      await Task.CompletedTask;
    }
  }
}
