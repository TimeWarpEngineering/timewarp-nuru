namespace TimeWarp.Nuru;

/// <summary>
/// A built TimeWarp.Console application.
/// </summary>
public class NuruApp
{
  private readonly IServiceProvider ServiceProvider;

  public IServiceProvider Services => ServiceProvider;

  public NuruApp(IServiceProvider serviceProvider)
  {
    ServiceProvider = serviceProvider;
  }

  public Task<int> RunAsync(string[] args)
  {
    NuruCli cli = ServiceProvider.GetRequiredService<NuruCli>();
    return cli.RunAsync(args);
  }
}