using TimeWarp.Nuru;

namespace TimeWarp.Nuru.Benchmarks.Commands;

public static class NuruDirectCommand
{
  public static async Task Execute(string[] args)
  {
    DirectAppBuilder builder = new();
    
    // Add a route that matches the benchmark arguments pattern
    builder.AddRoute
    (
      "test --str {str} -i {intOption:int} -b",
      (string str, int intOption) => { }
    );
    
    // Prepend "test" to the args since Nuru expects a command name
    string[] nuruArgs = new[] { "test" }.Concat(args).ToArray();
    
    DirectApp app = builder.Build();
    await app.RunAsync(nuruArgs);
  }
}