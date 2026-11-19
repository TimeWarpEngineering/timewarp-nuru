#!/usr/bin/env -S dotnet --
#:property EnableConfigurationBindingGenerator=true

#region Purpose
// Pins that an endpoint Handler constructor receives IOptions<T> from configuration,
// the same binding a Fluent delegate parameter already gets (task 591-004).
#endregion

#region Design
// Map<T>() keeps this file out of other apps in the Jaribu multi assembly (NURU_A003).
// Types are prefixed E591 so they do not collide with generator-13's DatabaseOptions.
// Validation uses try/catch: interceptors cannot intercept a call inside a lambda (#367).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.EndpointHandlerIOptions
{
  /// <summary>
  /// Endpoint <c>Handler</c> constructors bind <c>IOptions&lt;T&gt;</c> from configuration.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("IOptions")]
  [TestTag("Endpoints")]
  public class EndpointHandlerIOptionsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EndpointHandlerIOptionsTests>();

    /// <summary>
    /// Convention strips a trailing <c>Options</c>: <c>E591DatabaseOptions</c> binds <c>E591Database</c>.
    /// </summary>
    public static async Task Should_bind_endpoint_constructor_options_from_convention_section()
    {
      using TestTerminal terminal = new();
      string[] testArgs = ["e591-show-db", "--E591Database:Host=myhost", "--E591Database:Port=3306"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<E591ShowDatabaseCommand>()
        .Build();

      int exitCode = await app.RunAsync(testArgs);

      exitCode.ShouldBe(0);
      terminal.OutputContains("Host: myhost").ShouldBeTrue();
      terminal.OutputContains("Port: 3306").ShouldBeTrue();
    }

    /// <summary>
    /// <c>[ConfigurationKey]</c> selects the section instead of the type name.
    /// </summary>
    public static async Task Should_bind_endpoint_constructor_options_from_attribute_section()
    {
      using TestTerminal terminal = new();
      string[] testArgs = ["e591-show-api", "--E591Api:Endpoint=https://test.com", "--E591Api:TimeoutSeconds=60"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<E591ShowApiCommand>()
        .Build();

      int exitCode = await app.RunAsync(testArgs);

      exitCode.ShouldBe(0);
      terminal.OutputContains("Endpoint: https://test.com").ShouldBeTrue();
      terminal.OutputContains("Timeout: 60").ShouldBeTrue();
    }

    /// <summary>
    /// A missing section keeps the property initializers.
    /// </summary>
    public static async Task Should_use_default_when_endpoint_constructor_section_missing()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<E591ShowDatabaseCommand>()
        .Build();

      int exitCode = await app.RunAsync(["e591-show-db"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("Host: localhost").ShouldBeTrue();
      terminal.OutputContains("Port: 5432").ShouldBeTrue();
    }

    /// <summary>
    /// <c>IValidateOptions&lt;T&gt;</c> runs when the constructor options are created.
    /// </summary>
    public static async Task Should_throw_when_endpoint_constructor_options_fail_validation()
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<E591ValidatedCommand>()
        .Build();

      try
      {
        await app.RunAsync(["e591-validated", "--E591Checked:Port=0"]);
        throw new ShouldAssertException("Expected OptionsValidationException but none was thrown");
      }
      catch (OptionsValidationException ex)
      {
        ex.Message.ShouldContain("Port must be between");
      }
    }

    /// <summary>
    /// <c>UseDirectInvoke()</c> binds the same section from the route's configuration local.
    /// </summary>
    public static async Task Should_bind_endpoint_constructor_options_on_direct_invoke()
    {
      using TestTerminal terminal = new();
      string[] testArgs = ["e591-direct-db", "--E591Database:Host=direct-host"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseDirectInvoke()
        .Map<E591DirectDatabaseCommand>()
        .Build();

      int exitCode = await app.RunAsync(testArgs);

      exitCode.ShouldBe(0);
      terminal.OutputContains("Host: direct-host").ShouldBeTrue();
    }
  }
}

/// <summary>
/// Convention section <c>E591Database</c>.
/// </summary>
public class E591DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
}

/// <summary>
/// Attribute section <c>E591Api</c>. The type name would otherwise be the section.
/// </summary>
[ConfigurationKey("E591Api")]
public class E591ApiSettings
{
  public string Endpoint { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Validated by <see cref="E591CheckedOptionsValidator"/>.
/// </summary>
[ConfigurationKey("E591Checked")]
public class E591CheckedOptions
{
  public int Port { get; set; } = 8080;
}

/// <summary>
/// Fails when <see cref="E591CheckedOptions.Port"/> is outside 1..65535.
/// </summary>
public class E591CheckedOptionsValidator : IValidateOptions<E591CheckedOptions>
{
  public ValidateOptionsResult Validate(string? name, E591CheckedOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    if (options.Port < 1 || options.Port > 65535)
      return ValidateOptionsResult.Fail("Port must be between 1 and 65535");

    return ValidateOptionsResult.Success;
  }
}

/// <summary>
/// Constructor takes <c>ITerminal</c> and <c>IOptions&lt;T&gt;</c> together.
/// </summary>
[NuruRoute("e591-show-db")]
public sealed class E591ShowDatabaseCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal, IOptions<E591DatabaseOptions> options)
    : IRequestHandler<E591ShowDatabaseCommand, Unit>
  {
    public async ValueTask<Unit> Handle(E591ShowDatabaseCommand command, CancellationToken cancellationToken)
    {
      _ = command;
      await terminal.WriteLineAsync($"Host: {options.Value.Host}, Port: {options.Value.Port}").ConfigureAwait(false);
      return Unit.Value;
    }
  }
}

/// <summary>
/// Constructor takes options bound by <see cref="ConfigurationKeyAttribute"/>.
/// </summary>
[NuruRoute("e591-show-api")]
public sealed class E591ShowApiCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal, IOptions<E591ApiSettings> options)
    : IRequestHandler<E591ShowApiCommand, Unit>
  {
    public async ValueTask<Unit> Handle(E591ShowApiCommand command, CancellationToken cancellationToken)
    {
      _ = command;
      await terminal.WriteLineAsync($"Endpoint: {options.Value.Endpoint}, Timeout: {options.Value.TimeoutSeconds}").ConfigureAwait(false);
      return Unit.Value;
    }
  }
}

/// <summary>
/// Constructor takes options that have an <c>IValidateOptions&lt;T&gt;</c>.
/// </summary>
[NuruRoute("e591-validated")]
public sealed class E591ValidatedCommand : ICommand<Unit>
{
  public sealed class Handler(IOptions<E591CheckedOptions> options) : IRequestHandler<E591ValidatedCommand, Unit>
  {
    public ValueTask<Unit> Handle(E591ValidatedCommand command, CancellationToken cancellationToken)
    {
      _ = command;
      _ = options.Value;
      return Unit.ValueTask;
    }
  }
}

/// <summary>
/// Same options type as <see cref="E591ShowDatabaseCommand"/>, invoked with <c>UseDirectInvoke()</c>.
/// </summary>
[NuruRoute("e591-direct-db")]
public sealed class E591DirectDatabaseCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal, IOptions<E591DatabaseOptions> options)
    : IRequestHandler<E591DirectDatabaseCommand, Unit>
  {
    public async ValueTask<Unit> Handle(E591DirectDatabaseCommand command, CancellationToken cancellationToken)
    {
      _ = command;
      await terminal.WriteLineAsync($"Host: {options.Value.Host}").ConfigureAwait(false);
      return Unit.Value;
    }
  }
}
