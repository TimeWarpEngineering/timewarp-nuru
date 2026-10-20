#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-build/timewarp-nuru-build.csproj
#:package Microsoft.CodeAnalysis.CSharp
#:package Microsoft.Build.Utilities.Core

#region Purpose
// Regression for kanban 508-013: the JSON context MSBuild task registers JSON types of
// [Operation] endpoints that live in a referenced assembly, with the generator's opt-in rules.
#endregion

#region Design
// The task parses only source files, so it reaches a referenced endpoint through the same
// opt-ins the generator honors: [assembly: NuruEndpointAssembly(typeof(X))] and Map<T>().
// The library is a real DLL on disk because the task loads references by path.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen60JsonContextReferencedEndpoints
{
  using System.Collections;
  using Microsoft.Build.Framework;
  using TaskItem = Microsoft.Build.Utilities.TaskItem;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using Microsoft.CodeAnalysis.Emit;
  using TimeWarp.Nuru.Build;

  [TestTag("generator")]
  public class JsonContextReferencedEndpointTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<JsonContextReferencedEndpointTests>();

    private const string Library = """
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Contracts;
      using TimeWarp.Mediator;

      namespace RefJson;

      public sealed class RefJsonResult
      {
        public int Total { get; set; }
      }

      public sealed class RefJsonPayload
      {
        public string Label { get; set; } = "";
      }

      [Operation("ref-json")]
      public sealed class RefJsonQuery : IQuery<RefJsonResult>
      {
        public RefJsonPayload? Payload { get; set; }

        public sealed class Handler : IQueryHandler<RefJsonQuery, RefJsonResult>
        {
          public Task<RefJsonResult> Handle(RefJsonQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new RefJsonResult());
        }
      }

      public static class RefJsonMarker
      {
      }
      """;

    private static readonly Lazy<string> LibraryPath = new(() => EmitLibrary(Library, "RefJsonLib"));

    public static async Task Should_register_json_types_of_a_named_assembly()
    {
      string context = RunTask("""
        using TimeWarp.Nuru;
        using RefJson;

        [assembly: NuruEndpointAssembly(typeof(RefJsonMarker))]

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """);

      context.ShouldContain("typeof(global::RefJson.RefJsonResult)");
      context.ShouldContain("typeof(global::RefJson.RefJsonPayload)");
      await Task.CompletedTask;
    }

    public static async Task Should_register_json_types_of_a_mapped_referenced_endpoint()
    {
      string context = RunTask("""
        using TimeWarp.Nuru;
        using RefJson;

        NuruApp app = NuruApp.CreateBuilder()
          .Map<RefJsonQuery>()
          .Build();

        return await app.RunAsync(args);
        """);

      context.ShouldContain("typeof(global::RefJson.RefJsonResult)");
      context.ShouldContain("typeof(global::RefJson.RefJsonPayload)");
      await Task.CompletedTask;
    }

    public static async Task Should_skip_a_referenced_assembly_without_opt_in()
    {
      string context = RunTask("""
        using TimeWarp.Nuru;

        NuruApp app = NuruApp.CreateBuilder()
          .DiscoverEndpoints()
          .Build();

        return await app.RunAsync(args);
        """);

      context.ShouldNotContain("RefJson");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Runs the task over one source file and returns the generated context, or empty.
    /// </summary>
    private static string RunTask(string source)
    {
      string directory = Path.Combine(Path.GetTempPath(), $"gen60-{Guid.NewGuid():N}");
      Directory.CreateDirectory(directory);
      string sourcePath = Path.Combine(directory, "app.cs");
      File.WriteAllText(sourcePath, source);

      List<ITaskItem> references = [.. ReferencePaths().Select(static path => new TaskItem(path))];
      references.Add(new TaskItem(LibraryPath.Value));

      RecordingBuildEngine engine = new();
      GenerateNuruJsonContextTask task = new()
      {
        BuildEngine = engine,
        SourceFiles = [new TaskItem(sourcePath)],
        References = [.. references],
        IntermediateOutputPath = directory
      };

      task.Execute().ShouldBeTrue(string.Join("\n", engine.Errors));
      if (task.GeneratedFiles.Length == 0)
        return "";

      return File.ReadAllText(task.GeneratedFiles[0].ItemSpec);
    }

    private static IEnumerable<string> ReferencePaths()
    {
      string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
      return tpa.Split(Path.PathSeparator).Where(static path => path.Length > 0);
    }

    private static string EmitLibrary(string source, string assemblyName)
    {
      SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: assemblyName + ".cs");
      CSharpCompilation compilation = CSharpCompilation.Create(
        assemblyName: assemblyName,
        syntaxTrees: [tree],
        references: [.. ReferencePaths().Select(static path => MetadataReference.CreateFromFile(path))],
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

      string path = Path.Combine(Path.GetTempPath(), $"{assemblyName}-{Guid.NewGuid():N}.dll");
      EmitResult emitResult;
      using (FileStream stream = new(path, FileMode.Create, FileAccess.Write))
      {
        emitResult = compilation.Emit(stream);
      }

      emitResult.Success.ShouldBeTrue(
        string.Join("\n", emitResult.Diagnostics.Select(static d => d.ToString())));
      return path;
    }

    private sealed class RecordingBuildEngine : IBuildEngine
    {
      public List<string> Errors { get; } = [];

      public bool ContinueOnError => false;

      public int LineNumberOfTaskNode => 0;

      public int ColumnNumberOfTaskNode => 0;

      public string ProjectFileOfTaskNode => "gen60.csproj";

      public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => false;

      public void LogCustomEvent(CustomBuildEventArgs e) { }

      public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message ?? "");

      public void LogMessageEvent(BuildMessageEventArgs e) { }

      public void LogWarningEvent(BuildWarningEventArgs e) { }
    }
  }
}
