public interface IConfigService
{
  string GetSetting(string key);
}

public class ConfigService : IConfigService
{
  private readonly Dictionary<string, string> Settings = new()
  {
    ["api.url"] = "https://api.example.com",
    ["timeout"] = "30",
    ["retries"] = "3"
  };

  public string GetSetting(string key) => Settings.TryGetValue(key, out string? value) ? value : "";
}
