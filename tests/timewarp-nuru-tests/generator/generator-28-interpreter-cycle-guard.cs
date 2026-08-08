#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
#:package Microsoft.CodeAnalysis.CSharp

#region Purpose
// Regression test for kanban 454-003: DslInterpreter.ResolveIdentifier cached a
// variable's value only AFTER evaluating its initializer, so self-referential or
// mutually-referential declarations recursed forever and killed the analyzer host
// with an uncatchable StackOverflowException.
//
// Two repro shapes:
//   1. `var x = x;` — an ordinary mid-typing state in an IDE (has a compile error,
//      but analyzers/generators run on broken code constantly).
//   2. Two static fields assigned from each other — compiles CLEANLY, so this could
//      crash on committed user code, not just mid-typing.
//
// The test hosts NuruGenerator in a CSharpGeneratorDriver over in-memory source.
// Success == the generator run completes at all (the bug killed the process).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen28InterpreterCycleGuard
{
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using TimeWarp.Nuru.Generators;

  [TestTag("generator")]
  public class InterpreterCycleGuardTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<InterpreterCycleGuardTests>();

    /// <summary>
    /// Runs NuruGenerator over the given source with the test process's own
    /// assemblies as metadata references (includes TimeWarp.Nuru).
    /// </summary>
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
        assemblyName: "InterpreterCycleRepro",
        syntaxTrees: [tree],
        references: references,
        options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

      CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new NuruGenerator());
      GeneratorDriver ran = driver.RunGenerators(compilation);
      return ran.GetRunResult();
    }

    /// <summary>
    /// Mid-typing state: a route pattern local that references itself. The generator
    /// must complete (returning diagnostics is fine); it must not recurse to death.
    /// </summary>
    public static async Task Should_survive_self_referential_local()
    {
      const string Source = """
        using TimeWarp.Nuru;

        var pattern = pattern;

        NuruApp.CreateBuilder(args)
          .Map(pattern).WithHandler(() => "hi").AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      // The generator ran to completion without throwing.
      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Mutually-referential locals — same guard, two-symbol cycle.
    /// </summary>
    public static async Task Should_survive_mutually_referential_locals()
    {
      const string Source = """
        using TimeWarp.Nuru;

        var a = b;
        var b = a;

        NuruApp.CreateBuilder(args)
          .Map(a).WithHandler(() => "hi").AsCommand().Done()
          .Build();
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      await Task.CompletedTask;
    }

    /// <summary>
    /// A field self-assignment (`Pattern = Pattern;`) compiles cleanly, and
    /// FindFieldAssignmentInContainingType evaluates the RHS of every assignment to
    /// the field — so resolving the field cycles on VALID user code, not just
    /// mid-typing states. This is the primary regression case: without the sentinel
    /// the generator recursed to death here (verified: StackOverflow pre-fix).
    /// </summary>
    public static async Task Should_survive_field_self_assignment()
    {
      const string Source = """
        using TimeWarp.Nuru;

        CycleApp.Run();

        internal static class CycleApp
        {
          private static string Pattern = "seed";

          public static void Run()
          {
            #pragma warning disable CS1717 // assignment to same variable
            Pattern = Pattern;
            #pragma warning restore CS1717

            NuruApp.CreateBuilder([])
              .Map(Pattern).WithHandler(() => "hi").AsCommand().Done()
              .Build();
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Two static fields assigned from each other — same cycle through a two-symbol
    /// chain, also on cleanly-compiling code.
    /// </summary>
    public static async Task Should_survive_mutually_referential_fields()
    {
      const string Source = """
        using TimeWarp.Nuru;

        SwapApp.Run();

        internal static class SwapApp
        {
          private static string PatternA = "seed-a";
          private static string PatternB = "seed-b";

          public static void Run()
          {
            PatternA = PatternB;
            PatternB = PatternA;

            NuruApp.CreateBuilder([])
              .Map(PatternA).WithHandler(() => "hi").AsCommand().Done()
              .Build();
          }
        }
        """;

      GeneratorDriverRunResult result = RunNuruGenerator(Source);

      result.Results.Length.ShouldBe(1);
      result.Results[0].Exception.ShouldBeNull();

      await Task.CompletedTask;
    }
  }
}
