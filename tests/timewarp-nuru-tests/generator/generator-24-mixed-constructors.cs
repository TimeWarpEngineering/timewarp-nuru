#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// Service with BOTH parameterless and parameterized constructors
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
  private readonly IWorkspaceService? _workspaceService;

  // Parameterless constructor (should take dependency from DI)
  public KanbanService()
  {
    _workspaceService = null;
  }

  // Parameterized constructor
  public KanbanService(IWorkspaceService workspaceService)
  {
    _workspaceService = workspaceService;
  }

  public string GetStatus() => _workspaceService is null
    ? "No workspace"
    : $"Workspace: {_workspaceService.GetPath()}";
}

namespace TimeWarp.Nuru.Tests.Generator.ParameterizedServiceConstructor
{
  using TestTerminal = TimeWarp.Terminal.TestTerminal;

  [TestTag("generator")]
  [TestTag("DI")]
  [TestTag("Issue175")]
  public class MixedConstructorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MixedConstructorTests>();

    public static async Task Should_resolve_service_with_both_constructors()
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
      // With both constructors, should use parameterless (the one it finds first)
      // This test just checks compilation - actual behavior depends on constructor selection
    }
  }
}