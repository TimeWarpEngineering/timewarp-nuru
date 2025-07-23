namespace TimeWarp.Nuru.Parsing.Segments;

/// <summary>
/// Base class for all route segments (literals, parameters, etc.)
/// </summary>
public abstract class RouteSegment
{
    /// <summary>
    /// Attempts to match this segment against an argument.
    /// </summary>
    /// <param name="arg">The argument to match against.</param>
    /// <param name="extractedValue">If this is a parameter segment, the extracted value; otherwise null.</param>
    /// <returns>True if the segment matches the argument; otherwise false.</returns>
    public abstract bool TryMatch(string arg, out string? extractedValue);

    /// <summary>
    /// Gets the display representation of this segment for debugging.
    /// </summary>
    public abstract string ToDisplayString();
}