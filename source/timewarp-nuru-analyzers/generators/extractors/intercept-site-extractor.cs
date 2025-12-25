// Extracts intercept site information (file/line/column) for the interceptor attribute.
//
// The [InterceptsLocation] attribute requires exact file path, line, and column
// of the method call to intercept. This extractor obtains that information from
// Roslyn syntax locations.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts intercept site information for generating [InterceptsLocation] attributes.
/// </summary>
internal static class InterceptSiteExtractor
{
  /// <summary>
  /// Extracts the intercept site from a method invocation expression.
  /// </summary>
  /// <param name="invocation">The invocation expression to intercept.</param>
  /// <returns>The intercept site model with file path, line, and column.</returns>
  public static InterceptSiteModel? Extract(InvocationExpressionSyntax invocation)
  {
    // Get the location of the method name (not the whole invocation)
    // For app.RunAsync(), we want the location of "RunAsync"
    Location? location = GetMethodNameLocation(invocation);

    if (location is null)
      return null;

    return InterceptSiteModel.FromLocation(location);
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

    return Extract(invocation);
  }

  /// <summary>
  /// Extracts multiple intercept sites from an array of invocations.
  /// </summary>
  /// <param name="invocations">The invocation expressions to intercept.</param>
  /// <returns>Array of intercept site models.</returns>
  public static ImmutableArray<InterceptSiteModel> ExtractAll(IEnumerable<InvocationExpressionSyntax> invocations)
  {
    ImmutableArray<InterceptSiteModel>.Builder builder = ImmutableArray.CreateBuilder<InterceptSiteModel>();

    foreach (InvocationExpressionSyntax invocation in invocations)
    {
      InterceptSiteModel? site = Extract(invocation);
      if (site is not null)
        builder.Add(site);
    }

    return builder.ToImmutable();
  }

  /// <summary>
  /// Gets the location of the method name within an invocation expression.
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
  /// Creates an intercept site from explicit file/line/column values.
  /// Useful for testing or when the location is known from other sources.
  /// </summary>
  /// <param name="filePath">Absolute path to the source file.</param>
  /// <param name="line">1-based line number.</param>
  /// <param name="column">1-based column number.</param>
  /// <returns>The intercept site model.</returns>
  public static InterceptSiteModel Create(string filePath, int line, int column)
  {
    return new InterceptSiteModel(filePath, line, column);
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
  /// Formats an intercept site for use in generated code.
  /// Produces a string like: @"C:\path\to\file.cs", 10, 5
  /// </summary>
  /// <param name="site">The intercept site.</param>
  /// <returns>Formatted string for code generation.</returns>
  public static string FormatForGeneration(InterceptSiteModel site)
  {
    // Use verbatim string literal for the file path to handle backslashes
    return $"@\"{site.FilePath}\", {site.Line}, {site.Column}";
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
