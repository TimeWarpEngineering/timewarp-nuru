# Hello World - ASP.NET Core Style CLI

**TimeWarp.Nuru** brings the familiar ASP.NET Core minimal API pattern to CLI development.

## ASP.NET Core Web API
```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();

app.MapGet("/", () => "Hello World");

app.Run();
```

## TimeWarp.Nuru CLI
```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru

using TimeWarp.Nuru;

NuruAppBuilder builder = NuruApp.CreateBuilder(args);

builder.Map("hello", () => "Hello World");

await builder.Build().RunAsync(args);
```

```bash
$ ./hello-world.cs hello
Hello World
```

## The Familiar Pattern

If you know ASP.NET Core, you already know Nuru:

| ASP.NET Core | TimeWarp.Nuru |
|--------------|---------------|
| `WebApplication.CreateBuilder(args)` | `NuruApp.CreateBuilder(args)` |
| `app.MapGet("/path", handler)` | `builder.Map("command", handler)` |
| `app.Run()` | `await app.RunAsync(args)` |

## Three Builder Options

```csharp
// Full featured: DI, Config, Mediator, REPL, Completion
NuruAppBuilder builder = NuruApp.CreateBuilder(args);

// Lightweight: Auto-help, AOT-friendly
NuruAppBuilder builder = NuruApp.CreateSlimBuilder();

// Bare minimum: Total control
NuruAppBuilder builder = NuruApp.CreateEmptyBuilder();
```

## Get Started

```bash
dotnet add package TimeWarp.Nuru
```

---

*Nuru means "light" in Swahili - illuminating the path to your commands.*
