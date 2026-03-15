#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for --capabilities --search functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesSearch
{

[TestTag("Capabilities")]
public class CapabilitiesSearchTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesSearchTests>();

  public static async Task Should_show_error_when_nuru_search_not_installed()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").Done()
      .Build();

    await app.RunAsync(["--capabilities", "--search", "greet"]);

    terminal.ErrorOutput.Contains("Search requires timewarp-nuru-search to be installed").ShouldBeTrue("Should show install message");
    terminal.ErrorOutput.Contains("dotnet tool install --global TimeWarp.Nuru.Search").ShouldBeTrue("Should show install command");

    await Task.CompletedTask;
  }

  public static async Task Should_support_short_form_flag()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").Done()
      .Build();

    await app.RunAsync(["--capabilities", "-s", "greet"]);

    terminal.ErrorOutput.Contains("Search requires timewarp-nuru-search to be installed").ShouldBeTrue("Should show install message");

    await Task.CompletedTask;
  }

  public static async Task Should_support_combined_search_and_group_filter()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    await app.RunAsync(["--capabilities", "--search", "list", "--group-filter", "kanban"]);

    terminal.ErrorOutput.Contains("Search requires timewarp-nuru-search to be installed").ShouldBeTrue("Should show install message");

    await Task.CompletedTask;
  }

  public static async Task Should_support_alternate_flag_order()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    await app.RunAsync(["--capabilities", "--group-filter", "kanban", "--search", "list"]);

    terminal.ErrorOutput.Contains("Search requires timewarp-nuru-search to be installed").ShouldBeTrue("Should show install message");

    await Task.CompletedTask;
  }

  public static async Task Should_support_short_form_group_filter_with_search()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("kanban")
        .Map("list").WithHandler(() => "kanban list").Done()
        .Done()
      .Build();

    await app.RunAsync(["--capabilities", "-s", "list", "-g", "kanban"]);

    terminal.ErrorOutput.Contains("Search requires timewarp-nuru-search to be installed").ShouldBeTrue("Should show install message");

    await Task.CompletedTask;
  }

  public static async Task Should_return_error_code_when_nuru_search_not_found()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"Hello, {name}!").Done()
      .Build();

    int exitCode = await app.RunAsync(["--capabilities", "--search", "greet"]);

    exitCode.ShouldBe(1, "Should return error code 1 when nuru-search not installed");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesSearch
