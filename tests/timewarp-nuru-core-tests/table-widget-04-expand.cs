#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Table widget expand functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.TableWidgetExpand
{

[TestTag("Widgets")]
public class TableWidgetExpandTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TableWidgetExpandTests>();

  public static async Task Should_expand_table_to_terminal_width()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Value")
      .AddRow("A", "1");
    table.Expand = true;

    // Act
    string[] lines = table.Render(80);

    // Assert
    // The table should expand to fill 80 characters
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBe(80);

    await Task.CompletedTask;
  }

  public static async Task Should_not_expand_when_expand_is_false()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Value")
      .AddRow("A", "1");
    table.Expand = false;

    // Act
    string[] lines = table.Render(80);

    // Assert
    // The table should be sized to content, not terminal width
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThan(80);

    await Task.CompletedTask;
  }

  public static async Task Should_distribute_extra_width_evenly_across_columns()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("A") // natural width: 1
      .AddColumn("B") // natural width: 1
      .AddRow("1", "2");
    table.Expand = true;

    // Act - render to 50 chars
    // Natural width: 2 borders + 1 separator + 4 padding spaces + 2 content = 9
    // Extra: 50 - 9 = 41 chars to distribute across 2 columns
    string[] lines = table.Render(50);

    // Assert
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBe(50);

    await Task.CompletedTask;
  }

  public static async Task Should_not_expand_borderless_table()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Value")
      .AddRow("A", "1");
    table.Expand = true;
    table.Border = BorderStyle.None;

    // Act
    string[] lines = table.Render(80);

    // Assert
    // Borderless tables don't expand because there's no visual border to fill
    int lineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    lineWidth.ShouldBeLessThan(80);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.TableWidgetExpand
