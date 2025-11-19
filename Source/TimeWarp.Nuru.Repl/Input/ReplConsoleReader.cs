namespace TimeWarp.Nuru.Repl.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;

/// <summary>
/// Provides advanced console input handling for REPL mode with tab completion and history navigation.
/// </summary>
public sealed class ReplConsoleReader
{
  private readonly List<string> History;
  private readonly CompletionProvider CompletionProvider;
  private readonly EndpointCollection Endpoints;
  private readonly bool EnableColors;

  private string CurrentLine = string.Empty;
  private int CursorPosition;
  private int HistoryIndex = -1;
  private List<string> CompletionCandidates = [];
  private int CompletionIndex = -1;

  // ANSI escape codes for colored terminal output
  private const string AnsiReset = "\x1b[0m";
  private const string AnsiGreen = "\x1b[32m";
  private const string AnsiGray = "\x1b[90m";

  /// <summary>
  /// Creates a new REPL console reader.
  /// </summary>
  /// <param name="history">The command history list.</param>
  /// <param name="completionProvider">The completion provider for tab completion.</param>
  /// <param name="endpoints">The endpoint collection for completion.</param>
  /// <param name="enableColors">Whether to enable colored output.</param>
  public ReplConsoleReader(
    IEnumerable<string> history,
    CompletionProvider completionProvider,
    EndpointCollection endpoints,
    bool enableColors = true)
  {
    History = history?.ToList() ?? throw new ArgumentNullException(nameof(history));
    CompletionProvider = completionProvider ?? throw new ArgumentNullException(nameof(completionProvider));
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    EnableColors = enableColors;
  }

  /// <summary>
  /// Reads a line of input with advanced editing capabilities.
  /// </summary>
  /// <param name="prompt">The prompt to display.</param>
  /// <returns>The input line from user.</returns>
  public string ReadLine(string prompt)
  {
    System.Console.Write(GetFormattedPrompt(prompt));

    CurrentLine = string.Empty;
    CursorPosition = 0;
    HistoryIndex = History.Count;
    CompletionCandidates.Clear();
    CompletionIndex = -1;

    while (true)
    {
      ConsoleKeyInfo keyInfo = System.Console.ReadKey(true);

      switch (keyInfo.Key)
      {
        case ConsoleKey.Enter:
          HandleEnter();
          return CurrentLine;

        case ConsoleKey.Tab:
          HandleTabCompletion(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
          break;

        case ConsoleKey.Backspace:
          HandleBackspace();
          break;

        case ConsoleKey.Delete:
          HandleDelete();
          break;

        case ConsoleKey.LeftArrow:
          HandleLeftArrow(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
          break;

        case ConsoleKey.RightArrow:
          HandleRightArrow(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
          break;

        case ConsoleKey.Home:
          HandleHome();
          break;

        case ConsoleKey.End:
          HandleEnd();
          break;

        case ConsoleKey.UpArrow:
          HandleUpArrow();
          break;

        case ConsoleKey.DownArrow:
          HandleDownArrow();
          break;

        case ConsoleKey.Escape:
          HandleEscape();
          break;

        default:
          if (!char.IsControl(keyInfo.KeyChar))
          {
            HandleCharacter(keyInfo.KeyChar);
          }

          break;
      }
    }
  }

  private string GetFormattedPrompt(string prompt)
  {
    return EnableColors ? AnsiGreen + prompt + AnsiReset : prompt;
  }

  private void HandleEnter()
  {
    System.Console.WriteLine();

    // Add to history if not empty and not duplicate of last entry
    if (!string.IsNullOrWhiteSpace(CurrentLine))
    {
      if (History.Count == 0 || History[History.Count - 1] != CurrentLine)
      {
        History.Add(CurrentLine);
      }
    }
  }

  private void HandleTabCompletion(bool reverse)
  {
    // Parse current line into arguments for completion context
    string[] args = CommandLineParser.Parse(CurrentLine[..CursorPosition]);

    // Build completion context
    var context = new CompletionContext(
      Args: args,
      CursorPosition: CursorPosition,
      Endpoints: Endpoints
    );

    // Get completion candidates
#pragma warning disable IDE0007 // Use implicit type
    var candidates = CompletionProvider.GetCompletions(context, Endpoints).ToList();
#pragma warning restore IDE0007 // Use implicit type

    if (candidates.Count == 0)
      return;

    if (candidates.Count == 1)
    {
      // Single completion - apply it
      ApplyCompletion(candidates[0]);
    }
    else
    {
      // Multiple completions - cycle through them or show all
      if (CompletionCandidates.Count != candidates.Count ||
          !candidates.Select(c => c.Value).SequenceEqual(CompletionCandidates))
      {
        // New completion set - show all candidates
        CompletionCandidates = candidates.ConvertAll(c => c.Value);
        ShowCompletionCandidates(candidates);
      }
      else
      {
        // Same completion set - cycle through them
        CompletionIndex = reverse
          ? (CompletionIndex - 1 + candidates.Count) % candidates.Count
          : (CompletionIndex + 1) % candidates.Count;

        ApplyCompletion(candidates[CompletionIndex]);
      }
    }
  }

  private void ApplyCompletion(CompletionCandidate candidate)
  {
    // Find the start position of the word to complete
    int wordStart = FindWordStart(CurrentLine, CursorPosition);

    // Replace the word with the completion
    CurrentLine = CurrentLine[..wordStart] + candidate.Value + CurrentLine[CursorPosition..];
    CursorPosition = wordStart + candidate.Value.Length;

    // Redraw the line
    RedrawLine();
  }

  private static int FindWordStart(string line, int position)
  {
    // Find the start of the current word (simplified - looks for space before position)
    for (int i = position - 1; i >= 0; i--)
    {
      if (char.IsWhiteSpace(line[i]))
        return i + 1;
    }

    return 0;
  }

  private void ShowCompletionCandidates(List<CompletionCandidate> candidates)
  {
    System.Console.WriteLine();
    if (EnableColors)
    {
      System.Console.WriteLine(AnsiGray + "Available completions:" + AnsiReset);
    }
    else
    {
      System.Console.WriteLine("Available completions:");
    }

    // Display nicely in columns
    int maxLen = candidates.Max(c => c.Value.Length) + 2;
    int columns = Math.Max(1, System.Console.WindowWidth / maxLen);

    for (int i = 0; i < candidates.Count; i++)
    {
      CompletionCandidate candidate = candidates[i];
      string padded = candidate.Value.PadRight(maxLen);

      System.Console.Write(padded);

      if ((i + 1) % columns == 0)
        System.Console.WriteLine();
    }

    if (candidates.Count % columns != 0)
      System.Console.WriteLine();

    // Redraw the prompt and current line
    System.Console.Write(GetFormattedPrompt("> "));
    System.Console.Write(CurrentLine);
  }

  private void HandleBackspace()
  {
    if (CursorPosition > 0)
    {
      CurrentLine = CurrentLine[..(CursorPosition - 1)] + CurrentLine[CursorPosition..];
      CursorPosition--;
      RedrawLine();
    }
  }

  private void HandleDelete()
  {
    if (CursorPosition < CurrentLine.Length)
    {
      CurrentLine = CurrentLine[..CursorPosition] + CurrentLine[(CursorPosition + 1)..];
      RedrawLine();
    }
  }

  private void HandleLeftArrow(bool ctrl)
  {
    if (ctrl)
    {
      // Move to previous word
      int newPos = CursorPosition;
      while (newPos > 0 && char.IsWhiteSpace(CurrentLine[newPos - 1]))
        newPos--;
      while (newPos > 0 && !char.IsWhiteSpace(CurrentLine[newPos - 1]))
        newPos--;
      CursorPosition = newPos;
    }
    else
    {
      // Move one character left
      if (CursorPosition > 0)
        CursorPosition--;
    }

    UpdateCursorPosition();
  }

  private void HandleRightArrow(bool ctrl)
  {
    if (ctrl)
    {
      // Move to next word
      int newPos = CursorPosition;
      while (newPos < CurrentLine.Length && !char.IsWhiteSpace(CurrentLine[newPos]))
        newPos++;
      while (newPos < CurrentLine.Length && char.IsWhiteSpace(CurrentLine[newPos]))
        newPos++;
      CursorPosition = newPos;
    }
    else
    {
      // Move one character right
      if (CursorPosition < CurrentLine.Length)
        CursorPosition++;
    }

    UpdateCursorPosition();
  }

  private void HandleHome()
  {
    CursorPosition = 0;
    UpdateCursorPosition();
  }

  private void HandleEnd()
  {
    CursorPosition = CurrentLine.Length;
    UpdateCursorPosition();
  }

  private void HandleUpArrow()
  {
    if (HistoryIndex > 0)
    {
      HistoryIndex--;
      CurrentLine = History[HistoryIndex];
      CursorPosition = CurrentLine.Length;
      RedrawLine();
    }
  }

  private void HandleDownArrow()
  {
    if (HistoryIndex < History.Count - 1)
    {
      HistoryIndex++;
      CurrentLine = History[HistoryIndex];
      CursorPosition = CurrentLine.Length;
      RedrawLine();
    }
    else if (HistoryIndex == History.Count - 1)
    {
      HistoryIndex = History.Count;
      CurrentLine = string.Empty;
      CursorPosition = 0;
      RedrawLine();
    }
  }

  private void HandleEscape()
  {
    // Clear completion state
    CompletionCandidates.Clear();
    CompletionIndex = -1;
  }

  private void HandleCharacter(char charToInsert)
  {
    CurrentLine = CurrentLine[..CursorPosition] + charToInsert + CurrentLine[CursorPosition..];
    CursorPosition++;
    RedrawLine();
  }

  private void RedrawLine()
  {
    // Move cursor to beginning of line
    System.Console.SetCursorPosition(0, System.Console.CursorTop);

    // Clear the line
    System.Console.Write(new string(' ', System.Console.WindowWidth));

    // Move back to beginning
    System.Console.SetCursorPosition(0, System.Console.CursorTop);

    // Redraw the prompt and current line
    System.Console.Write(GetFormattedPrompt("> "));
    System.Console.Write(CurrentLine);

    // Update cursor position
    UpdateCursorPosition();
  }

  private void UpdateCursorPosition()
  {
    // Calculate desired cursor position (after prompt)
    int promptLength = 2; // "> " length
    int desiredLeft = promptLength + CursorPosition;

    // Set cursor position if within bounds
    if (desiredLeft < System.Console.WindowWidth)
    {
      System.Console.SetCursorPosition(desiredLeft, System.Console.CursorTop);
    }
  }
}