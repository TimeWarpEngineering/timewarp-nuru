namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a single usage example declared via
/// <c>[NuruRouteExample]</c> (Endpoint DSL) or <c>.WithExample()</c> (Fluent DSL).
/// </summary>
/// <param name="Command">
/// The example invocation, verbatim as typed after the executable name (e.g. "deploy prod --dry-run").
/// </param>
/// <param name="Description">Optional description of what this example demonstrates.</param>
public sealed record ExampleDefinition(
  string Command,
  string? Description);
