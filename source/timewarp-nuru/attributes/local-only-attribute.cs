#region Purpose
// Attribute that keeps an endpoint in the CLI process so a served app never forwards it to a server.
#endregion

#region Design
// Read from the endpoint class by EndpointExtractor into RouteDefinition.IsLocalOnly. The
// capabilities emitter publishes it as `localOnly` and the JSON-args emitter keeps the route out
// of server forwarding. Not inherited, so each endpoint states its own locality. Locality is
// declared per route because only the author knows whether a handler needs the caller's
// terminal, stdin, or environment.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Keeps an endpoint in the CLI process: a served app (<c>AddServeRoute()</c>, package
/// TimeWarp.Nuru.Daemon) never forwards it to a running server. The <c>--capabilities</c> catalog
/// carries it as <c>"localOnly": true</c>.
/// </summary>
/// <remarks>
/// Use it for routes that read stdin, prompt, depend on the caller's environment, or manage the
/// server itself. Fluent routes use <c>.LocalOnly()</c>. Built-in routes (help, version,
/// capabilities, completion, <c>serve</c>, <c>mcp</c>) are always local.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LocalOnlyAttribute : Attribute;
