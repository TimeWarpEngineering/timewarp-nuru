using GroupFilteringSample.Shared;
using TimeWarp.Nuru;

namespace GroupFilteringSample.Kanban;

public static class Program
{
  public static Task Main(string[] args)
  {
    NuruApp app = NuruApp.CreateBuilder(args)
      .DiscoverEndpoints(typeof(KanbanGroupBase))
      .Build();

    return app.RunAsync(args);
  }
}
