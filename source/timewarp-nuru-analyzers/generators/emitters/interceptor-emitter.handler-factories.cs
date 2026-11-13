#region Purpose
// InterceptorEmitter partial: explicit constructor factories for nested [NuruRoute] handlers
// in the containers that hold the TimeWarp.Mediator generated mediator.
#endregion
#region Design
// The TimeWarp.Mediator generator registers each handler as AddScoped<Handler>(services), so the
// container builds it by reflection and the compiler never sees `new Handler(...)`. An internal
// handler then trips CA1812 in any consumer that compiles it with the rule on: TimeWarp.Nuru.DevCli
// ships its endpoints as source, and every repo taking the generated dev CLI hit it (kanban 573).
//
// After AddGeneratedMediator(services) the generator swaps each such descriptor for one with the
// same service type and lifetime whose factory calls the handler's constructor, resolving every
// argument with GetRequiredService. That is how the container activates a single-constructor type
// with required parameters, so behavior does not change, and CA1812 counts the instantiation.
// __UseHandlerFactory replaces only a descriptor the mediator added (non-keyed, implementation type
// is the handler); with none it adds nothing. Registrations made after it (a runtime-DI app's own
// ConfigureServices) still win. Handlers the container could build differently (several public
// constructors, optional or params arguments) keep the reflection registration; see
// EndpointExtractor.IsFactoryConstructible.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class InterceptorEmitter
{
  private const string HandlerFactoryHelper = "__UseHandlerFactory";

  /// <summary>
  /// Nested endpoint handlers the generator can construct with an explicit factory, one per type.
  /// </summary>
  private static ImmutableArray<HandlerDefinition> CollectHandlerFactories(GeneratorModel model) =>
  [
    .. model.AllRoutes
      .Select(static route => route.Handler)
      .Where(static handler => handler.HandlerKind == HandlerKind.Command
        && handler.NestedHandlerTypeName is not null
        && handler.NestedHandlerFactoryConstructible)
      .DistinctBy(static handler => handler.NestedHandlerTypeName, StringComparer.Ordinal)
      .OrderBy(static handler => handler.NestedHandlerTypeName, StringComparer.Ordinal)
  ];

  /// <summary>
  /// Emits <c>__UseHandlerFactory&lt;THandler&gt;</c>, which swaps a reflection-activated handler
  /// registration for a factory registration with the same lifetime.
  /// </summary>
  private static void EmitHandlerFactoryHelper(StringBuilder sb)
  {
    const string Di = "global::Microsoft.Extensions.DependencyInjection";

    sb.AppendLine("  // Swaps the generated mediator's reflection-activated handler registration for an explicit");
    sb.AppendLine("  // constructor call with the same lifetime, so code analysis (CA1812) sees the handler built.");
    sb.AppendLine($"  private static void {HandlerFactoryHelper}<THandler>({Di}.IServiceCollection services, global::System.Func<global::System.IServiceProvider, THandler> factory)");
    sb.AppendLine("    where THandler : class");
    sb.AppendLine("  {");
    sb.AppendLine("    for (int i = services.Count - 1; i >= 0; i--)");
    sb.AppendLine("    {");
    sb.AppendLine($"      {Di}.ServiceDescriptor descriptor = services[i];");
    sb.AppendLine("      if (descriptor.ServiceType != typeof(THandler) || descriptor.IsKeyedService || descriptor.ImplementationType != typeof(THandler))");
    sb.AppendLine("        continue;");
    sb.AppendLine();
    sb.AppendLine($"      services[i] = new {Di}.ServiceDescriptor(typeof(THandler), factory, descriptor.Lifetime);");
    sb.AppendLine("      return;");
    sb.AppendLine("    }");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits one <c>__UseHandlerFactory</c> call per handler. Call it right after the line that
  /// registers the generated mediator into <c>services</c>.
  /// </summary>
  private static void EmitHandlerFactoryRegistrations(StringBuilder sb, ImmutableArray<HandlerDefinition> handlers, string indent)
  {
    const string Resolve = "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService";

    foreach (HandlerDefinition handler in handlers)
    {
      ImmutableArray<ParameterBinding> dependencies = handler.ConstructorDependencies;
      string lambdaParameter = dependencies.Length == 0 ? "_" : "sp";
      string arguments = string.Join(", ", dependencies.Select(static dependency => $"{Resolve}<{dependency.ParameterTypeName}>(sp)"));

      sb.AppendLine($"{indent}{HandlerFactoryHelper}<{handler.NestedHandlerTypeName}>(services, static {lambdaParameter} => new {handler.NestedHandlerTypeName}({arguments}));");
    }
  }
}
