using static System.Console;

public interface IAnalyzer
{
  string Analyze(string data);
}

public class DataAnalyzer(string Name, ILogger Logger) : IAnalyzer
{
  public string Analyze(string data)
  {
    Logger.Log($"{Name} analyzing: {data}");
    return $"Analysis by {Name}: {data.Length} chars";
  }
}
