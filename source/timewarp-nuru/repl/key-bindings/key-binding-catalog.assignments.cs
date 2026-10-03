#region Purpose
// Chord tables and function descriptions for the built-in REPL key binding profiles.
#endregion

#region Design
// Each Add method mirrors one profile's GetBindings dictionary. Digit arguments and the
// shared yank-argument chords are helpers because every built-in profile binds them
// the same way. EnsureTable rejects a duplicate chord or an action the registry
// does not know, so the listing cannot drift from KeyBindingActionRegistry names.
#endregion

namespace TimeWarp.Nuru;

public static partial class KeyBindingCatalog
{
  private readonly record struct Assignment
  (
    string Profile,
    ConsoleKey Key,
    ConsoleModifiers Modifiers,
    string Function
  );

  private readonly record struct FunctionInfo(string Category, string Description);

  private static readonly Assignment[] Assignments = CreateAssignments();

  private static readonly Dictionary<string, FunctionInfo> Functions = CreateFunctions();

  private static void EnsureTable()
  {
    HashSet<(string Profile, ConsoleKey Key, ConsoleModifiers Modifiers)> seen = [];
    foreach (Assignment assignment in Assignments)
    {
      if (!IsKnownProfile(assignment.Profile))
      {
        throw new InvalidOperationException($"Catalog profile '{assignment.Profile}' is not built in.");
      }

      if (!seen.Add((assignment.Profile, assignment.Key, assignment.Modifiers)))
      {
        throw new InvalidOperationException
        (
          $"Duplicate {assignment.Profile} binding for {assignment.Key} {assignment.Modifiers}."
        );
      }

      if (!Functions.ContainsKey(assignment.Function))
      {
        throw new InvalidOperationException($"Catalog function '{assignment.Function}' has no description.");
      }

      if (!KeyBindingActionRegistry.IsKnownAction(assignment.Function))
      {
        throw new InvalidOperationException($"Catalog function '{assignment.Function}' is not a registered action.");
      }
    }
  }

  private static Dictionary<string, FunctionInfo> CreateFunctions()
  {
    Dictionary<string, FunctionInfo> functions = new(StringComparer.Ordinal);
    Add(functions, "Enter", BasicEditing, "Submit the input.");
    Add(functions, "AddLine", BasicEditing, "Insert a new line without submitting.");
    Add(functions, "BackwardDeleteChar", BasicEditing, "Delete the character before the cursor.");
    Add(functions, "DeleteChar", BasicEditing, "Delete the character at the cursor.");
    Add(functions, "DeleteCharOrExit", BasicEditing, "Delete the character at the cursor, or exit when the line is empty.");
    Add(functions, "KillLine", BasicEditing, "Delete from the cursor to the end of the line.");
    Add(functions, "BackwardKillInput", BasicEditing, "Delete from the start of the line to the cursor.");
    Add(functions, "UnixWordRubout", BasicEditing, "Delete the previous whitespace-delimited word.");
    Add(functions, "KillWord", BasicEditing, "Delete from the cursor to the end of the word.");
    Add(functions, "BackwardKillWord", BasicEditing, "Delete from the start of the word to the cursor.");
    Add(functions, "BackwardDeleteWord", BasicEditing, "Delete the word before the cursor.");
    Add(functions, "Yank", BasicEditing, "Insert the most recent kill-ring entry.");
    Add(functions, "YankPop", BasicEditing, "Replace the last yank with the previous kill-ring entry.");
    Add(functions, "YankLastArg", BasicEditing, "Insert the last argument from the previous command.");
    Add(functions, "YankNthArg", BasicEditing, "Insert an argument from the previous command.");
    Add(functions, "Undo", BasicEditing, "Undo the last edit.");
    Add(functions, "Redo", BasicEditing, "Redo the last undone edit.");
    Add(functions, "RevertLine", BasicEditing, "Restore the line to its original text.");
    Add(functions, "UpcaseWord", BasicEditing, "Uppercase the word at the cursor.");
    Add(functions, "DowncaseWord", BasicEditing, "Lowercase the word at the cursor.");
    Add(functions, "CapitalizeWord", BasicEditing, "Capitalize the word at the cursor.");
    Add(functions, "SwapCharacters", BasicEditing, "Swap the characters around the cursor.");
    Add(functions, "CopyOrCancelLine", BasicEditing, "Copy the selection, or cancel the line when nothing is selected.");
    Add(functions, "Cut", BasicEditing, "Cut the selection to the clipboard.");
    Add(functions, "Paste", BasicEditing, "Paste the clipboard at the cursor.");

    Add(functions, "BackwardChar", CursorMovement, "Move the cursor back one character.");
    Add(functions, "ForwardChar", CursorMovement, "Move the cursor forward one character.");
    Add(functions, "BackwardWord", CursorMovement, "Move the cursor back one word.");
    Add(functions, "ForwardWord", CursorMovement, "Move the cursor forward one word.");
    Add(functions, "BeginningOfLine", CursorMovement, "Move the cursor to the beginning of the line.");
    Add(functions, "EndOfLine", CursorMovement, "Move the cursor to the end of the line.");

    Add(functions, "PreviousHistory", History, "Replace the input with the previous history item.");
    Add(functions, "NextHistory", History, "Replace the input with the next history item.");
    Add(functions, "BeginningOfHistory", History, "Replace the input with the first history item.");
    Add(functions, "EndOfHistory", History, "Replace the input with the last history item.");
    Add(functions, "HistorySearchBackward", History, "Search history backward for the current prefix.");
    Add(functions, "HistorySearchForward", History, "Search history forward for the current prefix.");
    Add(functions, "ReverseSearchHistory", History, "Search history backward interactively.");
    Add(functions, "ForwardSearchHistory", History, "Search history forward interactively.");

    Add(functions, "TabComplete", Completion, "Complete the input using the next completion.");
    Add(functions, "TabCompleteReverse", Completion, "Complete the input using the previous completion.");
    Add(functions, "PossibleCompletions", Completion, "Show the available completions.");

    Add(functions, "SelectBackwardChar", Selection, "Extend the selection back one character.");
    Add(functions, "SelectForwardChar", Selection, "Extend the selection forward one character.");
    Add(functions, "SelectBackwardWord", Selection, "Extend the selection back one word.");
    Add(functions, "SelectNextWord", Selection, "Extend the selection forward one word.");
    Add(functions, "SelectBackwardsLine", Selection, "Extend the selection to the beginning of the line.");
    Add(functions, "SelectLine", Selection, "Extend the selection to the end of the line.");
    Add(functions, "SelectAll", Selection, "Select the whole line.");

    Add(functions, "ClearScreen", Miscellaneous, "Clear the terminal screen.");
    Add(functions, "Escape", Miscellaneous, "Cancel the line or the current search.");
    Add(functions, "ToggleInsertMode", Miscellaneous, "Toggle between insert and overwrite.");
    for (int digit = 0; digit <= 9; digit++)
    {
      Add
      (
        functions,
        $"DigitArgument:{digit}",
        Miscellaneous,
        $"Pass {digit} as the numeric argument for the next operation."
      );
    }

    return functions;
  }

  private static void Add
  (
    Dictionary<string, FunctionInfo> functions,
    string name,
    string category,
    string description
  )
  {
    functions.Add(name, new FunctionInfo(category, description));
  }

  private static Assignment[] CreateAssignments()
  {
    List<Assignment> assignments = [];
    AddDefault(assignments);
    AddEmacs(assignments);
    AddVi(assignments);
    AddVisualStudioCode(assignments);
    return [.. assignments];
  }

  private static void AddDigits(List<Assignment> assignments, string profile)
  {
    for (int digit = 0; digit <= 9; digit++)
    {
      ConsoleKey key = (ConsoleKey)((int)ConsoleKey.D0 + digit);
      assignments.Add(new Assignment(profile, key, ConsoleModifiers.Alt, $"DigitArgument:{digit}"));
    }
  }

  private static void AddDefault(List<Assignment> assignments)
  {
    const string Profile = "Default";
    void Add(ConsoleKey key, ConsoleModifiers modifiers, string function) =>
      assignments.Add(new Assignment(Profile, key, modifiers, function));

    Add(ConsoleKey.Enter, ConsoleModifiers.None, "Enter");
    Add(ConsoleKey.Enter, ConsoleModifiers.Shift, "AddLine");
    Add(ConsoleKey.Tab, ConsoleModifiers.None, "TabComplete");
    Add(ConsoleKey.Tab, ConsoleModifiers.Shift, "TabCompleteReverse");
    Add(ConsoleKey.Oem7, ConsoleModifiers.Alt, "PossibleCompletions");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.None, "BackwardChar");
    Add(ConsoleKey.B, ConsoleModifiers.Control, "BackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.None, "ForwardChar");
    Add(ConsoleKey.F, ConsoleModifiers.Control, "ForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control, "BackwardWord");
    Add(ConsoleKey.B, ConsoleModifiers.Alt, "BackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control, "ForwardWord");
    Add(ConsoleKey.F, ConsoleModifiers.Alt, "ForwardWord");
    Add(ConsoleKey.Home, ConsoleModifiers.None, "BeginningOfLine");
    Add(ConsoleKey.A, ConsoleModifiers.Control, "BeginningOfLine");
    Add(ConsoleKey.End, ConsoleModifiers.None, "EndOfLine");
    Add(ConsoleKey.E, ConsoleModifiers.Control, "EndOfLine");

    Add(ConsoleKey.UpArrow, ConsoleModifiers.None, "PreviousHistory");
    Add(ConsoleKey.P, ConsoleModifiers.Control, "PreviousHistory");
    Add(ConsoleKey.DownArrow, ConsoleModifiers.None, "NextHistory");
    Add(ConsoleKey.N, ConsoleModifiers.Control, "NextHistory");
    Add(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "BeginningOfHistory");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "EndOfHistory");
    Add(ConsoleKey.F8, ConsoleModifiers.None, "HistorySearchBackward");
    Add(ConsoleKey.F8, ConsoleModifiers.Shift, "HistorySearchForward");
    Add(ConsoleKey.R, ConsoleModifiers.Control, "ReverseSearchHistory");
    Add(ConsoleKey.S, ConsoleModifiers.Control, "ForwardSearchHistory");

    Add(ConsoleKey.Backspace, ConsoleModifiers.None, "BackwardDeleteChar");
    Add(ConsoleKey.Delete, ConsoleModifiers.None, "DeleteChar");
    Add(ConsoleKey.K, ConsoleModifiers.Control, "KillLine");
    Add(ConsoleKey.U, ConsoleModifiers.Control, "BackwardKillInput");
    Add(ConsoleKey.W, ConsoleModifiers.Control, "UnixWordRubout");
    Add(ConsoleKey.D, ConsoleModifiers.Alt, "KillWord");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Alt, "BackwardKillWord");
    Add(ConsoleKey.Y, ConsoleModifiers.Control, "Yank");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt, "YankPop");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt, "YankLastArg");
    Add(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "YankLastArg");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control, "YankNthArg");
    AddDigits(assignments, Profile);

    Add(ConsoleKey.U, ConsoleModifiers.Alt, "UpcaseWord");
    Add(ConsoleKey.L, ConsoleModifiers.Alt, "DowncaseWord");
    Add(ConsoleKey.C, ConsoleModifiers.Alt, "CapitalizeWord");
    Add(ConsoleKey.T, ConsoleModifiers.Control, "SwapCharacters");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Control, "BackwardDeleteWord");

    Add(ConsoleKey.Z, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift, "Redo");
    Add(ConsoleKey.OemMinus, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.R, ConsoleModifiers.Alt, "RevertLine");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Shift, "SelectBackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Shift, "SelectForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.Home, ConsoleModifiers.Shift, "SelectBackwardsLine");
    Add(ConsoleKey.End, ConsoleModifiers.Shift, "SelectLine");
    Add(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectAll");

    Add(ConsoleKey.C, ConsoleModifiers.Control, "CopyOrCancelLine");
    Add(ConsoleKey.X, ConsoleModifiers.Control, "Cut");
    Add(ConsoleKey.V, ConsoleModifiers.Control, "Paste");
    Add(ConsoleKey.D, ConsoleModifiers.Control, "DeleteCharOrExit");
    Add(ConsoleKey.L, ConsoleModifiers.Control, "ClearScreen");
    Add(ConsoleKey.H, ConsoleModifiers.Control, "BackwardDeleteChar");
    Add(ConsoleKey.Insert, ConsoleModifiers.None, "ToggleInsertMode");
    Add(ConsoleKey.M, ConsoleModifiers.Control, "Enter");
    Add(ConsoleKey.J, ConsoleModifiers.Control, "Enter");
    Add(ConsoleKey.Escape, ConsoleModifiers.None, "Escape");
  }

  private static void AddEmacs(List<Assignment> assignments)
  {
    const string Profile = "Emacs";
    void Add(ConsoleKey key, ConsoleModifiers modifiers, string function) =>
      assignments.Add(new Assignment(Profile, key, modifiers, function));

    Add(ConsoleKey.Enter, ConsoleModifiers.None, "Enter");
    Add(ConsoleKey.Enter, ConsoleModifiers.Shift, "AddLine");
    Add(ConsoleKey.Tab, ConsoleModifiers.None, "TabComplete");
    Add(ConsoleKey.Tab, ConsoleModifiers.Shift, "TabCompleteReverse");
    Add(ConsoleKey.Oem7, ConsoleModifiers.Alt, "PossibleCompletions");

    Add(ConsoleKey.F, ConsoleModifiers.Control, "ForwardChar");
    Add(ConsoleKey.B, ConsoleModifiers.Control, "BackwardChar");
    Add(ConsoleKey.F, ConsoleModifiers.Alt, "ForwardWord");
    Add(ConsoleKey.B, ConsoleModifiers.Alt, "BackwardWord");
    Add(ConsoleKey.A, ConsoleModifiers.Control, "BeginningOfLine");
    Add(ConsoleKey.E, ConsoleModifiers.Control, "EndOfLine");

    Add(ConsoleKey.P, ConsoleModifiers.Control, "PreviousHistory");
    Add(ConsoleKey.N, ConsoleModifiers.Control, "NextHistory");
    Add(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "BeginningOfHistory");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "EndOfHistory");
    Add(ConsoleKey.R, ConsoleModifiers.Control, "ReverseSearchHistory");
    Add(ConsoleKey.S, ConsoleModifiers.Control, "ForwardSearchHistory");

    Add(ConsoleKey.Backspace, ConsoleModifiers.None, "BackwardDeleteChar");
    Add(ConsoleKey.H, ConsoleModifiers.Control, "BackwardDeleteChar");
    Add(ConsoleKey.Delete, ConsoleModifiers.None, "DeleteChar");
    Add(ConsoleKey.D, ConsoleModifiers.Control, "DeleteCharOrExit");
    Add(ConsoleKey.K, ConsoleModifiers.Control, "KillLine");
    Add(ConsoleKey.U, ConsoleModifiers.Control, "BackwardKillInput");
    Add(ConsoleKey.W, ConsoleModifiers.Control, "UnixWordRubout");
    Add(ConsoleKey.D, ConsoleModifiers.Alt, "KillWord");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Alt, "BackwardKillWord");
    Add(ConsoleKey.Y, ConsoleModifiers.Control, "Yank");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt, "YankPop");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt, "YankLastArg");
    Add(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "YankLastArg");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control, "YankNthArg");
    AddDigits(assignments, Profile);

    Add(ConsoleKey.U, ConsoleModifiers.Alt, "UpcaseWord");
    Add(ConsoleKey.L, ConsoleModifiers.Alt, "DowncaseWord");
    Add(ConsoleKey.C, ConsoleModifiers.Alt, "CapitalizeWord");
    Add(ConsoleKey.T, ConsoleModifiers.Control, "SwapCharacters");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Control, "BackwardDeleteWord");

    Add(ConsoleKey.OemMinus, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.Z, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift, "Redo");
    Add(ConsoleKey.R, ConsoleModifiers.Alt, "RevertLine");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Shift, "SelectBackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Shift, "SelectForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.Home, ConsoleModifiers.Shift, "SelectBackwardsLine");
    Add(ConsoleKey.End, ConsoleModifiers.Shift, "SelectLine");
    Add(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectAll");

    Add(ConsoleKey.C, ConsoleModifiers.Control, "CopyOrCancelLine");
    Add(ConsoleKey.X, ConsoleModifiers.Control, "Cut");
    Add(ConsoleKey.V, ConsoleModifiers.Control, "Paste");
    Add(ConsoleKey.L, ConsoleModifiers.Control, "ClearScreen");
    Add(ConsoleKey.Insert, ConsoleModifiers.None, "ToggleInsertMode");
    Add(ConsoleKey.M, ConsoleModifiers.Control, "Enter");
    Add(ConsoleKey.J, ConsoleModifiers.Control, "Enter");
    Add(ConsoleKey.Escape, ConsoleModifiers.None, "Escape");
  }

  private static void AddVi(List<Assignment> assignments)
  {
    const string Profile = "Vi";
    void Add(ConsoleKey key, ConsoleModifiers modifiers, string function) =>
      assignments.Add(new Assignment(Profile, key, modifiers, function));

    Add(ConsoleKey.Enter, ConsoleModifiers.None, "Enter");
    Add(ConsoleKey.Enter, ConsoleModifiers.Shift, "AddLine");
    Add(ConsoleKey.Tab, ConsoleModifiers.None, "TabComplete");
    Add(ConsoleKey.Tab, ConsoleModifiers.Shift, "TabCompleteReverse");

    Add(ConsoleKey.B, ConsoleModifiers.Control, "BackwardChar");
    Add(ConsoleKey.F, ConsoleModifiers.Control, "ForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.None, "BackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.None, "ForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control, "BackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control, "ForwardWord");
    Add(ConsoleKey.A, ConsoleModifiers.Control, "BeginningOfLine");
    Add(ConsoleKey.E, ConsoleModifiers.Control, "EndOfLine");
    Add(ConsoleKey.Home, ConsoleModifiers.None, "BeginningOfLine");
    Add(ConsoleKey.End, ConsoleModifiers.None, "EndOfLine");

    Add(ConsoleKey.P, ConsoleModifiers.Control, "PreviousHistory");
    Add(ConsoleKey.N, ConsoleModifiers.Control, "NextHistory");
    Add(ConsoleKey.UpArrow, ConsoleModifiers.None, "PreviousHistory");
    Add(ConsoleKey.DownArrow, ConsoleModifiers.None, "NextHistory");
    Add(ConsoleKey.R, ConsoleModifiers.Control, "ReverseSearchHistory");
    Add(ConsoleKey.S, ConsoleModifiers.Control, "ForwardSearchHistory");

    Add(ConsoleKey.Backspace, ConsoleModifiers.None, "BackwardDeleteChar");
    Add(ConsoleKey.Delete, ConsoleModifiers.None, "DeleteChar");
    Add(ConsoleKey.D, ConsoleModifiers.Control, "DeleteCharOrExit");
    Add(ConsoleKey.W, ConsoleModifiers.Control, "UnixWordRubout");
    Add(ConsoleKey.U, ConsoleModifiers.Control, "BackwardKillInput");
    Add(ConsoleKey.K, ConsoleModifiers.Control, "KillLine");
    Add(ConsoleKey.D, ConsoleModifiers.Alt, "KillWord");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Alt, "BackwardKillWord");
    Add(ConsoleKey.Y, ConsoleModifiers.Control, "Yank");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt, "YankPop");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt, "YankLastArg");
    Add(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "YankLastArg");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control, "YankNthArg");
    AddDigits(assignments, Profile);

    Add(ConsoleKey.Z, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift, "Redo");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Shift, "SelectBackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Shift, "SelectForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.Home, ConsoleModifiers.Shift, "SelectBackwardsLine");
    Add(ConsoleKey.End, ConsoleModifiers.Shift, "SelectLine");

    Add(ConsoleKey.C, ConsoleModifiers.Control, "CopyOrCancelLine");
    Add(ConsoleKey.X, ConsoleModifiers.Control, "Cut");
    Add(ConsoleKey.V, ConsoleModifiers.Control, "Paste");
    Add(ConsoleKey.L, ConsoleModifiers.Control, "ClearScreen");
    Add(ConsoleKey.Escape, ConsoleModifiers.None, "Escape");
  }

  private static void AddVisualStudioCode(List<Assignment> assignments)
  {
    const string Profile = "VSCode";
    void Add(ConsoleKey key, ConsoleModifiers modifiers, string function) =>
      assignments.Add(new Assignment(Profile, key, modifiers, function));

    Add(ConsoleKey.Enter, ConsoleModifiers.None, "Enter");
    Add(ConsoleKey.Enter, ConsoleModifiers.Shift, "AddLine");
    Add(ConsoleKey.Tab, ConsoleModifiers.None, "TabComplete");
    Add(ConsoleKey.Tab, ConsoleModifiers.Shift, "TabCompleteReverse");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.None, "BackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.None, "ForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control, "BackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control, "ForwardWord");
    Add(ConsoleKey.Home, ConsoleModifiers.None, "BeginningOfLine");
    Add(ConsoleKey.End, ConsoleModifiers.None, "EndOfLine");
    Add(ConsoleKey.Home, ConsoleModifiers.Control, "BeginningOfHistory");
    Add(ConsoleKey.End, ConsoleModifiers.Control, "EndOfHistory");

    Add(ConsoleKey.UpArrow, ConsoleModifiers.None, "PreviousHistory");
    Add(ConsoleKey.DownArrow, ConsoleModifiers.None, "NextHistory");
    Add(ConsoleKey.R, ConsoleModifiers.Control, "ReverseSearchHistory");
    Add(ConsoleKey.S, ConsoleModifiers.Control, "ForwardSearchHistory");

    Add(ConsoleKey.Backspace, ConsoleModifiers.None, "BackwardDeleteChar");
    Add(ConsoleKey.Delete, ConsoleModifiers.None, "DeleteChar");
    Add(ConsoleKey.K, ConsoleModifiers.Control, "KillLine");
    Add(ConsoleKey.Backspace, ConsoleModifiers.Control, "BackwardKillWord");
    Add(ConsoleKey.U, ConsoleModifiers.Control, "BackwardKillInput");
    Add(ConsoleKey.Y, ConsoleModifiers.Control, "Yank");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt, "YankPop");
    Add(ConsoleKey.OemPeriod, ConsoleModifiers.Alt, "YankLastArg");
    Add(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift, "YankLastArg");
    Add(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control, "YankNthArg");
    AddDigits(assignments, Profile);

    Add(ConsoleKey.Z, ConsoleModifiers.Control, "Undo");
    Add(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift, "Redo");

    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Shift, "SelectBackwardChar");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Shift, "SelectForwardChar");
    Add(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectBackwardWord");
    Add(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift, "SelectNextWord");
    Add(ConsoleKey.Home, ConsoleModifiers.Shift, "SelectBackwardsLine");
    Add(ConsoleKey.End, ConsoleModifiers.Shift, "SelectLine");
    Add(ConsoleKey.A, ConsoleModifiers.Control, "SelectAll");

    Add(ConsoleKey.C, ConsoleModifiers.Control, "CopyOrCancelLine");
    Add(ConsoleKey.X, ConsoleModifiers.Control, "Cut");
    Add(ConsoleKey.V, ConsoleModifiers.Control, "Paste");
    Add(ConsoleKey.L, ConsoleModifiers.Control, "ClearScreen");
    Add(ConsoleKey.Insert, ConsoleModifiers.None, "ToggleInsertMode");
    Add(ConsoleKey.Escape, ConsoleModifiers.None, "Escape");
  }
}
