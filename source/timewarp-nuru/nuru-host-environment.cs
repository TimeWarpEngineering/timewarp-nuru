namespace TimeWarp.Nuru;

/// <summary>
/// Simple IHostEnvironment implementation for Nuru applications.
/// </summary>
internal sealed class NuruHostEnvironment : IHostEnvironment
{
  public string EnvironmentName { get; set; } = "Production";
  public string ApplicationName { get; set; } = "NuruApp";
  public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
  public IFileProvider? ContentRootFileProvider { get; set; }
}
