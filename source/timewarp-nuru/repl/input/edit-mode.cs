#region Purpose
// Enum of the REPL reader's editing modes, which decide how key presses are interpreted.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Defines the editing mode for the REPL console reader.
/// </summary>
/// <remarks>
/// The mode determines how key presses are interpreted:
/// - Normal: Standard line editing
/// - Search: Interactive history search (Ctrl+R/Ctrl+S)
/// - MenuComplete: Menu-style completion selection (future)
/// </remarks>
internal enum EditMode
{
  /// <summary>Standard line editing mode.</summary>
  Normal,

  /// <summary>Interactive incremental history search mode (Ctrl+R/Ctrl+S).</summary>
  Search,

  /// <summary>Menu completion mode for Ctrl+Space (future).</summary>
  MenuComplete
}
