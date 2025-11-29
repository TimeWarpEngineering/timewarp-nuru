namespace TimeWarp.Nuru;

/// <summary>
/// Simple IHostEnvironment implementation for Nuru applications.
/// </summary>
internal sealed class NuruHostEnvironment : IHostEnvironment
{
  public NuruHostEnvironment(string environmentName, string applicationName, string contentRootPath)
  {
    EnvironmentName = environmentName;
    ApplicationName = applicationName;
    ContentRootPath = contentRootPath;
    ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
  }

  public string EnvironmentName { get; set; }
  public string ApplicationName { get; set; }
  public string ContentRootPath { get; set; }
  public IFileProvider ContentRootFileProvider { get; set; }
}
