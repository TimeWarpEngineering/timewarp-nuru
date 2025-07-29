namespace TimeWarp.Nuru.Benchmarks.Commands;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

public static class NuruMediatorCommand
{
  public static async Task Execute(string[] args)
  {
    var builder = new NuruAppBuilder()
        .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(TestCommand).Assembly));

    // Add a route that matches the benchmark arguments pattern
    builder.AddRoute<TestCommand>("test --str {str} -i {intOption:int} -b");

    // Prepend "test" to the args since Nuru expects a command name
    string[] nuruArgs = new[] { "test" }.Concat(args).ToArray();

    var app = builder.Build();
    await app.RunAsync(nuruArgs);
  }
}

public class TestCommand : IRequest
{
  public string Str { get; set; } = string.Empty;
  public int IntOption { get; set; }
  public bool B { get; set; }
  
  public class Handler : IRequestHandler<TestCommand>
  {
    public Task Handle(TestCommand request, CancellationToken cancellationToken)
    {
      // Empty handler for benchmarking
      return Task.CompletedTask;
    }
  }
}
