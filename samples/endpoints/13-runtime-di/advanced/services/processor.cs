public interface IProcessor
{
  Task<string> ProcessAsync(string input);
}

public class FastProcessor : IProcessor
{
  public Task<string> ProcessAsync(string input) =>
    Task.FromResult($"Fast result for {input}");
}

public class ThoroughProcessor : IProcessor
{
  public async Task<string> ProcessAsync(string input)
  {
    await Task.Delay(200);
    return $"Thorough analysis of {input}";
  }
}
