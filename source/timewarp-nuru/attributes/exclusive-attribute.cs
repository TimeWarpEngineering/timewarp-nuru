#region Purpose
// Attribute that marks an endpoint exclusive so served-app calls to it run one at a time.
#endregion

#region Design
// A marker only: the generator reads it from the endpoint class (EndpointExtractor sets
// IsExclusive) and the serve/MCP runtime serializes those calls. It is not inherited, so a
// derived request must opt in itself, matching fluent .Exclusive(). The attribute exists
// because most handlers are safe to run in parallel and exclusivity is the exception.
#endregion

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
