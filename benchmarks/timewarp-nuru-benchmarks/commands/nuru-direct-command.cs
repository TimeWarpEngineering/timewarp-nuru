namespace TimeWarp.Nuru.Benchmarks.Commands;

using TimeWarp.Nuru;

// Nuru's only builder is CreateBuilder: CreateSlimBuilder and CreateEmptyBuilder were removed in
// January 2026, and the generator now emits only what the app uses. The route and argv match
// every other framework in CliFrameworkBenchmark (task 501).
public static class NuruDirectCommand
{
  public static Task<int> Execute(string[] args)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .Map("--str {str} -i {i:int} -b")
        .WithHandler((string str, int i) => { })
        .AsQuery()
        .Done()
      .Build();

    return app.RunAsync(args);
  }
}
