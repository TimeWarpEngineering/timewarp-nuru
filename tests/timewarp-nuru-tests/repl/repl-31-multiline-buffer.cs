#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins the MultilineBuffer data model used by the REPL line editor (task 043-009).
#endregion

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
  // InsertText Tests (kanban 470-003 / M36)
  // ============================================================================

  public static async Task Should_insert_single_line_text_at_cursor()
  {
    MultilineBuffer buffer = new();
    buffer.SetText("hello");
    buffer.SetCursor(0, 5);

    buffer.InsertText(" world");

    buffer.LineCount.ShouldBe(1);
    buffer.Lines[0].ShouldBe("hello world");
    buffer.Cursor.Line.ShouldBe(0);
    buffer.Cursor.Column.ShouldBe(11);

    await Task.CompletedTask;
  }

  public static async Task Should_insert_lf_multiline_without_blank_line()
  {
    MultilineBuffer buffer = new();

    buffer.InsertText("line one\nline two");

    buffer.LineCount.ShouldBe(2, "LF is one break");
    buffer.Lines[0].ShouldBe("line one");
    buffer.Lines[1].ShouldBe("line two");
    buffer.Cursor.Line.ShouldBe(1);
    buffer.Cursor.Column.ShouldBe(8);

    await Task.CompletedTask;
  }

  public static async Task Should_insert_crlf_as_one_break_not_blank_line()
  {
    // Old InsertText treated every \r and every \n as AddLine(), so \r\n made a blank line.
    MultilineBuffer buffer = new();

    buffer.InsertText("line one\r\nline two");

    buffer.LineCount.ShouldBe(2, "CRLF must split like SetText, not insert a blank line");
    buffer.Lines[0].ShouldBe("line one");
    buffer.Lines[1].ShouldBe("line two");
    buffer.IsMultiline.ShouldBeTrue();
    buffer.GetFullText().ShouldBe("line one\nline two");
    buffer.Cursor.Line.ShouldBe(1);
    buffer.Cursor.Column.ShouldBe(8);
    buffer.CursorToPosition(buffer.Cursor).ShouldBe(buffer.TotalLength);
    buffer.TotalLength.ShouldBe(buffer.GetFullText().Length);

    await Task.CompletedTask;
  }

  public static async Task Should_insert_bare_cr_as_one_break()
  {
    MultilineBuffer buffer = new();

    buffer.InsertText("line one\rline two");

    buffer.LineCount.ShouldBe(2, "Bare CR is one break");
    buffer.Lines[0].ShouldBe("line one");
    buffer.Lines[1].ShouldBe("line two");
    buffer.GetFullText().ShouldBe("line one\nline two");

    await Task.CompletedTask;
  }

  public static async Task Should_insert_crlf_in_middle_of_line_keeping_remainder()
  {
    MultilineBuffer buffer = new();
    buffer.SetText("hello world");
    buffer.SetCursor(0, 5);

    buffer.InsertText("A\r\nB");

    buffer.LineCount.ShouldBe(2);
    buffer.Lines[0].ShouldBe("helloA");
    buffer.Lines[1].ShouldBe("B world");
    buffer.Cursor.Line.ShouldBe(1);
    buffer.Cursor.Column.ShouldBe(1);
    buffer.GetFullText().ShouldBe("helloA\nB world");

    await Task.CompletedTask;
  }

  public static async Task Should_insert_trailing_crlf_without_extra_blank_line()
  {
    MultilineBuffer buffer = new();

    buffer.InsertText("line\r\n");

    buffer.LineCount.ShouldBe(2, "Trailing CRLF adds one empty line, not two");
    buffer.Lines[0].ShouldBe("line");
    buffer.Lines[1].ShouldBe("");
    buffer.Cursor.Line.ShouldBe(1);
    buffer.Cursor.Column.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_keep_linear_domain_in_sync_after_crlf_insert()
  {
    MultilineBuffer buffer = new();
    buffer.SetText("prefix");
    buffer.SetCursor(0, 6);

    buffer.InsertText("a\r\nb\r\nc");

    string fullText = buffer.GetFullText();
    fullText.ShouldBe("prefixa\nb\nc");
    buffer.TotalLength.ShouldBe(fullText.Length);
    buffer.CursorToPosition(buffer.Cursor).ShouldBe(fullText.Length, "Cursor must sit at end of inserted text in the \\n domain");
    fullText.Contains('\r').ShouldBeFalse("GetFullText must not retain CR after CRLF insert");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.MultilineBufferTests
