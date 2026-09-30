namespace TimeWarp.Nuru;

/// <summary>
/// Maps key binding action names to <see cref="ReplConsoleReader"/> handlers.
/// </summary>
/// <remarks>
/// <para>
/// Names are case-insensitive. <c>TabComplete:reverse</c> is an alias of <c>TabCompleteReverse</c>.
/// Digit arguments are <c>DigitArgument:0</c> through <c>DigitArgument:9</c>.
/// </para>
/// <para>
/// Cursor movement: BackwardChar, ForwardChar, BackwardWord, ForwardWord, BeginningOfLine, EndOfLine.
/// History: PreviousHistory, NextHistory, BeginningOfHistory, EndOfHistory, HistorySearchBackward,
/// HistorySearchForward, ReverseSearchHistory, ForwardSearchHistory.
/// Editing: BackwardDeleteChar, DeleteChar, DeleteCharOrExit, Escape, KillLine, DeleteWordBackward,
/// DeleteToLineStart, ClearScreen, ToggleInsertMode.
/// Words: UpcaseWord, DowncaseWord, CapitalizeWord, SwapCharacters, DeleteWord, BackwardDeleteWord.
/// Selection: SelectBackwardChar, SelectForwardChar, SelectBackwardWord, SelectNextWord,
/// SelectBackwardsLine, SelectLine, SelectAll, CopyOrCancelLine, Cut, Paste, DeleteSelection.
/// Kill ring: KillLineToRing, BackwardKillInput, UnixWordRubout, KillWord, BackwardKillWord, Yank, YankPop.
/// Yank arguments: YankLastArg, YankNthArg, DigitArgument:0 through DigitArgument:9.
/// Undo: Undo, Redo, RevertLine.
/// Completion: TabComplete, TabCompleteReverse, PossibleCompletions.
/// Multiline: AddLine.
/// Submit: Enter.
/// </para>
/// <para>
/// Overwrite typing is not a separate action. <c>ToggleInsertMode</c> switches the reader into
/// overwrite, and ordinary character input then replaces the character at the cursor.
/// </para>
/// </remarks>
public static class KeyBindingActionRegistry
{
  private static readonly (Dictionary<string, Func<ReplConsoleReader, Func<Task>>> Actions, string[] Names) Registry =
    CreateRegistry();

  private static (Dictionary<string, Func<ReplConsoleReader, Func<Task>>> Actions, string[] Names) CreateRegistry()
  {
    Dictionary<string, Func<ReplConsoleReader, Func<Task>>> actions = new(StringComparer.OrdinalIgnoreCase);
    List<string> names = [];

    Register(actions, names, "BackwardChar", static reader => reader.HandleBackwardCharAsync);
    Register(actions, names, "ForwardChar", static reader => reader.HandleForwardCharAsync);
    Register(actions, names, "BackwardWord", static reader => reader.HandleBackwardWordAsync);
    Register(actions, names, "ForwardWord", static reader => reader.HandleForwardWordAsync);
    Register(actions, names, "BeginningOfLine", static reader => reader.HandleBeginningOfLineAsync);
    Register(actions, names, "EndOfLine", static reader => reader.HandleEndOfLineAsync);

    Register(actions, names, "PreviousHistory", static reader => reader.HandlePreviousHistoryAsync);
    Register(actions, names, "NextHistory", static reader => reader.HandleNextHistoryAsync);
    Register(actions, names, "BeginningOfHistory", static reader => reader.HandleBeginningOfHistoryAsync);
    Register(actions, names, "EndOfHistory", static reader => reader.HandleEndOfHistoryAsync);
    Register(actions, names, "HistorySearchBackward", static reader => reader.HandleHistorySearchBackwardAsync);
    Register(actions, names, "HistorySearchForward", static reader => reader.HandleHistorySearchForwardAsync);
    Register(actions, names, "ReverseSearchHistory", static reader => reader.HandleReverseSearchHistoryAsync);
    Register(actions, names, "ForwardSearchHistory", static reader => reader.HandleForwardSearchHistoryAsync);

    Register(actions, names, "BackwardDeleteChar", static reader => reader.HandleBackwardDeleteCharAsync);
    Register(actions, names, "DeleteChar", static reader => reader.HandleDeleteCharAsync);
    Register(actions, names, "DeleteCharOrExit", static reader => reader.HandleDeleteCharOrExitAsync);
    Register(actions, names, "Escape", static reader => reader.HandleEscapeAsync);
    Register(actions, names, "KillLine", static reader => reader.HandleKillLineAsync);
    Register(actions, names, "DeleteWordBackward", static reader => reader.HandleDeleteWordBackwardAsync);
    Register(actions, names, "DeleteToLineStart", static reader => reader.HandleDeleteToLineStartAsync);
    Register(actions, names, "ClearScreen", static reader => reader.HandleClearScreenAsync);
    Register(actions, names, "ToggleInsertMode", static reader => reader.HandleToggleInsertModeAsync);

    Register(actions, names, "UpcaseWord", static reader => reader.HandleUpcaseWordAsync);
    Register(actions, names, "DowncaseWord", static reader => reader.HandleDowncaseWordAsync);
    Register(actions, names, "CapitalizeWord", static reader => reader.HandleCapitalizeWordAsync);
    Register(actions, names, "SwapCharacters", static reader => reader.HandleSwapCharactersAsync);
    Register(actions, names, "DeleteWord", static reader => reader.HandleDeleteWordAsync);
    Register(actions, names, "BackwardDeleteWord", static reader => reader.HandleBackwardDeleteWordAsync);

    Register(actions, names, "SelectBackwardChar", static reader => reader.HandleSelectBackwardCharAsync);
    Register(actions, names, "SelectForwardChar", static reader => reader.HandleSelectForwardCharAsync);
    Register(actions, names, "SelectBackwardWord", static reader => reader.HandleSelectBackwardWordAsync);
    Register(actions, names, "SelectNextWord", static reader => reader.HandleSelectNextWordAsync);
    Register(actions, names, "SelectBackwardsLine", static reader => reader.HandleSelectBackwardsLineAsync);
    Register(actions, names, "SelectLine", static reader => reader.HandleSelectLineAsync);
    Register(actions, names, "SelectAll", static reader => reader.HandleSelectAllAsync);
    Register(actions, names, "CopyOrCancelLine", static reader => reader.HandleCopyOrCancelLineAsync);
    Register(actions, names, "Cut", static reader => reader.HandleCutAsync);
    Register(actions, names, "Paste", static reader => reader.HandlePasteAsync);
    Register(actions, names, "DeleteSelection", static reader => reader.HandleDeleteSelectionAsync);

    Register(actions, names, "KillLineToRing", static reader => reader.HandleKillLineToRingAsync);
    Register(actions, names, "BackwardKillInput", static reader => reader.HandleBackwardKillInputAsync);
    Register(actions, names, "UnixWordRubout", static reader => reader.HandleUnixWordRuboutAsync);
    Register(actions, names, "KillWord", static reader => reader.HandleKillWordAsync);
    Register(actions, names, "BackwardKillWord", static reader => reader.HandleBackwardKillWordAsync);
    Register(actions, names, "Yank", static reader => reader.HandleYankAsync);
    Register(actions, names, "YankPop", static reader => reader.HandleYankPopAsync);

    Register(actions, names, "YankLastArg", static reader => reader.HandleYankLastArgAsync);
    Register(actions, names, "YankNthArg", static reader => reader.HandleYankNthArgAsync);
    for (int digit = 0; digit <= 9; digit++)
    {
      int captured = digit;
      Register
      (
        actions,
        names,
        $"DigitArgument:{captured}",
        reader => () =>
        {
          reader.HandleDigitArgument(captured);
          return Task.CompletedTask;
        }
      );
    }

    Register(actions, names, "Undo", static reader => reader.HandleUndoAsync);
    Register(actions, names, "Redo", static reader => reader.HandleRedoAsync);
    Register(actions, names, "RevertLine", static reader => reader.HandleRevertLineAsync);

    Register(actions, names, "TabComplete", static reader => () => reader.HandleTabCompletionAsync(reverse: false));
    Func<ReplConsoleReader, Func<Task>> tabReverse = static reader => () => reader.HandleTabCompletionAsync(reverse: true);
    Register(actions, names, "TabCompleteReverse", tabReverse);
    Register(actions, names, "TabComplete:reverse", tabReverse, alias: true);
    Register(actions, names, "PossibleCompletions", static reader => reader.HandlePossibleCompletionsAsync);

    Register(actions, names, "AddLine", static reader => reader.HandleAddLineAsync);
    Register(actions, names, "Enter", static reader => reader.HandleEnterAsync);

    return (actions, [.. names]);
  }

  /// <summary>
  /// Creates the handler for an action name.
  /// </summary>
  /// <param name="name">The action name, for example <c>BackwardChar</c> or <c>DigitArgument:3</c>.</param>
  /// <param name="reader">The reader whose handler will run.</param>
  /// <returns>The async handler.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> is null.</exception>
  /// <exception cref="KeyBindingConfigException">Thrown when the action name is unknown.</exception>
  public static Func<Task> GetAction(string name, ReplConsoleReader reader)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentNullException.ThrowIfNull(reader);

    if (!Registry.Actions.TryGetValue(name.Trim(), out Func<ReplConsoleReader, Func<Task>>? factory))
    {
      throw new KeyBindingConfigException
      (
        $"Unknown key binding action '{name}'. Call GetAvailableActions for the supported names."
      );
    }

    return factory(reader);
  }

  /// <summary>
  /// Returns the canonical action names, in registry order.
  /// </summary>
  /// <returns>The action names users can put in a config file.</returns>
  [SuppressMessage(
    "Design",
    "CA1024:Use properties where appropriate",
    Justification = "GetAvailableActions is the discoverability method for config authors.")]
  public static IReadOnlyList<string> GetAvailableActions() => Registry.Names;

  /// <summary>
  /// Returns whether <paramref name="name"/> is a known action or alias.
  /// </summary>
  /// <param name="name">The action name to test.</param>
  /// <returns><c>true</c> when the registry can create a handler for the name.</returns>
  public static bool IsKnownAction(string name) =>
    !string.IsNullOrWhiteSpace(name) && Registry.Actions.ContainsKey(name.Trim());

  private static void Register
  (
    Dictionary<string, Func<ReplConsoleReader, Func<Task>>> actions,
    List<string> names,
    string name,
    Func<ReplConsoleReader, Func<Task>> factory,
    bool alias = false
  )
  {
    actions.Add(name, factory);
    if (!alias)
    {
      names.Add(name);
    }
  }
}
