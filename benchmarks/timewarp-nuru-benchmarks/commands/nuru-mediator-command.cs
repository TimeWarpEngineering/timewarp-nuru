namespace TimeWarp.Nuru.Benchmarks.Commands;

using Mediator;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

public static class NuruMediatorCommand
{
  public static async Task Execute(string[] args)
  {
    // Prepend "test" to the args since Nuru expects a command name
    string[] nuruArgs = new string[args.Length + 1];
    nuruArgs[0] = "test";
    Array.Copy(args, 0, nuruArgs, 1, args.Length);

    NuruCoreApp app = NuruApp.CreateBuilder(nuruArgs)
      .ConfigureServices(services => services.AddMediator())
      .Map<TestCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

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
    public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
    {
      // Empty handler for benchmarking
      return default;
    }
  }
}
