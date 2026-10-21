#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Task 508-009: enums declared inside a class (nested types) must produce the same allowedValues
// as namespace-level enums — in app.Capabilities, --capabilities, and generated completion.
// The app is built in a helper, so runs go through ExecuteAsync (RunAsync is intercepted per call site).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesNestedEnum
{

[TestTag("Capabilities")]
public class CapabilitiesNestedEnumTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesNestedEnumTests>();

  private static NuruApp CreateApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ne-deploy {target:NeTarget} --tier {tier}")
        .WithHandler((NeOuter.NeTarget target, NeOuter.NeInner.NeTier tier) => $"{target}:{tier}")
        .AsCommand().Done()
      .Map("ne-maybe {target:NeTarget?}")
        .WithHandler((NeOuter.NeTarget? target) => target?.ToString() ?? "none")
        .AsQuery().Done()
      .Map("ne-mode {mode:NeMode}")
        .WithHandler((NeGeneric<int>.NeMode mode) => mode.ToString())
        .AsQuery().Done()
      .Map<NeEndpointCommand>()
      .EnableCompletion()
      .Build();
    return app;
  }

  public static async Task Should_include_allowed_values_for_nested_enum_parameter_and_option()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    EndpointCapability deploy = app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("ne-deploy", StringComparison.Ordinal));

    deploy.Parameters.Single(p => p.Name == "target").AllowedValues.ShouldBe(["Alpha", "Beta"]);
    deploy.Options.Single(o => o.Name == "tier").AllowedValues.ShouldBe(["Free", "Pro", "Enterprise"]);

    await Task.CompletedTask;
  }

  public static async Task Should_keep_allowed_values_for_nullable_nested_enum()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    EndpointCapability maybe = app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("ne-maybe", StringComparison.Ordinal));

    ParameterCapability target = maybe.Parameters.Single(p => p.Name == "target");
    target.AllowedValues.ShouldBe(["Alpha", "Beta"]);
    target.Type.ShouldBe("NeTarget?");
    target.Required.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_include_allowed_values_for_enum_nested_in_generic_type()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    EndpointCapability mode = app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("ne-mode", StringComparison.Ordinal));

    mode.Parameters.Single().AllowedValues.ShouldBe(["Fast", "Safe"]);

    await Task.CompletedTask;
  }

  public static async Task Should_include_allowed_values_for_nested_enum_endpoint_properties()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    EndpointCapability endpoint = app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("ne-endpoint", StringComparison.Ordinal));

    endpoint.Options.Single(o => o.Name == "target").AllowedValues.ShouldBe(["Alpha", "Beta"]);
    endpoint.Options.Single(o => o.Name == "tier").AllowedValues.ShouldBe(["Free", "Pro", "Enterprise"]);

    await Task.CompletedTask;
  }

  public static async Task Should_include_nested_enum_allowed_values_in_capabilities_output()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    await app.ExecuteAsync(["--capabilities"]);

    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(terminal.AllOutput, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    response.ShouldNotBeNull();
    EndpointCapability deploy = response.Endpoints.Single(e => e.Pattern.StartsWith("ne-deploy", StringComparison.Ordinal));
    deploy.Parameters.Single(p => p.Name == "target").AllowedValues.ShouldBe(["Alpha", "Beta"]);
    deploy.Options.Single(o => o.Name == "tier").AllowedValues.ShouldBe(["Free", "Pro", "Enterprise"]);
  }

  public static async Task Should_run_with_nested_enum_values()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    int exitCode = await app.ExecuteAsync(["ne-deploy", "Beta", "--tier", "Pro"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Beta:Pro").ShouldBeTrue();
  }

  public static async Task Should_complete_nested_enum_parameter_values()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    int exitCode = await app.ExecuteAsync(["__complete", "2", "app", "ne-deploy"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Alpha").ShouldBeTrue();
    terminal.OutputContains("Beta").ShouldBeTrue();
  }
}

public static class NeOuter
{
  public enum NeTarget
  {
    Alpha,
    Beta
  }

  public static class NeInner
  {
    public enum NeTier
    {
      Free,
      Pro,
      Enterprise
    }
  }
}

#pragma warning disable CA1724 // Test-only nested generic container.
public static class NeGeneric<T>
{
  public enum NeMode
  {
    Fast,
    Safe
  }
}
#pragma warning restore CA1724

[NuruRoute("ne-endpoint", Description = "Nested enum endpoint")]
public sealed class NeEndpointCommand : ICommand<Unit>
{
  [Option("target", Description = "Target")]
  public NeOuter.NeTarget Target { get; set; }

  [Option("tier", Description = "Tier")]
  public NeOuter.NeInner.NeTier? Tier { get; set; }

  public sealed class Handler : ICommandHandler<NeEndpointCommand, Unit>
  {
    public Task<Unit> Handle(NeEndpointCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      _ = cancellationToken;
      return Unit.Task;
    }
  }
}

}
