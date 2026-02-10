using static System.Console;

public interface IRepository
{
  Task<string> GetAsync(string key);
}

public class DatabaseRepository : IRepository
{
  public async Task<string> GetAsync(string key)
  {
    await Task.Delay(50);
    return $"DB value for {key}";
  }
}

public class CachedRepository(IRepository Inner, ILogger Logger) : IRepository
{
  private readonly Dictionary<string, string> Cache = new();

  public async Task<string> GetAsync(string key)
  {
    if (Cache.TryGetValue(key, out string? cached))
    {
      Logger.Log($"Cache hit for {key}");
      return cached;
    }

    Logger.Log($"Cache miss for {key}");
    string value = await Inner.GetAsync(key);
    Cache[key] = value;
    return value;
  }
}
