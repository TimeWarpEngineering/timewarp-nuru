#region Purpose
// One REPL key binding row: chord, function, description, and category.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// One key binding in a profile listing.
/// </summary>
/// <param name="Key">The chord, for example <c>Ctrl+a</c> or <c>LeftArrow</c>.</param>
/// <param name="Function">The handler name, for example <c>BeginningOfLine</c>.</param>
/// <param name="Description">What the handler does.</param>
/// <param name="Category">The function group, for example <c>Cursor movement functions</c>.</param>
public sealed record KeyBindingRow
(
  string Key,
  string Function,
  string Description,
  string Category
);
