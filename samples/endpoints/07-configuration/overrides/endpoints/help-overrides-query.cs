using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("help-overrides", Description = "Show how to use command-line overrides")]
public sealed class HelpOverridesQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<HelpOverridesQuery, Unit>
  {
    public ValueTask<Unit> Handle(HelpOverridesQuery query, CancellationToken ct)
    {
      WriteLine(@"Command-Line Configuration Overrides
 =====================================

 Override any configuration value using --Section:Key=Value syntax:

 Examples:
   --Database:Host=prod-db.example.com
   --Database:Port=5433
   --Api:TimeoutSeconds=60
   --Logging:LogLevel:Default=Debug

 Override Files:
   You can also use a .overrides.json file with the same structure.

 Try it:
   ./overrides.cs --Database:Host=prod-db config-show
 ");

      return default;
    }
  }
}
