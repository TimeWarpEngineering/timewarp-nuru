using GroupFilteringSample.Shared;
using TimeWarp.Nuru;

namespace GroupFilteringSample.Ganda;

public static class Program
{
  public static Task Main(string[] args)
  {
    NuruApp app = NuruApp.CreateBuilder(args)
      .DiscoverEndpoints()
      .Build();

    return app.RunAsync(args);
  }
}
