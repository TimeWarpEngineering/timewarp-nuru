#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins Shift+Enter (AddLine) on all key binding profiles and profile factory/resolution by name; split from repl-23-key-binding-profiles.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingProfilesShiftEnter
{

[TestTag("REPL")]
public class KeyBindingProfileShiftEnterTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KeyBindingProfileShiftEnterTests>();

  // ============================================================================
  // Shift+Enter (AddLine) on all profiles — M18 / 470-013
  // ============================================================================

  public static async Task Emacs_profile_should_support_shift_enter_add_line()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("line1 line2")
        .WithHandler(ShiftEnterEmacs)
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Emacs";
        options.EnableColors = false;
        options.WelcomeMessage = null;
        options.PersistHistory = false;
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("SHIFT-ENTER-Emacs")
      .ShouldBeTrue("Emacs profile must bind Shift+Enter to AddLine (not swallow the chord)");
  }

  public static async Task Vi_profile_should_support_shift_enter_add_line()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("line1 line2")
        .WithHandler(ShiftEnterVi)
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Vi";
        options.EnableColors = false;
        options.WelcomeMessage = null;
        options.PersistHistory = false;
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("SHIFT-ENTER-Vi")
      .ShouldBeTrue("Vi profile must bind Shift+Enter to AddLine (not swallow the chord)");
  }

  public static async Task VSCode_profile_should_support_shift_enter_add_line()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("line1 line2")
        .WithHandler(ShiftEnterVSCode)
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "VSCode";
        options.EnableColors = false;
        options.WelcomeMessage = null;
        options.PersistHistory = false;
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("SHIFT-ENTER-VSCode")
      .ShouldBeTrue("VSCode profile must bind Shift+Enter to AddLine (not swallow the chord)");
  }

  public static async Task Default_profile_should_support_shift_enter_add_line()
  {
    using TestTerminal terminal = new();
    terminal.QueueKeys("line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("line1 line2")
        .WithHandler(ShiftEnterDefault)
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Default";
        options.EnableColors = false;
        options.WelcomeMessage = null;
        options.PersistHistory = false;
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("SHIFT-ENTER-Default")
      .ShouldBeTrue("Default profile must bind Shift+Enter to AddLine (not swallow the chord)");
  }

  // internal (not private) so the source-generated interceptor can reference the method group.
  internal static string ShiftEnterEmacs() => "SHIFT-ENTER-Emacs";
  internal static string ShiftEnterVi() => "SHIFT-ENTER-Vi";
  internal static string ShiftEnterVSCode() => "SHIFT-ENTER-VSCode";
  internal static string ShiftEnterDefault() => "SHIFT-ENTER-Default";

  // ============================================================================
  // Profile Factory/Resolution Tests
  // ============================================================================

  public static void Should_resolve_default_profile_by_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Default";
      })
      .Build();

    // Assert - if we get here without exception, profile resolved correctly
    app.ShouldNotBeNull("App should build with Default profile");
  }

  public static void Should_resolve_emacs_profile_by_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Emacs";
      })
      .Build();

    // Assert
    app.ShouldNotBeNull("App should build with Emacs profile");
  }

  public static void Should_resolve_vi_profile_by_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Vi";
      })
      .Build();

    // Assert
    app.ShouldNotBeNull("App should build with Vi profile");
  }

  public static void Should_resolve_vscode_profile_by_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "VSCode";
      })
      .Build();

    // Assert
    app.ShouldNotBeNull("App should build with VSCode profile");
  }

  public static void Should_throw_on_unknown_profile_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    // Act & Assert
    Should.Throw<ArgumentException>(() =>
    {
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddRepl(options =>
        {
          options.KeyBindingProfileName = "InvalidProfile";
        })
        .Build();
    }, "Unknown profile name should throw ArgumentException");
  }

  public static void Should_default_to_default_profile_when_not_specified()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl() // No profile specified
      .Build();

    // Assert - Default profile is used implicitly
    app.ShouldNotBeNull("App should build with implicit Default profile");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingProfilesShiftEnter
