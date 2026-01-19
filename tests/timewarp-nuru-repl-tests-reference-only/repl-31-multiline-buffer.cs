#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Tests for MultilineBuffer data model (Task 043-009)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.MultilineBufferTests
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("Multiline")]
public class MultilineBufferTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultilineBufferTests>();

  // ============================================================================
  // Construction and Basic State Tests
  // ============================================================================

  public static async Task Should_initialize_with_empty_line()
  {
    // Arrange & Act
    MultilineBuffer buffer = new();

    // Assert
    buffer.LineCount.ShouldBe(1, "Should start with one empty line");
    buffer.Lines[0].ShouldBe("", "First line should be empty");
    buffer.Cursor.Line.ShouldBe(0, "Cursor should be on line 0");
    buffer.Cursor.Column.ShouldBe(0, "Cursor should be at column 0");
    buffer.IsMultiline.ShouldBeFalse("Should not be multiline initially");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_string_for_full_text_when_empty()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    string fullText = buffer.GetFullText();

    // Assert
    fullText.ShouldBe("", "Full text should be empty string");

    await Task.CompletedTask;
  }

  // ============================================================================
  // SetText Tests
  // ============================================================================

  public static async Task Should_set_single_line_text()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    buffer.SetText("hello world");

    // Assert
    buffer.LineCount.ShouldBe(1, "Should have one line");
    buffer.Lines[0].ShouldBe("hello world", "Line content should match");
    buffer.Cursor.Line.ShouldBe(0, "Cursor should be on line 0");
    buffer.Cursor.Column.ShouldBe(11, "Cursor should be at end of line");

    await Task.CompletedTask;
  }

  public static async Task Should_set_multiline_text()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    buffer.SetText("line one\nline two\nline three");

    // Assert
    buffer.LineCount.ShouldBe(3, "Should have three lines");
    buffer.Lines[0].ShouldBe("line one", "First line should match");
    buffer.Lines[1].ShouldBe("line two", "Second line should match");
    buffer.Lines[2].ShouldBe("line three", "Third line should match");
    buffer.IsMultiline.ShouldBeTrue("Should be multiline");
    buffer.Cursor.Line.ShouldBe(2, "Cursor should be on last line");
    buffer.Cursor.Column.ShouldBe(10, "Cursor should be at end of last line");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_windows_line_endings()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    buffer.SetText("line one\r\nline two");

    // Assert
    buffer.LineCount.ShouldBe(2, "Should handle CRLF");
    buffer.Lines[0].ShouldBe("line one", "First line should match");
    buffer.Lines[1].ShouldBe("line two", "Second line should match");

    await Task.CompletedTask;
  }

  // ============================================================================
  // InsertCharacter Tests
  // ============================================================================

  public static async Task Should_insert_character_at_cursor()
  {
    // Arrange
    MultilineBuffer buffer = new();

    // Act
    buffer.InsertCharacter('h');
    buffer.InsertCharacter('i');

    // Assert
    buffer.Lines[0].ShouldBe("hi", "Characters should be inserted");
    buffer.Cursor.Column.ShouldBe(2, "Cursor should advance");

    await Task.CompletedTask;
  }

  public static async Task Should_insert_character_in_middle_of_line()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hllo");
    buffer.SetCursor(0, 1);

    // Act
    buffer.InsertCharacter('e');

    // Assert
    buffer.Lines[0].ShouldBe("hello", "Character inserted in middle");
    buffer.Cursor.Column.ShouldBe(2, "Cursor should be after inserted char");

    await Task.CompletedTask;
  }

  // ============================================================================
  // AddLine Tests
  // ============================================================================

  public static async Task Should_add_new_line_at_end()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("first line");

    // Act
    buffer.AddLine();

    // Assert
    buffer.LineCount.ShouldBe(2, "Should have two lines");
    buffer.Lines[0].ShouldBe("first line", "First line unchanged");
    buffer.Lines[1].ShouldBe("", "Second line is empty");
    buffer.Cursor.Line.ShouldBe(1, "Cursor on new line");
    buffer.Cursor.Column.ShouldBe(0, "Cursor at start of new line");

    await Task.CompletedTask;
  }

  public static async Task Should_split_line_at_cursor()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("hello world");
    buffer.SetCursor(0, 5);

    // Act
    buffer.AddLine();

    // Assert
    buffer.LineCount.ShouldBe(2, "Should have two lines");
    buffer.Lines[0].ShouldBe("hello", "First line is before cursor");
    buffer.Lines[1].ShouldBe(" world", "Second line is after cursor");
    buffer.Cursor.Line.ShouldBe(1, "Cursor on new line");
    buffer.Cursor.Column.ShouldBe(0, "Cursor at start of new line");

    await Task.CompletedTask;
  }

  public static async Task Should_add_line_at_start()
  {
    // Arrange
    MultilineBuffer buffer = new();
    buffer.SetText("existing line");
    buffer.SetCursor(0, 0);

    // Act
    buffer.AddLine();

    // Assert
    buffer.LineCount.ShouldBe(2, "Should have two lines");
    buffer.Lines[0].ShouldBe("", "First line is empty");
    buffer.Lines[1].ShouldBe("existing line", "Original content on second line");

    await Task.CompletedTask;
  }

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

    // Assert
    fullText.ShouldBe($"line one{System.Environment.NewLine}line two", "Full text should include newlines");

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

} // namespace TimeWarp.Nuru.Tests.ReplTests.MultilineBufferTests
