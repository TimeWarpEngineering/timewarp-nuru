namespace TimeWarp.Terminal;

/// <summary>
/// Defines the visual style for borders in terminal widgets.
/// </summary>
public enum BorderStyle
{
  /// <summary>
  /// No border. Content is displayed without any surrounding box.
  /// </summary>
  None,

  /// <summary>
  /// Rounded corners using box-drawing characters ╭╮╰╯ with thin lines ─│.
  /// </summary>
  Rounded,

  /// <summary>
  /// Square corners using box-drawing characters ┌┐└┘ with thin lines ─│.
  /// </summary>
  Square,

  /// <summary>
  /// Double-line border using box-drawing characters ╔╗╚╝═║.
  /// </summary>
  Doubled,

  /// <summary>
  /// Heavy (thick) border using box-drawing characters ┏┓┗┛━┃.
  /// </summary>
  Heavy
}
