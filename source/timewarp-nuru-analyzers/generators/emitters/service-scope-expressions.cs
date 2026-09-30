// Expressions that read or construct a source-generated service for its lifetime.
// Process init reads __svc_ fields. Command resolution reads the REPL ternary for Scoped.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// How a constructor dependency is read while emitting source-generated DI.
/// </summary>
internal enum ServiceResolveMode
{
  /// <summary>
  /// Singleton and non-REPL scoped initialization. Scoped dependencies read the process field.
  /// Transients are constructed and not tracked for disposal.
  /// </summary>
  ProcessInit,

  /// <summary>
  /// Handler and command-scope construction. Scoped dependencies follow <c>__replCommandActive</c>.
  /// Transients are tracked until the command ends.
  /// </summary>
  Command,

  /// <summary>
  /// Session-scope construction. Transients are tracked until the session ends.
  /// </summary>
  Session
}

/// <summary>
/// Field names and construction expressions for source-generated service lifetimes.
/// </summary>
internal static class ServiceScopeExpressions
{
  internal static string SessionField(string implementationTypeName) =>
    "__ses_" + Sanitize(implementationTypeName);

  internal static string CommandField(string implementationTypeName) =>
    "__cmd_" + Sanitize(implementationTypeName);

  internal static string Read(ServiceDefinition service, ServiceResolveMode mode)
  {
    string implementationTypeName = service.ImplementationTypeName;
    return service.Lifetime switch
    {
      ServiceLifetime.Singleton => InterceptorEmitter.GetServiceFieldName(implementationTypeName),
      ServiceLifetime.SessionScoped => SessionField(implementationTypeName),
      ServiceLifetime.CommandScoped => CommandField(implementationTypeName),
      ServiceLifetime.Scoped => mode == ServiceResolveMode.Command
        ? $"(__replCommandActive ? {CommandField(implementationTypeName)} : {InterceptorEmitter.GetServiceFieldName(implementationTypeName)})"
        : InterceptorEmitter.GetServiceFieldName(implementationTypeName),
      _ => throw new InvalidOperationException($"Service lifetime {service.Lifetime} is constructed, not read.")
    };
  }

  internal static string Construct
  (
    ServiceDefinition service,
    ImmutableArray<ServiceDefinition> services,
    ServiceResolveMode mode
  )
  {
    string args = ServiceResolverEmitter.ResolveConstructorArguments(service, services, mode);
    string created = args.Length == 0
      ? $"new {service.ImplementationTypeName}()"
      : $"new {service.ImplementationTypeName}({args})";

    string track = TrackMethod(mode);
    return track.Length == 0 ? created : $"{track}({created})";
  }

  internal static string TrackMethod(ServiceResolveMode mode) => mode switch
  {
    ServiceResolveMode.Command => "__TrackCommand",
    ServiceResolveMode.Session => "__TrackSession",
    _ => ""
  };

  private static string Sanitize(string implementationTypeName)
  {
    string name = implementationTypeName;
    if (name.StartsWith("global::", StringComparison.Ordinal))
      name = name[8..];

    return name.Replace(".", "_", StringComparison.Ordinal);
  }
}
