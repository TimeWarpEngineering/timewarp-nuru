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
    bool matched = false;
    NuruApp app = new NuruAppBuilder()
      .Map("status", () => { matched = true; return 0; })
      .Build();

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
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddTransient<IRequestHandler<StatusCommand>, StatusHandler>();
      })
      .Map<StatusCommand>("status")
      .Build();

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
    string? boundName = null;
    NuruApp app = new NuruAppBuilder()
      .Map("greet {name}", (string name) => { boundName = name; return 0; })
      .Build();

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
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddTransient<IRequestHandler<GreetCommand>, GreetHandler>();
      })
      .Map<GreetCommand>("greet {name}")
      .Build();

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
    NuruApp app = new NuruAppBuilder()
    .Map("delay {ms:int}", (int _) => 0)
    .Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_identical_error_type_mismatch_mediator()
  {
    // Arrange - Mediator
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddTransient<IRequestHandler<DelayCommand>, DelayHandler>();
      })
      .Map<DelayCommand>("delay {ms:int}")
      .Build();

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
    string? boundEnv = null;
    NuruApp app = new NuruAppBuilder()
      .Map("deploy {env?}", (string? env) => { boundEnv = env; return 0; })
      .Build();

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
    NuruApp app = new NuruAppBuilder()
      .AddDependencyInjection()
      .ConfigureServices(services =>
      {
        services.AddTransient<IRequestHandler<DeployCommand>, DeployHandler>();
      })
      .Map<DeployCommand>("deploy {env?}")
      .Build();

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
