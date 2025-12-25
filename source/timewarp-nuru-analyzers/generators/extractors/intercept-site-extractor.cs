// Extracts intercept site information for the interceptor attribute.
//
// In .NET 10 / C# 14, interceptors use the new InterceptableLocation API
// which provides a versioned, opaque data encoding. This is more portable
// across machines and more evolvable than the old file/line/column approach.
//
// See: https://github.com/dotnet/roslyn/issues/72133

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts intercept site information for generating [InterceptsLocation] attributes.
/// Uses the new .NET 10 / C# 14 InterceptableLocation API.
/// </summary>
internal static class InterceptSiteExtractor
{
  /// <summary>
  /// Extracts the intercept site from a method invocation expression using SemanticModel.
  /// </summary>
  /// <param name="semanticModel">The semantic model for the syntax tree.</param>
  /// <param name="invocation">The invocation expression to intercept.</param>
  /// <returns>The intercept site model, or null if the call is not interceptable.</returns>
  public static InterceptSiteModel? Extract(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
  {
    // Use the new Roslyn API to get an InterceptableLocation
    // This returns null if the call cannot be intercepted
    InterceptableLocation? interceptableLocation = semanticModel.GetInterceptableLocation(invocation);

    if (interceptableLocation is null)
      return null;

    // Get location for diagnostics (file/line/column)
    Location? location = GetMethodNameLocation(invocation);
    if (location is null)
      return null;

    return InterceptSiteModel.FromInterceptableLocation(interceptableLocation, location);
  }

  /// <summary>
  /// Extracts the intercept site from a GeneratorSyntaxContext.
  /// </summary>
  /// <param name="context">The generator syntax context.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The intercept site model, or null if extraction fails.</returns>
  public static InterceptSiteModel? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    return Extract(context.SemanticModel, invocation);
  }

  /// <summary>
  /// Gets the location of the method name within an invocation expression.
  /// Used for diagnostic purposes (human-readable file/line/column).
  /// </summary>
  private static Location? GetMethodNameLocation(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      // app.RunAsync() - get location of "RunAsync"
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),

      // RunAsync() - get location of "RunAsync"
      IdentifierNameSyntax identifier => identifier.GetLocation(),

      // Fallback to the whole expression
      _ => invocation.Expression.GetLocation()
    };
  }

  /// <summary>
  /// Validates that an intercept site has valid values.
  /// </summary>
  /// <param name="site">The intercept site to validate.</param>
  /// <returns>True if the site is valid, false otherwise.</returns>
  public static bool IsValid(InterceptSiteModel site)
  {
    if (string.IsNullOrEmpty(site.FilePath))
      return false;

    if (site.Line < 1)
      return false;

    if (site.Column < 1)
      return false;

    return true;
  }

  /// <summary>
  /// Formats an intercept site as a diagnostic location string.
  /// Produces a string like: file.cs(10,5)
  /// </summary>
  /// <param name="site">The intercept site.</param>
  /// <returns>Formatted string for diagnostics.</returns>
  public static string FormatForDiagnostic(InterceptSiteModel site)
  {
    string fileName = Path.GetFileName(site.FilePath);
    return $"{fileName}({site.Line},{site.Column})";
  }
}
