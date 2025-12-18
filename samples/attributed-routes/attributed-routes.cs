// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTES - AUTO-REGISTRATION EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates attributed routes with auto-registration:
// - No Map() calls needed - routes are discovered via [NuruRoute] attribute
// - Parameters and options defined via [Parameter] and [Option] attributes
// - Grouped routes via [NuruRouteGroup] on base classes
// - Route aliases via [NuruRouteAlias]
//
// HOW IT WORKS:
//   The NuruAttributedRouteGenerator source generator scans for classes with
//   [NuruRoute] and generates:
//   1. CompiledRouteBuilder calls for each route
//   2. ModuleInitializer code to register routes with NuruRouteRegistry
//   3. Route pattern strings for help display
//
// At app.Build() time, routes from NuruRouteRegistry are added to the
// endpoint collection automatically.
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

// No Map() calls! Routes are auto-registered via [NuruRoute] attributes
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddMediator();
  })
  .AddAutoHelp()
  .WithMetadata("attributed-routes", "Sample demonstrating attributed routes")
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// SIMPLE REQUESTS - Basic attributed routes
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Simple greeting request with a required parameter.
/// </summary>
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetRequest : IRequest
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<GreetRequest>
  {
    public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct)
    {
      WriteLine($"Hello, {request.Name}!");
      return default;
    }
  }
}

/// <summary>
/// Deploy request with a required parameter and options.
/// </summary>
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : IRequest
{
  [Parameter(Description = "Target environment (dev, staging, prod)")]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f", Description = "Skip confirmation prompt")]
  public bool Force { get; set; }

  [Option("config", "c", Description = "Path to config file")]
  public string? ConfigFile { get; set; }

  [Option("replicas", "r", Description = "Number of replicas")]
  public int Replicas { get; set; } = 1;

  public sealed class Handler : IRequestHandler<DeployRequest>
  {
    public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct)
    {
      WriteLine($"Deploying to {request.Env}...");
      WriteLine($"  Force: {request.Force}");
      WriteLine($"  Config: {request.ConfigFile ?? "(default)"}");
      WriteLine($"  Replicas: {request.Replicas}");
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DEFAULT ROUTE - Matches when no other route matches
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Default route that shows usage when no arguments provided.
/// </summary>
[NuruRoute("", Description = "Show usage information")]
public sealed class DefaultRequest : IRequest
{
  [Option("verbose", "v", Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IRequestHandler<DefaultRequest>
  {
    public ValueTask<Unit> Handle(DefaultRequest request, CancellationToken ct)
    {
      WriteLine("Attributed Routes Sample");
      WriteLine("========================");
      WriteLine();
      WriteLine("Commands:");
      WriteLine("  greet <name>              Greet someone");
      WriteLine("  deploy <env> [options]    Deploy to environment");
      WriteLine("  goodbye, bye, cya         Say goodbye and exit");
      WriteLine();
      WriteLine("Docker Commands:");
      WriteLine("  docker run <image>        Run a container");
      WriteLine("  docker build <path>       Build an image");
      WriteLine();
      if (request.Verbose)
      {
        WriteLine("Run 'attributed-routes --help' for detailed help.");
      }
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ALIASES - Multiple patterns for the same request
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Goodbye request with multiple aliases.
/// Note: We use "goodbye" instead of "exit" because the REPL already registers
/// built-in exit/quit/q commands. This demonstrates [NuruRouteAlias] without conflicts.
/// </summary>
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeRequest : IRequest
{
  public sealed class Handler : IRequestHandler<GoodbyeRequest>
  {
    public ValueTask<Unit> Handle(GoodbyeRequest request, CancellationToken ct)
    {
      WriteLine("Goodbye! Thanks for using attributed routes.");
      Environment.Exit(0);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GROUPED ROUTES - Base class with shared prefix and options
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base class for Docker commands - defines group prefix and shared options.
/// </summary>
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D", Description = "Enable debug mode")]
  public bool Debug { get; set; }
}

/// <summary>
/// Docker run command - inherits 'docker' prefix and --debug option.
/// </summary>
[NuruRoute("run", Description = "Run a Docker container")]
public sealed class DockerRunRequest : DockerRequestBase, IRequest
{
  [Parameter(Description = "Image name to run")]
  public string Image { get; set; } = string.Empty;

  [Option("detach", "d", Description = "Run in background")]
  public bool Detach { get; set; }

  public sealed class Handler : IRequestHandler<DockerRunRequest>
  {
    public ValueTask<Unit> Handle(DockerRunRequest request, CancellationToken ct)
    {
      WriteLine($"Running container: {request.Image}");
      WriteLine($"  Debug: {request.Debug}");
      WriteLine($"  Detach: {request.Detach}");
      return default;
    }
  }
}

/// <summary>
/// Docker build command - inherits 'docker' prefix and --debug option.
/// </summary>
[NuruRoute("build", Description = "Build a Docker image")]
public sealed class DockerBuildRequest : DockerRequestBase, IRequest
{
  [Parameter(Description = "Path to Dockerfile directory")]
  public string Path { get; set; } = ".";

  [Option("tag", "t", Description = "Tag for the image")]
  public string? Tag { get; set; }

  public sealed class Handler : IRequestHandler<DockerBuildRequest>
  {
    public ValueTask<Unit> Handle(DockerBuildRequest request, CancellationToken ct)
    {
      WriteLine($"Building image from: {request.Path}");
      WriteLine($"  Debug: {request.Debug}");
      WriteLine($"  Tag: {request.Tag ?? "(none)"}");
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CATCH-ALL PARAMETER - Captures remaining arguments
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Execute command with catch-all parameter for arbitrary arguments.
/// </summary>
[NuruRoute("exec", Description = "Execute a command with arguments")]
public sealed class ExecRequest : IRequest
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments to execute")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : IRequestHandler<ExecRequest>
  {
    public ValueTask<Unit> Handle(ExecRequest request, CancellationToken ct)
    {
      WriteLine($"Executing: {string.Join(" ", request.Args)}");
      return default;
    }
  }
}
