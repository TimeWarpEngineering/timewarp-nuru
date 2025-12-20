#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Table widget with styled content (ANSI colors)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.TableWidgetStyling
{

[TestTag("Widgets")]
public class TableWidgetStylingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TableWidgetStylingTests>();

  public static async Task Should_render_styled_cell_content()
  {
    // Arrange
    string styledValue = $"{AnsiColors.Red}Error{AnsiColors.Reset}";
    Table table = new Table()
      .AddColumn("Status")
      .AddRow(styledValue);

    // Act
    string[] lines = table.Render(40);

    // Assert
    // The styled content should be preserved in the output
    lines[3].ShouldContain(AnsiColors.Red);
    lines[3].ShouldContain("Error");
    lines[3].ShouldContain(AnsiColors.Reset);

    await Task.CompletedTask;
  }

  public static async Task Should_calculate_column_width_correctly_with_ansi_codes()
  {
    // Arrange
    // "Error" is 5 visible characters, but with ANSI codes the string is longer
    string styledValue = $"{AnsiColors.Red}Error{AnsiColors.Reset}";
    Table table = new Table()
      .AddColumn("Status") // 6 chars
      .AddRow(styledValue); // 5 visible chars

    // Act
    string[] lines = table.Render(40);

    // Assert
    // The column should be sized for "Status" (6 chars) not the full ANSI string length
    // Header row should have proper alignment
    lines[1].ShouldContain("Status");

    // The visible width of the content row should match the header width
    // (both padded to 6 characters)
    string contentLine = TimeWarp.Terminal.AnsiStringUtils.StripAnsiCodes(lines[3]);
    contentLine.ShouldContain("Error ");

    await Task.CompletedTask;
  }

  public static async Task Should_render_header_with_column_header_color()
  {
    // Arrange
    TableColumn column = new("Important")
    {
      HeaderColor = AnsiColors.Yellow
    };
    Table table = new Table()
      .AddColumn(column)
      .AddRow("Value");

    // Act
    string[] lines = table.Render(40);

    // Assert
    lines[1].ShouldContain(AnsiColors.Yellow);
    lines[1].ShouldContain("Important");
    lines[1].ShouldContain(AnsiColors.Reset);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_styled_cells_in_same_row()
  {
    // Arrange
    string green = $"{AnsiColors.Green}OK{AnsiColors.Reset}";
    string red = $"{AnsiColors.Red}FAIL{AnsiColors.Reset}";
    Table table = new Table()
      .AddColumn("Test 1")
      .AddColumn("Test 2")
      .AddRow(green, red);

    // Act
    string[] lines = table.Render(40);

    // Assert
    string dataRow = lines[3];
    dataRow.ShouldContain(AnsiColors.Green);
    dataRow.ShouldContain("OK");
    dataRow.ShouldContain(AnsiColors.Red);
    dataRow.ShouldContain("FAIL");

    await Task.CompletedTask;
  }

  public static async Task Should_truncate_long_content_with_ellipsis()
  {
    // Arrange
    TableColumn column = new("Description")
    {
      MaxWidth = 10
    };
    Table table = new Table()
      .AddColumn(column)
      .AddRow("This is a very long description that should be truncated");

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Content should be truncated to 10 chars with "..." at end
    string dataRow = lines[3];
    dataRow.ShouldContain("...");
    // Should not contain the full text
    dataRow.ShouldNotContain("truncated");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_cells()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddColumn("C")
      .AddRow("1", "", "3");

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Should render without errors, empty cell should just be spaces
    lines[3].ShouldContain("1");
    lines[3].ShouldContain("3");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_fewer_cells_than_columns()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddColumn("C")
      .AddRow("1"); // Only one cell for three columns

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Should render without errors, missing cells should be empty
    lines[3].ShouldContain("1");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_null_cell_values()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", null!);

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Should render without errors
    lines[3].ShouldContain("1");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.TableWidgetStyling
