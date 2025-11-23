namespace TimeWarp.Nuru;

using Microsoft.Extensions.Logging;

/// <summary>
/// High-performance logger message definitions for REPL operations using LoggerMessage.Define
/// to avoid allocations when logging is disabled.
/// </summary>
internal static class ReplLoggerMessages
{
  // ===== REPL Input Messages (2000-2099) =====
  internal static readonly Action<ILogger, string, int, Exception?> ReadLineStarted =
    LoggerMessage.Define<string, int>(
      LogLevel.Trace,
      new EventId(2000, "ReadLineStarted"),
      "ReadLine started with prompt '{Prompt}', history count: {HistoryCount}");

  internal static readonly Action<ILogger, string, int, Exception?> KeyPressed =
    LoggerMessage.Define<string, int>(
      LogLevel.Trace,
      new EventId(2001, "KeyPressed"),
      "Key pressed: {Key}, CursorPosition: {CursorPosition}");

  internal static readonly Action<ILogger, string, int, Exception?> UserInputChanged =
    LoggerMessage.Define<string, int>(
      LogLevel.Debug,
      new EventId(2002, "UserInputChanged"),
      "UserInput changed: '{UserInput}', CursorPosition: {CursorPosition}");

  internal static readonly Action<ILogger, char, int, Exception?> CharacterInserted =
    LoggerMessage.Define<char, int>(
      LogLevel.Trace,
      new EventId(2003, "CharacterInserted"),
      "Character inserted: '{Char}', at position: {Position}");

  internal static readonly Action<ILogger, int, Exception?> BackspacePressed =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(2004, "BackspacePressed"),
      "Backspace pressed at position: {Position}");

  internal static readonly Action<ILogger, int, Exception?> DeletePressed =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(2005, "DeletePressed"),
      "Delete pressed at position: {Position}");

  // ===== REPL Completion Messages (2100-2199) =====
  internal static readonly Action<ILogger, string, int, string[], Exception?> TabCompletionTriggered =
    LoggerMessage.Define<string, int, string[]>(
      LogLevel.Debug,
      new EventId(2100, "TabCompletionTriggered"),
      "Tab completion triggered. UserInput='{UserInput}', CursorPosition={CursorPosition}, Args=[{Args}]");

  internal static readonly Action<ILogger, int, Exception?> CompletionContextCreated =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(2101, "CompletionContextCreated"),
      "Completion context created with {ArgCount} arguments");

  internal static readonly Action<ILogger, int, Exception?> CompletionCandidatesGenerated =
    LoggerMessage.Define<int>(
      LogLevel.Debug,
      new EventId(2102, "CompletionCandidatesGenerated"),
      "Generated {CandidateCount} completion candidates");

  internal static readonly Action<ILogger, string, Exception?> CompletionCandidateDetails =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(2103, "CompletionCandidateDetails"),
      "Completion candidate: {Candidate}");

  internal static readonly Action<ILogger, string, int, Exception?> CompletionApplied =
    LoggerMessage.Define<string, int>(
      LogLevel.Debug,
      new EventId(2104, "CompletionApplied"),
      "Applied completion: '{Candidate}' at position {WordStart}");

  internal static readonly Action<ILogger, int, int, Exception?> CompletionCycling =
    LoggerMessage.Define<int, int>(
      LogLevel.Debug,
      new EventId(2105, "CompletionCycling"),
      "Cycling completion: Index={CompletionIndex}, Total={TotalCandidates}");

  // ===== REPL History Messages (2200-2299) =====
  internal static readonly Action<ILogger, int, int, Exception?> HistoryNavigation =
    LoggerMessage.Define<int, int>(
      LogLevel.Trace,
      new EventId(2200, "HistoryNavigation"),
      "History navigation: Index={HistoryIndex}, Total={TotalHistory}");

  internal static readonly Action<ILogger, string, Exception?> HistoryEntryLoaded =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(2201, "HistoryEntryLoaded"),
      "Loaded history entry: '{Entry}'");

  // ===== REPL Syntax Highlighting Messages (2300-2399) =====
  internal static readonly Action<ILogger, string, Exception?> SyntaxHighlightingStarted =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(2300, "SyntaxHighlightingStarted"),
      "Syntax highlighting started for input: '{Input}'");

  internal static readonly Action<ILogger, int, Exception?> TokensGenerated =
    LoggerMessage.Define<int>(
      LogLevel.Trace,
      new EventId(2301, "TokensGenerated"),
      "Generated {TokenCount} tokens for syntax highlighting");

  internal static readonly Action<ILogger, string, string, Exception?> TokenProcessed =
    LoggerMessage.Define<string, string>(
      LogLevel.Trace,
      new EventId(2302, "TokenProcessed"),
      "Token: Type={TokenType}, Text='{TokenText}'");

  internal static readonly Action<ILogger, string, bool, Exception?> CommandRecognitionChecked =
    LoggerMessage.Define<string, bool>(
      LogLevel.Debug,
      new EventId(2303, "CommandRecognitionChecked"),
      "Command recognition: '{Command}' IsKnown={IsKnown}");

  internal static readonly Action<ILogger, string, Exception?> HighlightedTextGenerated =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(2304, "HighlightedTextGenerated"),
      "Generated highlighted text: '{HighlightedText}'");

  // ===== REPL Display Messages (2400-2499) =====
  internal static readonly Action<ILogger, string, Exception?> LineRedrawn =
    LoggerMessage.Define<string>(
      LogLevel.Trace,
      new EventId(2400, "LineRedrawn"),
      "Line redrawn: '{UserInput}'");

  internal static readonly Action<ILogger, int, int, Exception?> CursorPositionUpdated =
    LoggerMessage.Define<int, int>(
      LogLevel.Trace,
      new EventId(2401, "CursorPositionUpdated"),
      "Cursor position updated: PromptLength={PromptLength}, CursorPosition={CursorPosition}");

  internal static readonly Action<ILogger, string, Exception?> ShowCompletionCandidatesStarted =
    LoggerMessage.Define<string>(
      LogLevel.Debug,
      new EventId(2402, "ShowCompletionCandidatesStarted"),
      "Showing completion candidates for input: '{Input}'");
}