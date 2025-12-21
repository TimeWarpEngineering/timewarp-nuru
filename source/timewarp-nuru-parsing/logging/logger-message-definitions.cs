#if !ANALYZER_BUILD
namespace TimeWarp.Nuru;

using Microsoft.Extensions.Logging;

/// <summary>
/// High-performance logger message definitions using LoggerMessage.Define
/// to avoid allocations when logging is disabled.
/// </summary>
internal static class ParsingLoggerMessages
{
  // ===== Registration Messages (1000-1099) =====
  internal static readonly Action<ILogger, Exception?> StartingRouteRegistration =
    LoggerMessage.Define(
      LogLevel.Information,
      new EventId(1000, "StartingRouteRegistration"),
      "Starting route registration");

  internal static readonly Action<ILogger, string, Exception?> RegisteringRoute =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1001, "RegisteringRoute"),
      "Registering route: '{RoutePattern}'");

  // ===== Lexer Messages (1050-1099) =====
  internal static readonly Action<ILogger, string, Exception?> StartingLexicalAnalysis =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1050, "StartingLexicalAnalysis"),
      "Starting lexical analysis of: '{Input}'");

  internal static readonly Action<ILogger, int, Exception?> CompletedLexicalAnalysis =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(1051, "CompletedLexicalAnalysis"),
      "Lexical analysis complete. Generated {TokenCount} tokens");

  // ===== Parser Messages (1100-1199) =====
  internal static readonly Action<ILogger, string, Exception?> ParsingPattern =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1100, "ParsingPattern"),
      "Parsing pattern: '{Pattern}'");

  internal static readonly Action<ILogger, string, Exception?> DumpingTokens =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1101, "DumpingTokens"),
      "Tokens: {Tokens}");

  internal static readonly Action<ILogger, string, Exception?> DumpingAst =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1102, "DumpingAst"),
      "AST: {Ast}");

  internal static readonly Action<ILogger, string, Exception?> SettingBooleanOptionParameter =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1103, "SettingBooleanOptionParameter"),
      "Setting boolean option parameter name to: '{ParameterName}'");

  // ===== Matcher Messages (1200-1399) =====
  internal static readonly Action<ILogger, string, Exception?> ResolvingCommand =
    LoggerMessage.Define<string>(
      LogLevel.Information,
      new EventId(1200, "ResolvingCommand"),
      "Resolving command: '{Command}'");

  internal static readonly Action<ILogger, int, Exception?> CheckingAvailableRoutes =
    LoggerMessage.Define<int>(
      LogLevel.Debug,
      new EventId(1201, "CheckingAvailableRoutes"),
      "Checking {RouteCount} available routes");

  internal static readonly Action<ILogger, int, int, string, Exception?> CheckingRoute =
    LoggerMessage.Define<int, int, string>(
      LogLevel.Trace,
      new EventId(1202, "CheckingRoute"),
      "[{Index}/{Total}] Checking route: '{RoutePattern}'");

  internal static readonly Action<ILogger, string, Exception?> MatchedCatchAllRoute =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1203, "MatchedCatchAllRoute"),
      "✓ Matched catch-all route: '{RoutePattern}'");

  internal static readonly Action<ILogger, string, Exception?> MatchedRoute =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(1204, "MatchedRoute"),
      "✓ Matched route: '{RoutePattern}'");

  internal static readonly Action<ILogger, string, int, int, Exception?> RouteConsumedPartialArgs =
    LoggerMessage.Define<string, int, int>(
      LogLevel.Trace,
      new EventId(1205, "RouteConsumedPartialArgs"),
      "Route '{RoutePattern}' consumed only {ConsumedCount}/{TotalCount} args");

  internal static readonly Action<ILogger, string, Exception?> RouteFailedAtOptionMatching =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1206, "RouteFailedAtOptionMatching"),
      "Route '{RoutePattern}' failed at option matching");

  internal static readonly Action<ILogger, string, Exception?> RouteFailedAtPositionalMatching =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1207, "RouteFailedAtPositionalMatching"),
      "Route '{RoutePattern}' failed at positional matching");

  internal static readonly Action<ILogger, string, Exception?> NoMatchingRouteFound =
    LoggerMessage.Define<string>(
      LogLevel.Information,
      new EventId(1208, "NoMatchingRouteFound"),
      "No matching route found for: '{Command}'");

  internal static readonly Action<ILogger, Exception?> ExtractedValues =
    LoggerMessage.Define(
      LogLevel.Debug,
      new EventId(1209, "ExtractedValues"),
      "Extracted values:");

  internal static readonly Action<ILogger, string, string, Exception?> ExtractedValue =
    LoggerMessage.Define<string, string>(
      LogLevel.Debug,
      new EventId(1210, "ExtractedValue"),
      "  {Key} = '{Value}'");

  // ===== Positional Matching Messages (1300-1349) =====
  internal static readonly Action<ILogger, int, int, Exception?> MatchingPositionalSegments =
    LoggerMessage.Define<int, int>(
      LogLevel.Trace,
      new EventId(1300, "MatchingPositionalSegments"),
      "Matching {SegmentCount} positional segments against {ArgumentCount} arguments");

  internal static readonly Action<ILogger, string, string, Exception?> CatchAllParameterCaptured =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1301, "CatchAllParameterCaptured"),
      "Catch-all parameter '{ParameterName}' captured: '{Value}'");

  internal static readonly Action<ILogger, string, Exception?> CatchAllParameterNoArgs =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1302, "CatchAllParameterNoArgs"),
      "Catch-all parameter '{ParameterName}' has no args to consume");

  internal static readonly Action<ILogger, string, Exception?> OptionalParameterNoValue =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1303, "OptionalParameterNoValue"),
      "Optional parameter '{ParameterName}' - no value provided");

  internal static readonly Action<ILogger, string, Exception?> NotEnoughArgumentsForSegment =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1304, "NotEnoughArgumentsForSegment"),
      "Not enough arguments for segment '{Segment}'");

  internal static readonly Action<ILogger, string, string, Exception?> OptionalParameterSkippedHitOption =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1305, "OptionalParameterSkippedHitOption"),
      "Optional parameter '{ParameterName}' skipped - hit option '{Option}'");

  internal static readonly Action<ILogger, string, string, Exception?> RequiredSegmentExpectedButFoundOption =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1306, "RequiredSegmentExpectedButFoundOption"),
      "Required segment '{Segment}' expected but found option '{Option}'");

  internal static readonly Action<ILogger, string, string, Exception?> AttemptingToMatch =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1307, "AttemptingToMatch"),
      "Attempting to match '{Argument}' against {Segment}");

  internal static readonly Action<ILogger, string, string, Exception?> FailedToMatch =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1308, "FailedToMatch"),
      "  Failed to match '{Argument}' against {Segment}");

  internal static readonly Action<ILogger, string, string, Exception?> ExtractedParameter =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(1309, "ExtractedParameter"),
      "  Extracted parameter '{ParameterName}' = '{Value}'");

  internal static readonly Action<ILogger, string, Exception?> LiteralMatched =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1310, "LiteralMatched"),
      "  Literal '{Literal}' matched");

  internal static readonly Action<ILogger, int, Exception?> PositionalMatchingComplete =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(1311, "PositionalMatchingComplete"),
      "Positional matching complete. Consumed {ConsumedCount} arguments.");

  // ===== Option Matching Messages (1350-1399) =====
  internal static readonly Action<ILogger, string, Exception?> BooleanOptionSet =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1350, "BooleanOptionSet"),
      "Boolean option '{OptionName}' = true");

  internal static readonly Action<ILogger, string, Exception?> RequiredOptionNotFound =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1351, "RequiredOptionNotFound"),
      "Required option not found: {Option}");

  internal static readonly Action<ILogger, string, Exception?> OptionalBooleanOptionNotProvided =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1352, "OptionalBooleanOptionNotProvided"),
      "Optional boolean option '{OptionName}' not provided, defaulting to false");

  internal static readonly Action<ILogger, string, Exception?> OptionalValueOptionNotProvided =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1353, "OptionalValueOptionNotProvided"),
      "Optional value option '{OptionName}' not provided, will be null");

  internal static readonly Action<ILogger, int, Exception?> OptionsMatchingComplete =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(1354, "OptionsMatchingComplete"),
      "Options matching complete. Consumed {ConsumedCount} args.");

  internal static readonly Action<ILogger, string, Exception?> RequiredOptionValueNotProvided =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(1355, "RequiredOptionValueNotProvided"),
      "Required option '{Option}' expects a value but none was provided");

  // ===== Binder Messages (1400-1499) =====
  // Currently no binder messages, but reserved for future use

  // ===== Type Converter Messages (1500-1599) =====
  // Reserved for future type converter messages

  // ===== Help Generation Messages (1600-1699) =====
  // Reserved for future help generation messages

  // ===== Configuration Messages (1700-1799) =====
  internal static readonly Action<ILogger, string, string, Exception?> ConfigurationBasePath =
    LoggerMessage.Define<string, string>(
      LogLevel.Debug,
      new EventId(1700, "ConfigurationBasePath"),
      "Configuration base path: {BasePath} (source: {Source})");
}
#endif
