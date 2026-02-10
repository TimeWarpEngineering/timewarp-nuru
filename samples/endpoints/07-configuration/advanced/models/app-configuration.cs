public class AppConfiguration
{
  public ServerConfig Server { get; set; } = new ServerConfig();
  public List<FeatureConfig> Features { get; set; } = [];
  public Dictionary<string, EndpointConfig> Endpoints { get; set; } = new Dictionary<string, EndpointConfig>();
  public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

public class ServerConfig
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8080;
  public bool UseSsl { get; set; } = false;
}

public class FeatureConfig
{
  public string Name { get; set; } = "";
  public bool Enabled { get; set; } = false;
  public int Weight { get; set; } = 100;
}

public class EndpointConfig
{
  public string Url { get; set; } = "";
  public int Timeout { get; set; } = 30;
  public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
