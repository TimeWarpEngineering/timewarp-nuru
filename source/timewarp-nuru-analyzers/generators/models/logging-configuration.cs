namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents logging configuration extracted from AddLogging(...) calls.
/// </summary>
/// <param name="ConfigurationLambdaBody">
/// The lambda body text to emit verbatim in LoggerFactory.Create().
/// Example: For AddLogging(builder => builder.AddConsole()), this would be "builder.AddConsole()".
/// </param>
/// <param name="LambdaParameterName">
/// The parameter name from the user's AddLogging lambda.
/// Example: For AddLogging(b => b.AddConsole()), this would be "b".
/// Defaults to "builder" when not extractable.
/// </param>
public sealed record LoggingConfiguration(
  string ConfigurationLambdaBody,
  string LambdaParameterName = "builder"
);
