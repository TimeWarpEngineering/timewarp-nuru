namespace TimeWarp.Nuru.Benchmarks.Commands;

using TimeWarp.Nuru;

public static class NuruCommand
{
  public static async Task Execute(string[] args)
  {
    var builder = new NuruAppBuilder()
        .AddDependencyInjection();

    // Add a route that matches the benchmark arguments pattern
    builder.AddRoute("test --str {str} -i {intOption:int} -b",
        (string str, int intOption) => { });

    // Prepend "test" to the args since Nuru expects a command name
    string[] nuruArgs = new[] { "test" }.Concat(args).ToArray();

    var app = builder.Build();
    await app.RunAsync(nuruArgs);
  }
}
