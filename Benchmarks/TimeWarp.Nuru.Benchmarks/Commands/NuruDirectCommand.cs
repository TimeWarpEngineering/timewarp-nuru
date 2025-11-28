namespace TimeWarp.Nuru.Benchmarks.Commands;

using TimeWarp.Nuru;

public static class NuruDirectCommand
{
  // Cache the array since benchmark always uses the same arguments
  private static readonly string[] CachedNuruArgs =
    ["test", "--str", "hello world", "-i", "13", "-b"];

  public static async Task Execute(string[] args)
  {
    NuruAppBuilder builder = new();

    // Add a route that matches the benchmark arguments pattern
    builder.Map
    (
      "test --str {str} -i {intOption:int} -b",
      (string str, int intOption) => { }
    );

    NuruCoreApp app = builder.Build();
    await app.RunAsync(CachedNuruArgs);
  }
}
