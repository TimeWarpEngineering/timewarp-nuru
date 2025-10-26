Configuration validation at startup.

```csharp
using System.ComponentModel.DataAnnotations;

public class DatabaseOptions
{
  [Required]
  public string ConnectionString { get; set; } = "";

  [Range(1, 300)]
  public int Timeout { get; set; } = 30;
}

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)
  .ConfigureServices(services =>
  {
    services.AddOptions<DatabaseOptions>()
      .BindConfiguration("Database")
      .ValidateDataAnnotations()
      .ValidateOnStart();  // ✅ Validates during Build()
  })
  .AddRoute<QueryCommand>("query {sql}")
  .Build();  // Throws if configuration is invalid
```

Matches ASP.NET Core behavior. Works with DataAnnotations, custom validation, and FluentValidation.

Requires version 2.1.0-beta.26 or greater.

dotnet add package TimeWarp.Nuru --prerelease