namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents logging configuration extracted from AddLogging(...) calls.
/// </summary>
/// <param name="ConfigurationLambdaBody">
/// The lambda body text to emit verbatim in LoggerFactory.Create().
/// Example: For AddLogging(builder => builder.AddConsole()), this would be "builder.AddConsole()".
/// </param>
public sealed record LoggingConfiguration(
  string ConfigurationLambdaBody
);
