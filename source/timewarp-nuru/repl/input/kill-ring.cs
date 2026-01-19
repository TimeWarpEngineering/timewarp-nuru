namespace TimeWarp.Nuru;

/// <summary>
/// Implements an Emacs-style kill ring for storing killed (cut) text.
/// </summary>
/// <remarks>
/// <para>
/// The kill ring is a circular buffer that stores text deleted by "kill" commands.
/// This enables users to recover previously deleted text using yank (Ctrl+Y) and
/// cycle through the ring using yank-pop (Alt+Y).
/// </para>
/// <para>
/// Key behaviors:
/// <list type="bullet">
/// <item>Consecutive kills append to the same kill ring entry</item>
/// <item>Kill ring is separate from the system clipboard</item>
/// <item>Ring rotates when full - oldest entries are discarded</item>
/// </list>
/// </para>
/// </remarks>
public sealed class KillRing
{
  private readonly string[] Ring;
  private readonly int Capacity;
  private int Head;        // Points to next write position
  private int Count;       // Number of items in ring
  private int YankIndex;   // Position for YankPop cycling

  /// <summary>
  /// Gets the default capacity for a kill ring.
  /// </summary>
  public const int DefaultCapacity = 10;

  /// <summary>
  /// Creates a new kill ring with the specified capacity.
  /// </summary>
  /// <param name="capacity">The maximum number of entries to store.</param>
  public KillRing(int capacity = DefaultCapacity)
  {
    if (capacity <= 0)
      throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");

    Capacity = capacity;
    Ring = new string[capacity];
    Head = 0;
    Count = 0;
    YankIndex = -1;
  }

  /// <summary>
  /// Gets the number of items in the kill ring.
  /// </summary>
  public int ItemCount => Count;

  /// <summary>
  /// Gets whether the kill ring is empty.
  /// </summary>
  public bool IsEmpty => Count == 0;

  /// <summary>
  /// Adds text to the kill ring as a new entry.
  /// </summary>
  /// <param name="text">The text to add.</param>
  public void Add(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    Ring[Head] = text;
    Head = (Head + 1) % Capacity;

    if (Count < Capacity)
      Count++;

    // Reset yank index when new text is added
    YankIndex = -1;
  }

  /// <summary>
  /// Appends text to the most recent kill ring entry.
  /// Used when consecutive kill commands are executed.
  /// </summary>
  /// <param name="text">The text to append.</param>
  /// <param name="prepend">If true, prepends instead of appends (for backward kills).</param>
  public void AppendToLast(string text, bool prepend = false)
  {
    if (string.IsNullOrEmpty(text))
      return;

    if (Count == 0)
    {
      Add(text);
      return;
    }

    int lastIndex = (Head - 1 + Capacity) % Capacity;
    Ring[lastIndex] = prepend
      ? text + Ring[lastIndex]
      : Ring[lastIndex] + text;
  }

  /// <summary>
  /// Gets the most recently killed text (for Yank/Ctrl+Y).
  /// </summary>
  /// <returns>The most recent kill ring entry, or null if empty.</returns>
  public string? Yank()
  {
    if (Count == 0)
      return null;

    YankIndex = (Head - 1 + Capacity) % Capacity;
    return Ring[YankIndex];
  }

  /// <summary>
  /// Gets the previous kill ring entry (for YankPop/Alt+Y).
  /// Cycles through the ring, returning to the start after reaching the oldest entry.
  /// </summary>
  /// <returns>The previous kill ring entry, or null if empty or Yank wasn't called first.</returns>
  public string? YankPop()
  {
    if (Count == 0 || YankIndex < 0)
      return null;

    int newestIndex = (Head - 1 + Capacity) % Capacity;

    if (Count < Capacity)
    {
      // Ring not full - entries are at indices 0 to Count-1
      // Move backward, wrap from 0 to Count-1
      if (YankIndex == 0)
        YankIndex = Count - 1;
      else
        YankIndex--;
    }
    else
    {
      // Ring full - all indices are valid
      // Move backward with modular arithmetic
      YankIndex = (YankIndex - 1 + Capacity) % Capacity;
    }

    return Ring[YankIndex];
  }

  /// <summary>
  /// Resets the yank position. Called when a non-yank command is executed.
  /// </summary>
  public void ResetYankPosition()
  {
    YankIndex = -1;
  }

  /// <summary>
  /// Gets whether a yank-pop operation is valid (Yank was the previous operation).
  /// </summary>
  public bool CanYankPop => YankIndex >= 0;

  /// <summary>
  /// Clears all entries from the kill ring.
  /// </summary>
  public void Clear()
  {
    Array.Clear(Ring);
    Head = 0;
    Count = 0;
    YankIndex = -1;
  }
}
