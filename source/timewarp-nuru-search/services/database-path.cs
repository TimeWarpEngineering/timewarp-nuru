namespace TimeWarp.Nuru.Search.Services;

public static class DatabasePath
{
  public static string GetIndexPath()
  {
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string nuruDir = Path.Combine(homeDir, ".nuru");

    if (!Directory.Exists(nuruDir))
    {
      Directory.CreateDirectory(nuruDir);
    }

    return Path.Combine(nuruDir, "index.db");
  }
}
