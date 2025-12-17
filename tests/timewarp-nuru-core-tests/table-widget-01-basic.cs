#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Table widget basic functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.TableWidget
{

[TestTag("Widgets")]
public class TableWidgetBasicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TableWidgetBasicTests>();

  public static async Task Should_render_basic_table_with_two_columns_and_two_rows()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Value")
      .AddRow("Foo", "123")
      .AddRow("Bar", "456");

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Expected:
    // ┌──────┬───────┐
    // │ Name │ Value │
    // ├──────┼───────┤
    // │ Foo  │ 123   │
    // │ Bar  │ 456   │
    // └──────┴───────┘
    lines.Length.ShouldBe(6); // top + header + separator + 2 data rows + bottom
    lines[0].ShouldContain("┌"); // top-left
    lines[0].ShouldContain("┬"); // top T-junction
    lines[0].ShouldContain("┐"); // top-right
    lines[1].ShouldContain("Name");
    lines[1].ShouldContain("Value");
    lines[2].ShouldContain("├"); // left T
    lines[2].ShouldContain("┼"); // cross
    lines[2].ShouldContain("┤"); // right T
    lines[3].ShouldContain("Foo");
    lines[3].ShouldContain("123");
    lines[4].ShouldContain("Bar");
    lines[4].ShouldContain("456");
    lines[5].ShouldContain("└"); // bottom-left
    lines[5].ShouldContain("┴"); // bottom T
    lines[5].ShouldContain("┘"); // bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_right_aligned_column()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Item")
      .AddColumn("Count", Alignment.Right)
      .AddRow("Apples", "5")
      .AddRow("Oranges", "123");

    // Act
    string[] lines = table.Render(40);

    // Assert
    // The "Count" column should have right-aligned values
    // Column width is determined by "Count" header (5 chars)
    // "5" should be padded on the left to match "Count" width
    lines[3].ShouldContain("    5"); // 5 should be right-aligned (4 spaces + "5")
    lines[4].ShouldContain("  123"); // 123 right-aligned (2 spaces + "123")

    await Task.CompletedTask;
  }

  public static async Task Should_render_center_aligned_column()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Status", Alignment.Center)
      .AddRow("OK")
      .AddRow("Error");

    // Act
    string[] lines = table.Render(40);

    // Assert
    // "Status" header and "OK" should be centered within the column
    lines[1].ShouldContain("Status");
    lines[3].ShouldContain("OK");

    await Task.CompletedTask;
  }

  public static async Task Should_render_multi_column_table()
  {
    // Arrange
    Table table = new Table()
      .AddColumns("A", "B", "C", "D", "E")
      .AddRow("1", "2", "3", "4", "5");

    // Act
    string[] lines = table.Render(80);

    // Assert
    lines[0].ShouldContain("┬"); // Should have T-junctions for all column separators
    lines[1].ShouldContain("A");
    lines[1].ShouldContain("E");
    lines[3].ShouldContain("1");
    lines[3].ShouldContain("5");

    await Task.CompletedTask;
  }

  public static async Task Should_render_headerless_table()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Col1")
      .AddColumn("Col2")
      .AddRow("A", "B")
      .AddRow("C", "D");
    table.ShowHeaders = false;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // No header row or separator, just top + data rows + bottom
    lines.Length.ShouldBe(4); // top + 2 data rows + bottom
    lines[0].ShouldContain("┌"); // top border
    lines[1].ShouldContain("A"); // first data row
    lines[1].ShouldContain("B");
    lines[2].ShouldContain("C"); // second data row
    lines[3].ShouldContain("└"); // bottom border

    await Task.CompletedTask;
  }

  public static async Task Should_render_row_separators()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddRow("Alice")
      .AddRow("Bob")
      .AddRow("Carol");
    table.ShowRowSeparators = true;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // top + header + header-sep + data1 + sep + data2 + sep + data3 + bottom = 9
    // Actually: top + header + header-sep + data1 + sep + data2 + sep + data3 + bottom
    // But separators are only between data rows, not after last
    // top(1) + header(1) + header-sep(1) + data1(1) + sep(1) + data2(1) + sep(1) + data3(1) + bottom(1) = 9
    lines.Length.ShouldBe(9);

    await Task.CompletedTask;
  }

  public static async Task Should_render_empty_table_with_headers_only()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Age");
    // No rows added

    // Act
    string[] lines = table.Render(40);

    // Assert
    // top + header + header-sep + bottom = 4
    lines.Length.ShouldBe(4);
    lines[1].ShouldContain("Name");
    lines[1].ShouldContain("Age");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_array_when_no_columns()
  {
    // Arrange
    Table table = new();

    // Act
    string[] lines = table.Render(40);

    // Assert
    lines.Length.ShouldBe(0);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.TableWidget
