#!/usr/bin/dotnet --

using Microsoft.Extensions.DependencyInjection;

return await RunTests<DelegateMediatorConsistencyTests>(clearCache: true);

[TestTag("Routing")]
[ClearRunfileCache]
public class DelegateMediatorConsistencyTests
{
  // Test same matching for basic literal (from Section 1)
  public static async Task Should_identical_matching_basic_literal_delegate()
  {
    // Arrange - Delegate
    NuruAppBuilder builder = new();
    bool matched = false;
    builder.AddRoute("status", () => { matched = true; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_identical_matching_basic_literal_mediator()
  {
    // Arrange - Mediator
    NuruAppBuilder builder = new();
    builder.AddDependencyInjection();
    builder.AddRoute<StatusCommand>("status");
    builder.Services.AddTransient<IRequestHandler<StatusCommand>>(_ => new StatusHandler());

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }

  // Test same matching for parameter binding (from Section 2)
  public static async Task Should_identical_matching_string_parameter_delegate()
  {
    // Arrange - Delegate
    NuruAppBuilder builder = new();
    string? boundName = null;
    builder.AddRoute("greet {name}", (string name) => { boundName = name; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);

    // Assert
    exitCode.ShouldBe(0);
    boundName.ShouldBe("Alice");

    await Task.CompletedTask;
  }

  public static async Task Should_identical_matching_string_parameter_mediator()
  {
    // Arrange - Mediator
    NuruAppBuilder builder = new();
    builder.AddDependencyInjection();
    builder.AddRoute<GreetCommand>("greet {name}");
    builder.Services.AddTransient<IRequestHandler<GreetCommand>>(_ => new GreetHandler());

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }

  // Test same error for type mismatch (from Section 2)
  public static async Task Should_identical_error_type_mismatch_delegate()
  {
    // Arrange - Delegate
    NuruAppBuilder builder = new();
    builder.AddRoute("delay {ms:int}", (int _) => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_identical_error_type_mismatch_mediator()
  {
    // Arrange - Mediator
    NuruAppBuilder builder = new();
    builder.AddDependencyInjection();
    builder.AddRoute<DelayCommand>("delay {ms:int}");
    builder.Services.AddTransient<IRequestHandler<DelayCommand>>(_ => new DelayHandler());

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  // Test same matching for optional parameter (from Section 3)
  public static async Task Should_identical_matching_optional_string_delegate()
  {
    // Arrange - Delegate
    NuruAppBuilder builder = new();
    string? boundEnv = null;
    builder.AddRoute("deploy {env?}", (string? env) => { boundEnv = env; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_identical_matching_optional_string_mediator()
  {
    // Arrange - Mediator
    NuruAppBuilder builder = new();
    builder.AddDependencyInjection();
    builder.AddRoute<DeployCommand>("deploy {env?}");
    builder.Services.AddTransient<IRequestHandler<DeployCommand>>(_ => new DeployHandler());

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }
}

// Command definitions (not nested to avoid CA1034)
internal sealed class StatusCommand : IRequest
{
}

internal sealed class StatusHandler : IRequestHandler<StatusCommand>
{
  public Task Handle(StatusCommand request, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}

internal sealed class GreetCommand : IRequest
{
  public string Name { get; set; } = "";
}

internal sealed class GreetHandler : IRequestHandler<GreetCommand>
{
  public Task Handle(GreetCommand request, CancellationToken cancellationToken)
  {
    request.Name.ShouldBe("Alice");
    return Task.CompletedTask;
  }
}

internal sealed class DelayCommand : IRequest
{
  public int Ms { get; set; }
}

internal sealed class DelayHandler : IRequestHandler<DelayCommand>
{
  public Task Handle(DelayCommand request, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}

internal sealed class DeployCommand : IRequest
{
  public string? Env { get; set; }
}

internal sealed class DeployHandler : IRequestHandler<DeployCommand>
{
  public Task Handle(DeployCommand request, CancellationToken cancellationToken)
  {
    request.Env.ShouldBeNull();
    return Task.CompletedTask;
  }
}
