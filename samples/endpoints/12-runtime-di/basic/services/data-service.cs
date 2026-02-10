using static System.Console;

public interface IDataService
{
  Task<string[]> GetDataAsync(string query);
}

public class DataService(IConfigService Config) : IDataService
{
  public async Task<string[]> GetDataAsync(string query)
  {
    WriteLine($"DataService querying: {query}");
    WriteLine($"  Using API: {Config.GetSetting("api.url")}");
    await Task.Delay(100);
    return [$"Result 1 for {query}", $"Result 2 for {query}"];
  }
}
