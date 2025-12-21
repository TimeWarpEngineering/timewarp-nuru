namespace TimeWarp.Nuru;

/// <summary>
/// High-performance logger message definitions for the CompletionEngine.
/// Uses LoggerMessage.Define to avoid allocations when logging is disabled.
/// </summary>
/// <remarks>
/// Event ID ranges for Completion Engine: 2000-2099
/// </remarks>
internal static partial class CompletionLoggerMessages
{
  // ===== Tokenization Messages (2000-2019) =====
  internal static readonly Action<ILogger, string, string, bool, Exception?> TokenizedInput =
    LoggerMessage.Define<string, string, bool>(
      LogLevel.Debug,
      new EventId(2000, "TokenizedInput"),
      "CompletionEngine: Tokenized input - CompletedWords=[{Words}], PartialWord='{Partial}', HasTrailingSpace={HasTrailingSpace}");

  internal static readonly Action<ILogger, string, string, string, Exception?> ParsedRawInput =
    LoggerMessage.Define<string, string, string>(
      LogLevel.Debug,
      new EventId(2001, "ParsedRawInput"),
      "CompletionEngine: Parsed raw input '{Input}' - CompletedWords=[{Words}], PartialWord='{Partial}'");

  // ===== Route Matching Messages (2020-2039) =====
  internal static readonly Action<ILogger, int, int, Exception?> MatchedRoutes =
    LoggerMessage.Define<int, int>(
      LogLevel.Debug,
      new EventId(2020, "MatchedRoutes"),
      "CompletionEngine: Matched {ViableCount}/{TotalCount} viable routes");

  // ===== Candidate Generation Messages (2040-2059) =====
  internal static readonly Action<ILogger, int, Exception?> GeneratedCandidates =
    LoggerMessage.Define<int>(
      LogLevel.Debug,
      new EventId(2040, "GeneratedCandidates"),
      "CompletionEngine: Generated {Count} completion candidates");
}
