using static System.Console;

public interface IProcessingService
{
  Task<string> ProcessAsync(string input);
}

public class ProcessingService(IDataService Data, IConfigService Config) : IProcessingService
{
  public async Task<string> ProcessAsync(string input)
  {
    WriteLine($"ProcessingService processing: {input}");
    WriteLine($"  Timeout: {Config.GetSetting("timeout")}s");

    string[] data = await Data.GetDataAsync(input);
    return $"Processed: {string.Join(", ", data)}";
  }
}
