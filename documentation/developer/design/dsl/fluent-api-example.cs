#!/usr/bin/env dotnet run
// GOAL: Generate ALL deterministic code at compile-time via source generators
// Nondeterministic items: 
//  args passed at runtime only
//  environment variables
//  config files
//
// This example demonstrates the Fluent DSL for Nuru

// Reference: .agent/workspace/2024-12-25T01-00-00_v2-generator-architecture.md
// Reference: sandbox/experiments/manual-runtime-construction.cs

using TestTerminal terminal = new();

#region DSL for Nuru route mapping
// This should become dead code once the generator is working correctly
// and the generated code is used at runtime instead.
// The generator looks for this syntax to extract route and handler info.
// Note: This code is never executed at runtime.
// It only exists to be parsed by the source generator.
// we expect the linker to remove it entirely from the final binary.
// AOT compilers should be able to eliminate this code path.

NuruApp app = NuruApp.CreateBuilder(args)
  #region Configuration
  // we support appsettings, environment variables, command-line args
  // configuration settings 
  .AddConfiguration()
  #endregion
  #region Dependency injection and services
  // Consumer is familiar with IServiceCollection pattern
  // We won't actually use DI at runtime, but this is syntax for the generator
  .ConfigureServices
  (    
    // If consumer code injects services, which we could determine as anything other than the route parameters, 
    // and options. We may not even need them to explicitly register DI if we can infer from usage.
    // But for now, keep it explicit.
    
    // Consumer can register other services as needed here.
    services => services
    .AddLogging(builder => builder.AddConsole()) // logging services
    .AddSingleton<MyService>()
  )
  #endregion
  #region Logging
  // If Consumer Handlers use ILogger<T> for structured logging
  // the generator will generate code to create loggers similar to below.
  // var logger = loggerFactory?.CreateLogger<StatusHandler>() 
  //      ?? NullLogger<StatusHandler>.Instance;
  #endregion
  #region Behaviors / Middleware
  // Note that order matters for behaviors
  .AddBehavior(typeof(TelemetryBehavior<,>))
  .AddBehavior(typeof(ValidationBehavior<,>))
  #endregion
  #region Terminal 
   // We will store terminal on NuruApp (minimal state)
  .UseTerminal(terminal)
  #endregion
  
  #region Help
  // Enable auto-generated help route
  // This should generate the help invoker based on registered routes
  .AddHelp(options => { options.ShowPerCommandHelpRoutes = false; })  
  #endregion
  #region REPL Support
  // Enable REPL support with custom options 
  .AddRepl(options => { options.Prompt = "my-app> "; })  // or just .AddRepl() for defaults
  #endregion
  #region Metadata
  .WithName("my app")
  .WithDescription("Does Cool Things")
  .WithAiPrompt("Use queries before commands.")
  #endregion
  #region Route mappings
  .Map("status")
    .WithHandler
    (
      // Generator sees ILogger<StatusHandler> in the handler lambda and generates injection code.
      (ILogger<StatusHandler> logger) =>
      { 
        logger.LogInformation("Status checked");
        return "healthy"; 
      }
    )
    .WithDescription("Check application status")
    .AsQuery() // used for --capabilities output and help
    .Done()
  .WithGroupPrefix("admin")
    .Map("restart")
      .WithHandler(() => "restarting...")
      .WithDescription("Restart the application")
      .AsCommand()
      .Done()
    .WithGroupPrefix("config")
      .Map("get {key}")
        .WithHandler((string key) => $"value-of-{key}")
        .WithDescription("Get configuration value by key")
        .AsQuery()
        .Done()
      .Map("set {key} {value}")
        .WithHandler((string key, string value) => $"set {key} to {value}")
        .WithDescription("Set configuration value by key")
        .AsIdempotentCommand()
        .Done()
      .Done() // end config group
    .Done() // end admin group
  .Map("my-command")
    .WithAlias("my-cmd")
    .WithHandler(() => "create resource")
    .WithDescription("Run my command")
    .AsCommand() 
    .Done()
  .Map("my-idempotent-command")
    .WithHandler(() => "set value idempotently")
    .WithDescription("Run my idempotent command")
    .AsIdempotentCommand()
    .Done()
    // Note we also have attribute-based routes in attributed-routes.cs
    // those should also be picked up by the generator
    // thus no need for Map<T>() calls 
  #endregion
  .Build();
#endregion

#region Intercept RunAsync to use generated invokers
// RunAsync is intercepted by the generated code in Nuru
int exitCode = await app.RunAsync(["status"]);

// normal consumer code would be
// return await app.RunAsync(args);
#endregion

#region Validate output
// Normal code hereafter - validate output
WriteLine($"Exit code: {exitCode}");
WriteLine($"Terminal output: {terminal.Output}");

exitCode.ShouldBe(0);
terminal.OutputContains("healthy").ShouldBeTrue();
#endregion

WriteLine("✓ Fluent DSL test passed");

return exitCode;
