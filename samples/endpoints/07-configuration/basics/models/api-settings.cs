using TimeWarp.Nuru;

[ConfigurationKey("Api")]
public class ApiSettings
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
}
