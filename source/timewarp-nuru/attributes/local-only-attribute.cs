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
