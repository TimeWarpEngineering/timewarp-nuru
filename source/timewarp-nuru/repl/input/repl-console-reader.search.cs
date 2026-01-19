namespace TimeWarp.Nuru;

/// <summary>
/// Interactive history search handlers (Ctrl+R / Ctrl+S) for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: ReverseSearchHistory - Enter interactive reverse incremental search mode.
  /// </summary>
  /// <remarks>
  /// In search mode, the user types a search pattern and sees matching history entries
  /// in real-time. Pressing Ctrl+R again cycles to the next (older) match.
  /// </remarks>
  internal void HandleReverseSearchHistory()
  {
    if (CurrentMode == EditMode.Normal)
    {
      EnterSearchMode(reverse: true);
    }
    else if (CurrentMode == EditMode.Search)
    {
      // Already in search mode - find next match in reverse direction
      SearchDirectionIsReverse = true;
      FindNextMatch(reverse: true);
    }
  }

  /// <summary>
  /// PSReadLine: ForwardSearchHistory - Enter interactive forward incremental search mode.
  /// </summary>
  /// <remarks>
  /// In search mode, the user types a search pattern and sees matching history entries
  /// in real-time. Pressing Ctrl+S again cycles to the next (newer) match.
  /// </remarks>
  internal void HandleForwardSearchHistory()
  {
    if (CurrentMode == EditMode.Normal)
    {
      EnterSearchMode(reverse: false);
    }
    else if (CurrentMode == EditMode.Search)
    {
      // Already in search mode - find next match in forward direction
      SearchDirectionIsReverse = false;
      FindNextMatch(reverse: false);
    }
  }

  private void EnterSearchMode(bool reverse)
  {
    // Save current state to restore on cancel
    SavedInputBeforeSearch = UserInput;
    SavedCursorBeforeSearch = CursorPosition;

    // Initialize search state
    CurrentMode = EditMode.Search;
    SearchPattern = string.Empty;
    SearchMatchIndex = reverse ? History.Count : -1;  // Start from end (reverse) or beginning (forward)
    SearchDirectionIsReverse = reverse;

    // Display search prompt
    RedrawSearchLine();
  }

  private void ExitSearchMode(bool acceptMatch)
  {
    CurrentMode = EditMode.Normal;

    if (acceptMatch && SearchMatchIndex >= 0 && SearchMatchIndex < History.Count)
    {
      // Accept: keep the matched history entry as user input
      UserInput = History[SearchMatchIndex];
      CursorPosition = UserInput.Length;
      HistoryIndex = SearchMatchIndex;
    }
    else
    {
      // Cancel: restore original input
      UserInput = SavedInputBeforeSearch;
      CursorPosition = SavedCursorBeforeSearch;
    }

    // Clear search state
    SearchPattern = string.Empty;
    SearchMatchIndex = -1;

    // Redraw in normal mode
    RedrawLine();
  }

  /// <summary>
  /// Handles a key press while in search mode.
  /// </summary>
  /// <returns>
  /// The result string if the read loop should exit (e.g., Enter was pressed),
  /// or null to continue the loop.
  /// </returns>
  private string? HandleSearchModeKey(ConsoleKeyInfo keyInfo)
  {
    ConsoleModifiers mods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);

    // Ctrl+R: find next older match
    if (keyInfo.Key == ConsoleKey.R && mods == ConsoleModifiers.Control)
    {
      SearchDirectionIsReverse = true;
      FindNextMatch(reverse: true);
      return null;
    }

    // Ctrl+S: find next newer match
    if (keyInfo.Key == ConsoleKey.S && mods == ConsoleModifiers.Control)
    {
      SearchDirectionIsReverse = false;
      FindNextMatch(reverse: false);
      return null;
    }

    // Enter: accept current match and exit
    if (keyInfo.Key == ConsoleKey.Enter)
    {
      ExitSearchMode(acceptMatch: true);
      HandleEnter();
      return UserInput;
    }

    // Escape: cancel and restore original input
    if (keyInfo.Key == ConsoleKey.Escape)
    {
      ExitSearchMode(acceptMatch: false);
      return null;
    }

    // Backspace: remove last character from search pattern
    if (keyInfo.Key == ConsoleKey.Backspace)
    {
      if (SearchPattern.Length > 0)
      {
        SearchPattern = SearchPattern[..^1];
        // Re-search from beginning with new pattern
        SearchMatchIndex = SearchDirectionIsReverse ? History.Count : -1;
        FindNextMatch(SearchDirectionIsReverse);
      }

      return null;
    }

    // Ctrl+G: cancel (alternative to Escape, like in Emacs)
    if (keyInfo.Key == ConsoleKey.G && mods == ConsoleModifiers.Control)
    {
      ExitSearchMode(acceptMatch: false);
      return null;
    }

    // Character input: append to search pattern
    if (!char.IsControl(keyInfo.KeyChar))
    {
      SearchPattern += keyInfo.KeyChar;
      // Search with new pattern
      FindNextMatch(SearchDirectionIsReverse);
      return null;
    }

    // Any other key: accept current match, exit search mode, and process the key
    ExitSearchMode(acceptMatch: true);

    // Now process the key in normal mode
    ConsoleModifiers normalizedMods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (keyInfo.Key, normalizedMods);

    if (KeyBindings.TryGetValue(keyBinding, out Action? handler))
    {
      handler();
      if (ExitKeys.Contains(keyBinding))
      {
        return keyInfo.Key == ConsoleKey.Enter ? UserInput : null;
      }
    }

    return null;
  }

  private void FindNextMatch(bool reverse)
  {
    if (History.Count == 0)
    {
      RedrawSearchLine();
      return;
    }

    int startIndex = SearchMatchIndex;

    if (reverse)
    {
      // Search backward (older entries)
      int searchIndex = startIndex == History.Count ? History.Count - 1 : startIndex - 1;
      while (searchIndex >= 0)
      {
        if (string.IsNullOrEmpty(SearchPattern) ||
            History[searchIndex].Contains(SearchPattern, StringComparison.OrdinalIgnoreCase))
        {
          SearchMatchIndex = searchIndex;
          RedrawSearchLine();
          return;
        }

        searchIndex--;
      }
    }
    else
    {
      // Search forward (newer entries)
      int searchIndex = startIndex < 0 ? 0 : startIndex + 1;
      while (searchIndex < History.Count)
      {
        if (string.IsNullOrEmpty(SearchPattern) ||
            History[searchIndex].Contains(SearchPattern, StringComparison.OrdinalIgnoreCase))
        {
          SearchMatchIndex = searchIndex;
          RedrawSearchLine();
          return;
        }

        searchIndex++;
      }
    }

    // No match found - keep current state but indicate "failing"
    // For now, just redraw (future: could show "failing" in prompt)
    RedrawSearchLine();
  }

  private void RedrawSearchLine()
  {
    // Move cursor to beginning of line
    (int _, int top) = Terminal.GetCursorPosition();
    Terminal.SetCursorPosition(0, top);

    // Clear line
    Terminal.Write(new string(' ', Terminal.WindowWidth));

    // Move back to beginning
    Terminal.SetCursorPosition(0, top);

    // Build search prompt: (reverse-i-search)`pattern': matched_text
    string direction = SearchDirectionIsReverse ? "reverse" : "forward";
    string searchPrompt = $"({direction}-i-search)`{SearchPattern}': ";

    Terminal.Write(searchPrompt);

    // Show matched history entry (if any)
    if (SearchMatchIndex >= 0 && SearchMatchIndex < History.Count)
    {
      string matchedEntry = History[SearchMatchIndex];
      Terminal.Write(matchedEntry);
    }
  }
}
