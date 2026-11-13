#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins MultilineBuffer deletion, cursor movement, full-text, clear, and position conversion (task 043-009); split from repl-31-multiline-buffer.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.MultilineBufferTestsEditing
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Multiline")]
public class MultilineBufferEditingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultilineBufferEditingTests>();

  // ============================================================================
  // DeleteCharacterBefore (Backspace) Tests
  // ============================================================================

  public static async Task Should_delete_character_before_cursor()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");

    // Act
    bool deleted = buffer.DeleteCharacterBefore();

    // Assert
    deleted.ShouldBeTrue("Should return true when char deleted");
    buffer.Lines[0].ShouldBe("hell", "Last character deleted");
    buffer.Cursor.Column.ShouldBe(4, "Cursor moved back");

    await Task.CompletedTask;
  }

  public static async Task Should_merge_lines_on_backspace_at_line_start()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first\nsecond");
    buffer.SetCursor(1, 0);

    // Act
    bool deleted = buffer.DeleteCharacterBefore();

    // Assert
    deleted.ShouldBeTrue("Should return true");
    buffer.LineCount.ShouldBe(1, "Lines should be merged");
    buffer.Lines[0].ShouldBe("firstsecond", "Lines merged without separator");
    buffer.Cursor.Line.ShouldBe(0, "Cursor on merged line");
    buffer.Cursor.Column.ShouldBe(5, "Cursor at merge point");

    await Task.CompletedTask;
  }

  public static async Task Should_not_delete_at_buffer_start()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    buffer.SetCursor(0, 0);

    // Act
    bool deleted = buffer.DeleteCharacterBefore();

    // Assert
    deleted.ShouldBeFalse("Should return false at buffer start");
    buffer.Lines[0].ShouldBe("hello", "Content unchanged");

    await Task.CompletedTask;
  }

  // ============================================================================
  // DeleteCharacterAt (Delete) Tests
  // ============================================================================

  public static async Task Should_delete_character_at_cursor()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    buffer.SetCursor(0, 0);

    // Act
    bool deleted = buffer.DeleteCharacterAt();

    // Assert
    deleted.ShouldBeTrue("Should return true when char deleted");
    buffer.Lines[0].ShouldBe("ello", "First character deleted");
    buffer.Cursor.Column.ShouldBe(0, "Cursor stays in place");

    await Task.CompletedTask;
  }

  public static async Task Should_merge_lines_on_delete_at_line_end()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first\nsecond");
    buffer.SetCursor(0, 5);

    // Act
    bool deleted = buffer.DeleteCharacterAt();

    // Assert
    deleted.ShouldBeTrue("Should return true");
    buffer.LineCount.ShouldBe(1, "Lines should be merged");
    buffer.Lines[0].ShouldBe("firstsecond", "Lines merged");

    await Task.CompletedTask;
  }

  public static async Task Should_not_delete_at_buffer_end()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    // Cursor is already at end after SetText

    // Act
    bool deleted = buffer.DeleteCharacterAt();

    // Assert
    deleted.ShouldBeFalse("Should return false at buffer end");
    buffer.Lines[0].ShouldBe("hello", "Content unchanged");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Cursor Movement Tests
  // ============================================================================

  public static async Task Should_move_cursor_left()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");

    // Act
    bool moved = buffer.MoveCursorLeft();

    // Assert
    moved.ShouldBeTrue("Should return true");
    buffer.Cursor.Column.ShouldBe(4, "Cursor moved left");

    await Task.CompletedTask;
  }

  public static async Task Should_move_cursor_left_across_lines()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first\nsecond");
    buffer.SetCursor(1, 0);

    // Act
    bool moved = buffer.MoveCursorLeft();

    // Assert
    moved.ShouldBeTrue("Should return true");
    buffer.Cursor.Line.ShouldBe(0, "Cursor moved to previous line");
    buffer.Cursor.Column.ShouldBe(5, "Cursor at end of previous line");

    await Task.CompletedTask;
  }

  public static async Task Should_move_cursor_right()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    buffer.SetCursor(0, 0);

    // Act
    bool moved = buffer.MoveCursorRight();

    // Assert
    moved.ShouldBeTrue("Should return true");
    buffer.Cursor.Column.ShouldBe(1, "Cursor moved right");

    await Task.CompletedTask;
  }

  public static async Task Should_move_cursor_right_across_lines()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first\nsecond");
    buffer.SetCursor(0, 5);

    // Act
    bool moved = buffer.MoveCursorRight();

    // Assert
    moved.ShouldBeTrue("Should return true");
    buffer.Cursor.Line.ShouldBe(1, "Cursor moved to next line");
    buffer.Cursor.Column.ShouldBe(0, "Cursor at start of next line");

    await Task.CompletedTask;
  }

  public static async Task Should_not_move_left_at_buffer_start()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    buffer.SetCursor(0, 0);

    // Act
    bool moved = buffer.MoveCursorLeft();

    // Assert
    moved.ShouldBeFalse("Should return false at start");

    await Task.CompletedTask;
  }

  public static async Task Should_not_move_right_at_buffer_end()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    // Cursor is at end

    // Act
    bool moved = buffer.MoveCursorRight();

    // Assert
    moved.ShouldBeFalse("Should return false at end");

    await Task.CompletedTask;
  }

  // ============================================================================
  // GetFullText Tests
  // ============================================================================

  public static async Task Should_get_full_text_with_newlines()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("line one");
    buffer.AddLine();
    buffer.InsertText("line two");

    // Act
    string fullText = buffer.GetFullText();

    // Assert - joined with '\n' on EVERY platform: the cursor-mapping math counts one
    // char per line break, so GetFullText() must too. Environment.NewLine here broke
    // cursor positions on Windows (kanban 454-007).
    fullText.ShouldBe("line one\nline two", "Full text should join lines with a single \\n");

    await Task.CompletedTask;
  }

  public static async Task Should_keep_linear_positions_consistent_with_full_text()
  {
    // Regression for kanban 454-007: TotalLength, CursorToPosition, and GetFullText()
    // must agree so linear positions index into the text on every platform.

    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first\nsecond\nthird");
    string fullText = buffer.GetFullText();

    // Assert - lengths agree
    buffer.TotalLength.ShouldBe(fullText.Length, "TotalLength must equal GetFullText().Length");

    // Assert - cursor at start of each line maps to an index whose char begins that line
    for (int line = 0; line < buffer.LineCount; line++)
    {
      int position = buffer.CursorToPosition(new MultilineCursor(line, 0));
      if (buffer.Lines[line].Length > 0)
      {
        fullText[position].ShouldBe(buffer.Lines[line][0], $"Position of line {line} start must index its first char");
      }

      // Assert - round-trip
      MultilineCursor roundTripped = buffer.PositionToCursor(position);
      roundTripped.ShouldBe(new MultilineCursor(line, 0), "PositionToCursor must invert CursorToPosition");
    }

    // Assert - slicing at an interior cursor does not throw and splits correctly
    int mid = buffer.CursorToPosition(new MultilineCursor(1, 3));
    fullText[..mid].ShouldBe("first\nsec", "Prefix slice at (1,3) must end mid-second-line");

    await Task.CompletedTask;
  }

  public static async Task Should_get_full_text_with_custom_separator()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("a\nb\nc");

    // Act
    string fullText = buffer.GetFullText(" ");

    // Assert
    fullText.ShouldBe("a b c", "Should use custom separator");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Clear Tests
  // ============================================================================

  public static async Task Should_clear_buffer()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("line one\nline two\nline three");

    // Act
    buffer.Clear();

    // Assert
    buffer.LineCount.ShouldBe(1, "Should have one empty line");
    buffer.Lines[0].ShouldBe("", "Line should be empty");
    buffer.Cursor.Line.ShouldBe(0, "Cursor at line 0");
    buffer.Cursor.Column.ShouldBe(0, "Cursor at column 0");
    buffer.IsMultiline.ShouldBeFalse("Should not be multiline");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Position Conversion Tests
  // ============================================================================

  public static async Task Should_convert_position_to_cursor()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("abc\ndefgh\nij");

    // Act & Assert - position 0 is start of first line
    MultilineCursor c0 = buffer.PositionToCursor(0);
    c0.Line.ShouldBe(0);
    c0.Column.ShouldBe(0);

    // Position 3 is end of first line
    MultilineCursor c3 = buffer.PositionToCursor(3);
    c3.Line.ShouldBe(0);
    c3.Column.ShouldBe(3);

    // Position 4 is start of second line (after newline)
    MultilineCursor c4 = buffer.PositionToCursor(4);
    c4.Line.ShouldBe(1);
    c4.Column.ShouldBe(0);

    // Position 9 is end of second line
    MultilineCursor c9 = buffer.PositionToCursor(9);
    c9.Line.ShouldBe(1);
    c9.Column.ShouldBe(5);

    await Task.CompletedTask;
  }

  public static async Task Should_convert_cursor_to_position()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("abc\ndefgh\nij");

    // Act & Assert
    buffer.CursorToPosition(new MultilineCursor(0, 0)).ShouldBe(0);
    buffer.CursorToPosition(new MultilineCursor(0, 3)).ShouldBe(3);
    buffer.CursorToPosition(new MultilineCursor(1, 0)).ShouldBe(4);
    buffer.CursorToPosition(new MultilineCursor(1, 5)).ShouldBe(9);
    buffer.CursorToPosition(new MultilineCursor(2, 0)).ShouldBe(10);

    await Task.CompletedTask;
  }

  // ============================================================================
  // TotalLength Tests
  // ============================================================================

  public static async Task Should_calculate_total_length()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("abc\nde\nf"); // 3 + 1 + 2 + 1 + 1 = 8

    // Act
    int length = buffer.TotalLength;

    // Assert
    length.ShouldBe(8, "Total length should include newlines");

    await Task.CompletedTask;
  }

  public static async Task Should_return_zero_length_for_empty_buffer()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    int length = buffer.TotalLength;

    // Assert
    length.ShouldBe(0, "Empty buffer should have zero length");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.MultilineBufferTestsEditing
