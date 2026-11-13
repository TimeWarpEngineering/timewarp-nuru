#region Purpose
// Behavior, service, telemetry and feature-toggle DSL methods of IrAppBuilder (partial).
#endregion

namespace TimeWarp.Nuru.Generators;

public partial class IrAppBuilder<TSelf> : IIrAppBuilder where TSelf : IrAppBuilder<TSelf>
{
  /// <summary>
  /// Adds a behavior (pipeline middleware).
  /// Mirrors: NuruAppBuilder.AddBehavior(Type)
  /// </summary>
  /// <param name="behavior">The behavior definition.</param>
  public TSelf AddBehavior(BehaviorDefinition behavior)
  {
    Behaviors.Add(behavior);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds a service registration.
  /// Mirrors: NuruAppBuilder.ConfigureServices()
  /// </summary>
  /// <param name="service">The service definition.</param>
  public TSelf AddService(ServiceDefinition service)
  {
    Services.Add(service);
    return (TSelf)this;
  }

  /// <summary>
  /// Adds an extension method call detected during service extraction.
  /// Used for NURU052 warnings about unanalyzable service registrations.
  /// </summary>
  /// <param name="extensionMethod">The extension method call.</param>
  public TSelf AddExtensionMethodCall(ExtensionMethodCall extensionMethod)
  {
    ExtensionMethods.Add(extensionMethod);
    return (TSelf)this;
  }

  /// <summary>
  /// No-op for UseTerminal (runtime only).
  /// Mirrors: NuruAppBuilder.UseTerminal()
  /// </summary>
  public TSelf UseTerminal()
  {
    // No-op - terminal is runtime only
    return (TSelf)this;
  }

  /// <summary>
  /// Enables telemetry (OpenTelemetry instrumentation) with default options.
  /// Mirrors: NuruAppBuilder.UseTelemetry()
  /// </summary>
  public TSelf UseTelemetry()
  {
    TelemetryEnabled = true;
    TelemetryOptions = TelemetryModel.Default;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables telemetry with custom options.
  /// Mirrors: NuruAppBuilder.UseTelemetry(Action&lt;NuruTelemetryOptions&gt;)
  /// </summary>
  /// <param name="telemetryOptions">The configured telemetry options.</param>
  public TSelf UseTelemetry(TelemetryModel telemetryOptions)
  {
    TelemetryEnabled = true;
    TelemetryOptions = telemetryOptions;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables shell completion support.
  /// Mirrors: NuruAppBuilderCompletionExtensions.EnableCompletion()
  /// </summary>
  public TSelf EnableCompletion()
  {
    CompletionEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Enables runtime Microsoft.Extensions.DependencyInjection instead of source-gen DI.
  /// Mirrors: NuruAppBuilder.UseMicrosoftDependencyInjection()
  /// </summary>
  public TSelf UseMicrosoftDependencyInjection()
  {
    MicrosoftDependencyInjectionEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Opts this app out of mediator dispatch. Nested handlers are constructed and invoked directly.
  /// </summary>
  public TSelf UseDirectInvoke()
  {
    DirectInvokeEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Writes renderer text with Write, so no newline is added after it.
  /// </summary>
  public TSelf UseVerbatimRendererOutput()
  {
    VerbatimRendererOutputEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Sends CLI binding errors through the app's problem renderer.
  /// </summary>
  public TSelf UseProblemRendererForErrors()
  {
    ProblemRendererForErrorsEnabled = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the raw ConfigureServices lambda body for runtime invocation.
  /// Used when UseMicrosoftDependencyInjection is enabled.
  /// </summary>
  /// <param name="lambdaBody">The lambda body source text.</param>
  public TSelf SetConfigureServicesBody(string lambdaBody)
  {
    ConfigureServicesBody = lambdaBody;
    return (TSelf)this;
  }

  /// <summary>
  /// Registers a custom type converter for code generation.
  /// Mirrors: NuruAppBuilder.AddTypeConverter()
  /// </summary>
  /// <param name="converter">The converter definition.</param>
  public TSelf AddTypeConverter(CustomConverterDefinition converter)
  {
    CustomConverters.Add(converter);
    return (TSelf)this;
  }
}
