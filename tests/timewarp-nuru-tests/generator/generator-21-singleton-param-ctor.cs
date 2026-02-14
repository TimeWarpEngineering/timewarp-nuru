#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// Service with parameterized constructor
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

namespace TimeWarp.Nuru.Tests.Generator.ParameterizedServiceConstructor
{
  [TestTag("generator")]
  [TestTag("DI")]
  [TestTag("Issue175")]
  public class SingletonParameterizedConstructorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SingletonParameterizedConstructorTests>();

    public static async Task Should_resolve_singleton_with_parameterized_constructor()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IWorkspaceService, WorkspaceService>();
          services.AddSingleton<IKanbanService, KanbanService>(); // Singleton with param
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