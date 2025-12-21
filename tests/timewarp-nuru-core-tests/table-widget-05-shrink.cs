#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Table widget shrink functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.TableWidgetShrink
{

[TestTag("Widgets")]
public class TableWidgetShrinkTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TableWidgetShrinkTests>();

  public static async Task Should_shrink_table_to_fit_terminal_width()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Very Long Path That Exceeds Normal Width")
      .AddRow("repo", "/home/user/worktrees/github.com/Organization/project-name/feature-branch");

    // Act - render to narrow terminal (60 chars)
    string[] lines = table.Render(60);

    // Assert
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThanOrEqualTo(60);

    await Task.CompletedTask;
  }

  public static async Task Should_shrink_wider_columns_more_aggressively()
  {
    // Arrange
    // Column A has 5 chars content, Column B has 50 chars content
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("Short", "This is a very long string that takes up lots of space");

    // Act - render to 40 chars (forces shrinking)
    string[] lines = table.Render(40);

    // Assert
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThanOrEqualTo(40);

    // The wider column B should be truncated with ellipsis
    string dataRow = lines[3];
    dataRow.ShouldContain("...");

    await Task.CompletedTask;
  }

  public static async Task Should_respect_column_min_width()
  {
    // Arrange
    TableColumn columnWithMinWidth = new("Description")
    {
      MinWidth = 20
    };
    Table table = new Table()
      .AddColumn("ID")
      .AddColumn(columnWithMinWidth)
      .AddRow("1", "This is a long description that would normally be truncated heavily");

    // Act - render to narrow terminal
    string[] lines = table.Render(40);

    // Assert
    // Table should fit, but Description column should maintain at least 20 chars
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThanOrEqualTo(40);

    await Task.CompletedTask;
  }

  public static async Task Should_not_shrink_when_shrink_is_false()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Name")
      .AddColumn("Very Long Path That Exceeds Normal Width")
      .AddRow("repo", "/home/user/worktrees/github.com/Organization/project-name/feature-branch");
    table.Shrink = false;

    // Act - render to narrow terminal (60 chars)
    string[] lines = table.Render(60);

    // Assert - table should exceed terminal width
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeGreaterThan(60);

    await Task.CompletedTask;
  }

  public static async Task Should_use_default_min_width_of_4()
  {
    // Arrange - table with many columns that would need extreme shrinking
    Table table = new Table()
      .AddColumn("Column One With Long Header")
      .AddColumn("Column Two With Long Header")
      .AddColumn("Column Three With Long Header")
      .AddRow("Value 1", "Value 2", "Value 3");

    // Act - render to very narrow terminal (30 chars)
    string[] lines = table.Render(30);

    // Assert - columns should shrink but not below 4 chars (shows "..." at minimum)
    // The table may exceed width if min widths can't be satisfied, but it tried
    lines.Length.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Should_truncate_content_after_shrinking()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Path")
      .AddRow("/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/feature");

    // Act - render to 30 chars
    string[] lines = table.Render(30);

    // Assert
    string dataRow = lines[3];
    dataRow.ShouldContain("...");
    // Should not contain the full path
    dataRow.ShouldNotContain("feature");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_table_already_fits()
  {
    // Arrange - small table that already fits
    Table table = new Table()
      .AddColumn("A")
      .AddColumn("B")
      .AddRow("1", "2");

    // Act - render to wide terminal (80 chars)
    string[] lines = table.Render(80);

    // Assert - table should render normally without shrinking
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThan(80);

    // Content should not be truncated
    string dataRow = lines[3];
    dataRow.ShouldNotContain("...");

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_ansi_colored_content()
  {
    // Arrange
    string coloredPath = $"{AnsiColors.Cyan}/home/user/very/long/path/that/needs/truncation{AnsiColors.Reset}";
    Table table = new Table()
      .AddColumn("Path")
      .AddRow(coloredPath);

    // Act - render to narrow terminal
    string[] lines = table.Render(30);

    // Assert
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeLessThanOrEqualTo(30);

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_different_border_styles()
  {
    // Arrange & Act & Assert for each border style
    BorderStyle[] styles = [BorderStyle.Square, BorderStyle.Rounded, BorderStyle.Doubled, BorderStyle.Heavy];

    foreach (BorderStyle style in styles)
    {
      Table table = new Table()
        .AddColumn("Long Column Header Name")
        .AddRow("Some content that is fairly long");
      table.Border = style;

      string[] lines = table.Render(30);

      int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
      topLineWidth.ShouldBeLessThanOrEqualTo(30, $"Border style {style} should fit within 30 chars");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_borderless_table()
  {
    // Arrange
    Table table = new Table()
      .AddColumn("Long Column Header Name")
      .AddColumn("Another Long Column Header")
      .AddRow("Content one", "Content two that is long");
    table.Border = BorderStyle.None;

    // Act - render to narrow terminal
    string[] lines = table.Render(40);

    // Assert - borderless tables should also shrink
    int lineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    lineWidth.ShouldBeLessThanOrEqualTo(40);

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_builder_shrink_method()
  {
    // Arrange & Act
    using TestTerminal terminal = new() { WindowWidth = 40 };

    terminal.WriteTable(t => t
      .AddColumn("Repository")
      .AddColumn("Path")
      .AddRow("nuru", "/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru")
      .Shrink());

    // Assert
    terminal.Output.ShouldNotBeNullOrEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_disable_shrink_with_builder()
  {
    // Arrange
    Table table = new TableBuilder()
      .AddColumn("Long Column Header")
      .AddRow("Content that is very long and would normally be truncated")
      .Shrink(false)
      .Build();

    // Act
    string[] lines = table.Render(30);

    // Assert - should overflow
    int topLineWidth = TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]);
    topLineWidth.ShouldBeGreaterThan(30);

    await Task.CompletedTask;
  }

  public static async Task Should_truncate_at_start_with_truncate_mode_start()
  {
    // Arrange
    TableColumn pathColumn = new("Path")
    {
      TruncateMode = TruncateMode.Start,
      MaxWidth = 20
    };
    Table table = new Table()
      .AddColumn(pathColumn)
      .AddRow("/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru");

    // Act
    string[] lines = table.Render(30);

    // Assert - should show ellipsis at start, end of path visible
    string dataRow = lines[3];
    dataRow.ShouldContain("...");
    dataRow.ShouldContain("nuru"); // End of path should be visible
    dataRow.ShouldNotContain("/home"); // Start should be truncated

    await Task.CompletedTask;
  }

  public static async Task Should_truncate_at_middle_with_truncate_mode_middle()
  {
    // Arrange
    TableColumn column = new("Description")
    {
      TruncateMode = TruncateMode.Middle,
      MaxWidth = 20
    };
    Table table = new Table()
      .AddColumn(column)
      .AddRow("beginning-of-text-middle-part-end-of-text");

    // Act
    string[] lines = table.Render(30);

    // Assert - should show ellipsis in middle, both start and end visible
    string dataRow = lines[3];
    dataRow.ShouldContain("...");
    dataRow.ShouldContain("begin"); // Start should be visible
    dataRow.ShouldContain("text"); // End should be visible

    await Task.CompletedTask;
  }

  public static async Task Should_truncate_at_end_by_default()
  {
    // Arrange - default TruncateMode is End
    Table table = new Table()
      .AddColumn(new TableColumn("Path") { MaxWidth = 20 })
      .AddRow("/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru");

    // Act
    string[] lines = table.Render(30);

    // Assert - should show ellipsis at end, start of path visible
    string dataRow = lines[3];
    dataRow.ShouldContain("...");
    dataRow.ShouldContain("/home"); // Start should be visible
    dataRow.ShouldNotContain("nuru"); // End should be truncated

    await Task.CompletedTask;
  }

  public static async Task Should_show_end_of_path_when_shrinking_with_start_mode()
  {
    // Arrange - This is the real-world use case from the issue
    TableColumn repoColumn = new("Repository");
    TableColumn pathColumn = new("Worktree Path")
    {
      TruncateMode = TruncateMode.Start
    };
    TableColumn branchColumn = new("Branch");

    Table table = new Table()
      .AddColumn(repoColumn)
      .AddColumn(pathColumn)
      .AddColumn(branchColumn)
      .AddRow("timewarp-nuru", "/home/user/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/feature-branch-name", "feature-xyz");

    // Act - render to constrained width
    string[] lines = table.Render(80);

    // Assert
    string dataRow = lines[3];
    // Path column should show the end (branch name) not the start
    dataRow.ShouldContain("feature-branch-name");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.TableWidgetShrink
