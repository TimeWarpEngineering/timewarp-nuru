using GroupFilteringSample.Shared;
using TimeWarp.Nuru;

namespace GroupFilteringSample.Git;

public static class Program
{
  public static Task Main(string[] args)
  {
    NuruApp app = NuruApp.CreateBuilder(args)
      .DiscoverEndpoints(typeof(GitGroupBase))
      .Build();

    return app.RunAsync(args);
  }
}
