# TimeWarp.Nuru: Illuminating Your Path to CLI Superpowers

I've been building command-line tools for years, and honestly? Most CLI frameworks feel like they're stuck in 2005. You write more boilerplate than actual logic, and by the time you're done, you've forgotten what problem you were trying to solve.

So I built TimeWarp.Nuru. 

Nuru means "light" in Swahili - and that's exactly what it brings to your command-line development. Like a superhero's origin story, it started with frustration and ended with newfound powers.

## The Problem

Ever notice how web frameworks let you define routes like this:

```csharp
app.MapGet("/users/{id}", (int id) => GetUser(id));
```

But then you go to build a CLI and suddenly you're writing:

```csharp
[Command("users")]
public class UsersCommand : Command<UsersCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<id>")]
        public int Id { get; set; }
    }
    
    public override int Execute(CommandContext context, Settings settings)
    {
        // Finally, your actual code
        return 0;
    }
}
```

That's... a lot of ceremony for "take an ID and do something."

## Enter Nuru

With TimeWarp.Nuru, you write CLI commands like web routes:

```csharp
var app = new DirectAppBuilder()
    .AddRoute("users {id:int}", (int id) => GetUser(id))
    .AddRoute("deploy {env} --dry-run", (string env) => DeployDryRun(env))
    .AddRoute("backup {*files}", (string[] files) => BackupFiles(files))
    .Build();

await app.RunAsync(args);
```

That's it. Your entire CLI app.

## But What About Complex Apps?

Fair question. Sometimes you need dependency injection, unit testing, and all that enterprise goodness. That's why Nuru gives you two modes:

**Direct Mode**: For when you just want to get stuff done
```csharp
.AddRoute("status", () => Console.WriteLine("All systems operational"))
```

**Mediator Mode**: For when you need structure
```csharp
builder.Services.AddSingleton<IDeploymentService, DeploymentService>();
builder.AddRoute<DeployCommand>("deploy {environment} --strategy {strategy}");
```

The kicker? You can use both in the same app. Simple commands stay simple. Complex commands get the full treatment.

## Real-World Example

Here's a deployment tool I built last week:

```csharp
var builder = new AppBuilder();

// Simple commands: Direct
builder.AddRoute("status", () => ShowStatus());
builder.AddRoute("version", () => Console.WriteLine("v1.0.0"));

// Complex commands: Mediator with DI
builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
builder.Services.AddSingleton<IDeploymentService, DeploymentService>();

builder.AddRoute<DeployCommand>("deploy {cluster} {app} --tag {tag}");
builder.AddRoute<RollbackCommand>("rollback {cluster} {app} --to {version}");

var app = builder.Build();
return await app.RunAsync(args);
```

The status command? One line. The deployment logic? Properly structured with services, logging, and error handling. Best of both worlds.

## Performance? Yeah, We Got That

Look, I'm not going to bore you with benchmark charts. But when you remove unnecessary abstractions, good things happen. The Direct mode is fast. Really fast. And it compiles to tiny Native AOT binaries that start instantly.

Perfect for containerized environments where every millisecond of cold start matters.

## Your CLI Superpower

Think of Nuru as your CLI utility belt. Just like every superhero needs the right tools, every developer needs a framework that amplifies their abilities, not hinders them.

Tools should get out of your way. You shouldn't need a 50-page manual to parse command-line arguments. You shouldn't need three classes to handle a simple command. 

Write the code that matters. Let Nuru handle the boring stuff. That's your superpower - focus on solving problems, not fighting frameworks.

## Try It

```bash
dotnet new console -n MyCli
cd MyCli
dotnet add package TimeWarp.Nuru
```

Then:

```csharp
using TimeWarp.Nuru;

var app = new DirectAppBuilder()
    .AddRoute("greet {name}", (string name) => 
        Console.WriteLine($"Hello, {name}!"))
    .Build();

return await app.RunAsync(args);
```

```bash
dotnet run -- greet World
# Output: Hello, World!
```

That's a complete CLI app in 6 lines.

## What's Next?

Nuru 1.0 is out now. It's stable, it's fast, and it's free (Unlicense - use it however you want).

1.0 does everything I need for my daily work. Maybe it'll do the same for you.

Check it out: [github.com/TimeWarpEngineering/timewarp-nuru](https://github.com/TimeWarpEngineering/timewarp-nuru)

And if you build something cool with it, let me know. I'd love to see what you create.

---

*Ready to unlock your CLI superpowers? TimeWarp.Nuru - bringing light to the command line.*