// Expressions that read or construct a source-generated service for its lifetime.
// A read calls the field's __Resolve accessor, which builds the instance on first use (task 508-007).
// Command resolution reads the REPL ternary for Scoped.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// How a constructor dependency is read while emitting source-generated DI.
/// </summary>
internal enum ServiceResolveMode
{
  /// <summary>
  /// Singleton and non-REPL scoped construction. Scoped dependencies read the process field.
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

  /// <summary>
  /// The accessor that returns a cached service field, constructing it on first read.
  /// </summary>
  internal static string ResolverName(string fieldName) => "__Resolve" + fieldName;

  internal static string Read(ServiceDefinition service, ServiceResolveMode mode)
  {
    string implementationTypeName = service.ImplementationTypeName;
    string processRead = ResolveCall(InterceptorEmitter.GetServiceFieldName(implementationTypeName));
    return service.Lifetime switch
    {
      ServiceLifetime.Singleton => processRead,
      ServiceLifetime.SessionScoped => ResolveCall(SessionField(implementationTypeName)),
      ServiceLifetime.CommandScoped => ResolveCall(CommandField(implementationTypeName)),
      ServiceLifetime.Scoped => mode == ServiceResolveMode.Command
        ? $"(__replCommandActive ? {ResolveCall(CommandField(implementationTypeName))} : {processRead})"
        : processRead,
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

  private static string ResolveCall(string fieldName) => ResolverName(fieldName) + "()";

  private static string Sanitize(string implementationTypeName)
  {
    string name = implementationTypeName;
    if (name.StartsWith("global::", StringComparison.Ordinal))
      name = name[8..];

    return name.Replace(".", "_", StringComparison.Ordinal);
  }
}
