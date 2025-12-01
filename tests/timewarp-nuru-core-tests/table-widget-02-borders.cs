#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Table widget border styles
return await RunTests<TableWidgetBorderTests>(clearCache: true);

[TestTag("Widgets")]
[ClearRunfileCache]
public class TableWidgetBorderTests
{
  public static async Task Should_render_table_with_rounded_border()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", "2");
    table.Border = BorderStyle.Rounded;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Structure: top(0) + header(1) + sep(2) + data(3) + bottom(4) = 5 lines
    lines.Length.ShouldBe(5);
    lines[0].ShouldContain("╭"); // rounded top-left
    lines[0].ShouldContain("╮"); // rounded top-right
    lines[0].ShouldContain("┬"); // T-junctions use square style (no rounded T exists)
    lines[4].ShouldContain("╰"); // rounded bottom-left
    lines[4].ShouldContain("╯"); // rounded bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_table_with_square_border()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", "2");
    table.Border = BorderStyle.Square;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Structure: top(0) + header(1) + sep(2) + data(3) + bottom(4) = 5 lines
    lines.Length.ShouldBe(5);
    lines[0].ShouldContain("┌"); // square top-left
    lines[0].ShouldContain("┐"); // square top-right
    lines[0].ShouldContain("┬"); // top T
    lines[2].ShouldContain("├"); // left T
    lines[2].ShouldContain("┼"); // cross
    lines[2].ShouldContain("┤"); // right T
    lines[4].ShouldContain("└"); // square bottom-left
    lines[4].ShouldContain("┘"); // square bottom-right
    lines[4].ShouldContain("┴"); // bottom T

    await Task.CompletedTask;
  }

  public static async Task Should_render_table_with_double_border()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", "2");
    table.Border = BorderStyle.Doubled;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Structure: top(0) + header(1) + sep(2) + data(3) + bottom(4) = 5 lines
    lines.Length.ShouldBe(5);
    lines[0].ShouldContain("╔"); // double top-left
    lines[0].ShouldContain("╗"); // double top-right
    lines[0].ShouldContain("═"); // double horizontal
    lines[0].ShouldContain("╦"); // double top T
    lines[1].ShouldContain("║"); // double vertical
    lines[2].ShouldContain("╠"); // double left T
    lines[2].ShouldContain("╬"); // double cross
    lines[2].ShouldContain("╣"); // double right T
    lines[4].ShouldContain("╚"); // double bottom-left
    lines[4].ShouldContain("╝"); // double bottom-right
    lines[4].ShouldContain("╩"); // double bottom T

    await Task.CompletedTask;
  }

  public static async Task Should_render_table_with_heavy_border()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", "2");
    table.Border = BorderStyle.Heavy;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Structure: top(0) + header(1) + sep(2) + data(3) + bottom(4) = 5 lines
    lines.Length.ShouldBe(5);
    lines[0].ShouldContain("┏"); // heavy top-left
    lines[0].ShouldContain("┓"); // heavy top-right
    lines[0].ShouldContain("━"); // heavy horizontal
    lines[0].ShouldContain("┳"); // heavy top T
    lines[1].ShouldContain("┃"); // heavy vertical
    lines[2].ShouldContain("┣"); // heavy left T
    lines[2].ShouldContain("╋"); // heavy cross
    lines[2].ShouldContain("┫"); // heavy right T
    lines[4].ShouldContain("┗"); // heavy bottom-left
    lines[4].ShouldContain("┛"); // heavy bottom-right
    lines[4].ShouldContain("┻"); // heavy bottom T

    await Task.CompletedTask;
  }

  public static async Task Should_render_table_without_border_when_style_is_none()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Value")
      .AddRow("Foo", "123")
      .AddRow("Bar", "456");
    table.Border = BorderStyle.None;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Should be just header + 2 data rows without any box characters
    lines.Length.ShouldBe(3);
    lines[0].ShouldContain("Name");
    lines[0].ShouldContain("Value");
    lines[0].ShouldNotContain("│");
    lines[0].ShouldNotContain("┌");
    lines[1].ShouldContain("Foo");
    lines[1].ShouldContain("123");
    lines[2].ShouldContain("Bar");
    lines[2].ShouldContain("456");

    await Task.CompletedTask;
  }

  public static async Task Should_render_table_with_border_color()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddRow("Test");
    table.BorderColor = AnsiColors.Cyan;

    // Act
    string[] lines = table.Render(40);

    // Assert
    // Border characters should be wrapped with color codes
    lines[0].ShouldContain(AnsiColors.Cyan);
    lines[0].ShouldContain(AnsiColors.Reset);
    lines[1].ShouldContain(AnsiColors.Cyan); // vertical border
    lines[4].ShouldContain(AnsiColors.Cyan); // bottom border

    await Task.CompletedTask;
  }
}
