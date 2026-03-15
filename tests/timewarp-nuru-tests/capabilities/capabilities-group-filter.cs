#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for --capabilities --group-filter functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesGroupFilter
{

[TestTag("Capabilities")]
public class CapabilitiesGroupFilterTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesGroupFilterTests>();

  public static async Task Should_filter_endpoints_by_single_segment_group_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Map("add").WithHandler(() => "kanban add").Done()
        .Done()
      .WithGroupPrefix("config")
        .Map("get").WithHandler(() => "config get").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities", "--group-filter", "kanban"]);

    // Assert
    terminal.OutputContains("kanban list").ShouldBeTrue("Should contain kanban list");
    terminal.OutputContains("kanban add").ShouldBeTrue("Should contain kanban add");
    terminal.OutputContains("config get").ShouldBeFalse("Should NOT contain config get");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_endpoints_by_multi_segment_group_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get").WithHandler(() => "admin config get").Done()
          .Map("set").WithHandler(() => "admin config set").Done()
          .Done()
        .WithGroupPrefix("users")
          .Map("list").WithHandler(() => "admin users list").Done()
          .Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities", "--group-filter", "admin.config"]);

    // Assert
    terminal.OutputContains("admin config get").ShouldBeTrue("Should contain admin config get");
    terminal.OutputContains("admin config set").ShouldBeTrue("Should contain admin config set");
    terminal.OutputContains("admin users list").ShouldBeFalse("Should NOT contain admin users list");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_endpoints_when_no_match()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities", "--group-filter", "nonexistent"]);

    // Assert
    terminal.OutputContains("\"endpoints\": []").ShouldBeTrue("Should have empty endpoints array");
    terminal.OutputContains("kanban list").ShouldBeFalse("Should NOT contain kanban list");

    await Task.CompletedTask;
  }

  public static async Task Should_include_filter_metadata_in_output()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities", "--group-filter", "kanban"]);

    // Assert
    terminal.OutputContains("\"filter\":").ShouldBeTrue("Should contain filter field");
    terminal.OutputContains("\"group\": \"kanban\"").ShouldBeTrue("Should contain group value in filter");

    await Task.CompletedTask;
  }

  public static async Task Should_not_include_filter_metadata_when_no_filter()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"filter\":").ShouldBeFalse("Should NOT contain filter field when no filter applied");

    await Task.CompletedTask;
  }

  public static async Task Should_support_short_form_flag()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities", "-g", "kanban"]);

    // Assert
    terminal.OutputContains("kanban list").ShouldBeTrue("Should contain kanban list");
    terminal.OutputContains("\"group\": \"kanban\"").ShouldBeTrue("Should contain group value in filter");

    await Task.CompletedTask;
  }

  public static async Task Should_match_group_path_as_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .Map("status").WithHandler(() => "admin status").Done()
        .Done()
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get").WithHandler(() => "admin config get").Done()
          .Done()
        .Done()
      .Build();

    // Act - filter for "admin" prefix (matches both "admin" and "admin.config")
    await app.RunAsync(["--capabilities", "--group-filter", "admin"]);

    // Assert - prefix match includes both admin and admin.config groups
    terminal.OutputContains("admin status").ShouldBeTrue("Should contain admin status");
    terminal.OutputContains("admin config get").ShouldBeTrue("Should contain admin config get (prefix match)");

    await Task.CompletedTask;
  }

  public static async Task Should_match_partial_group_path_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("admin")
        .WithGroupPrefix("config")
          .Map("get").WithHandler(() => "admin config get").Done()
          .Map("set").WithHandler(() => "admin config set").Done()
          .Done()
        .WithGroupPrefix("users")
          .Map("list").WithHandler(() => "admin users list").Done()
          .Done()
        .Done()
      .Build();

    // Act - filter for "admin" should match all admin.* groups
    await app.RunAsync(["--capabilities", "--group-filter", "admin"]);

    // Assert - should match all endpoints under admin
    terminal.OutputContains("admin config get").ShouldBeTrue("Should contain admin config get");
    terminal.OutputContains("admin config set").ShouldBeTrue("Should contain admin config set");
    terminal.OutputContains("admin users list").ShouldBeTrue("Should contain admin users list");

    await Task.CompletedTask;
  }

  public static async Task Should_match_case_insensitively()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("Kanban")
        .Map("list").WithHandler(() => "Kanban list").Done()
        .Done()
      .Build();

    // Act - use lowercase filter for mixed-case group
    await app.RunAsync(["--capabilities", "--group-filter", "kanban"]);

    // Assert - should match case-insensitively
    terminal.OutputContains("Kanban list").ShouldBeTrue("Should contain Kanban list (case-insensitive match)");

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_case_filter()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    // Act - use mixed-case filter for lowercase group
    await app.RunAsync(["--capabilities", "--group-filter", "KANBAN"]);

    // Assert - should match case-insensitively
    terminal.OutputContains("kanban list").ShouldBeTrue("Should contain kanban list (case-insensitive match)");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesGroupFilter
