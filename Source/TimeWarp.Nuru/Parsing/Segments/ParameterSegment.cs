namespace TimeWarp.Nuru.Parsing.Segments;

/// <summary>
/// Represents a parameter segment in a route pattern that extracts a value.
/// </summary>
public class ParameterSegment : RouteSegment
{
  /// <summary>
  /// Gets the name of the parameter.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Gets whether this is a catch-all parameter (*).
  /// </summary>
  public bool IsCatchAll { get; }

  /// <summary>
  /// Gets the constraint for this parameter (e.g., "int" in {id:int}).
  /// </summary>
  public string? Constraint { get; }

  public ParameterSegment(string name, bool isCatchAll = false, string? constraint = null)
  {
    Name = name ?? throw new ArgumentNullException(nameof(name));
    IsCatchAll = isCatchAll;
    Constraint = constraint;
  }

  public override bool TryMatch(string arg, out string? extractedValue)
  {
    extractedValue = arg;
    return true; // Constraint validation happens during parameter binding
  }

  public override string ToDisplayString()
  {
    string result = IsCatchAll ? "{*" : "{";
    result += Name;
    if (Constraint is not null)
    {
      result += ":" + Constraint;
    }

    result += "}";
    return result;
  }
}
