namespace TimeWarp.Nuru;

/// <summary>
/// Runs an endpoint one call at a time: while one call of an exclusive route runs, every other
/// exclusive call on the same app waits for it. Calls of routes without it run concurrently.
/// </summary>
/// <remarks>
/// Concurrency only arises in a served app (TimeWarp.Nuru.Daemon) or an MCP host
/// (TimeWarp.Nuru.McpHost), which run calls in parallel by default. Mark a route exclusive when
/// its handler is not thread-safe. A host can make every call exclusive with its <c>ExclusiveCalls</c>
/// option. Fluent routes use <c>.Exclusive()</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ExclusiveAttribute : Attribute;
