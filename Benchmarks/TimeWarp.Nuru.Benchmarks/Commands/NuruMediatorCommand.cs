namespace TimeWarp.Nuru.Benchmarks.Commands;

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

public static class NuruMediatorCommand
{
  public static async Task Execute(string[] args)
  {
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        // Register only the specific handler for this benchmark
        services.AddTransient<IRequestHandler<TestCommand>, TestCommand.Handler>();
      })
      .AddRoute<TestCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    // Prepend "test" to the args since Nuru expects a command name
    string[] nuruArgs = new string[args.Length + 1];
    nuruArgs[0] = "test";
    Array.Copy(args, 0, nuruArgs, 1, args.Length);

    await app.RunAsync(nuruArgs);
  }
}

public sealed class TestCommand : IRequest
{
  public string Str { get; set; } = string.Empty;
  public int IntOption { get; set; }
  public bool B { get; set; }

  internal sealed class Handler : IRequestHandler<TestCommand>
  {
    public Task Handle(TestCommand request, CancellationToken cancellationToken)
    {
      // Empty handler for benchmarking
      return Task.CompletedTask;
    }
  }
}
