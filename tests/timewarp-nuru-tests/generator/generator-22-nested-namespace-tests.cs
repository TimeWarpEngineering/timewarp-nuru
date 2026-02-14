#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// Namespacd service with parameterized constructor (mimics timewarp-ganda pattern)
namespace TimeWarp.Zana.Kanban
{
  public interface IWorkspaceService
  {
    string GetPath();
  }

  public class WorkspaceService : IWorkspaceService
  {
    public string GetPath() => "/tmp";
  }

  public interface IKanbanService
  {
    string GetStatus();
  }

  public class KanbanService : IKanbanService
  {
    private readonly IWorkspaceService _workspaceService;

    public KanbanService(IWorkspaceService workspaceService)
    {
      _workspaceService = workspaceService;
    }

    public string GetStatus() => $"Workspace: {_workspaceService.GetPath()}";
  }
}

namespace TimeWarp.Nuru.Tests.Generator.ParameterizedServiceConstructor
{
  using TimeWarp.Zana.Kanban;

  [TestTag("generator")]
  [TestTag("DI")]
  [TestTag("Issue175")]
  public class NestedNamespaceSingletonTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NestedNamespaceSingletonTests>();

    public static async Task Should_resolve_singleton_in_nested_namespace()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IWorkspaceService, WorkspaceService>();
          services.AddSingleton<IKanbanService, KanbanService>();
        })
        .Map("status")
          .WithHandler((IKanbanService kanban) => kanban.GetStatus())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["status"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("Workspace: /tmp").ShouldBeTrue();
    }
  }
}