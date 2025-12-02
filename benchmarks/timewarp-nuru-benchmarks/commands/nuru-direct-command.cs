namespace TimeWarp.Nuru.Benchmarks.Commands;

using TimeWarp.Nuru;

public static class NuruDirectCommand
{
  // Cache the array since benchmark always uses the same arguments
  private static readonly string[] CachedNuruArgs =
    ["test", "--str", "hello world", "-i", "13", "-b"];

  public static async Task Execute(string[] args)
  {
    // Use CreateEmptyBuilder for fastest benchmark (no DI, no configuration)
    NuruCoreApp app = NuruCoreApp.CreateEmptyBuilder(CachedNuruArgs)
      // Add a route that matches the benchmark arguments pattern
      .Map
      (
        "test --str {str} -i {intOption:int} -b",
        (string str, int intOption) => { }
      )
      .Build();

    await app.RunAsync(CachedNuruArgs);
  }
}
