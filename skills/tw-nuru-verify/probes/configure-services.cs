// Probe: ConfigureServices registers a service that a handler receives by injection.
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services => services.AddSingleton<IGreeter, Greeter>())
  .Map("greet {name}")
    .WithHandler((string name, IGreeter greeter) => greeter.Greet(name))
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

public interface IGreeter
{
  string Greet(string name);
}

public sealed class Greeter : IGreeter
{
  public string Greet(string name) => $"greeter says hello to {name}";
}
