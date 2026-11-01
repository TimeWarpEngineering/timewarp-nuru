using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("config", Description = "Read configuration value")]
public sealed class ConfigQuery : IQuery<string>
{
  [Parameter] public string Key { get; set; } = "";

  public sealed class Handler(IConfigService Config) : IRequestHandler<ConfigQuery, string>
  {
    public ValueTask<string> Handle(ConfigQuery q, CancellationToken ct)
    {
      string value = Config.GetSetting(q.Key);
      WriteLine($"{q.Key} = {value}");
      return ValueTask.FromResult<string>(value);
    }
  }
}
