#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingProfiles
{

/// <summary>
/// Tests for key binding profiles (Default, Emacs, Vi, VSCode).
/// </summary>
/// <remarks>
/// <para><strong>IMPLEMENTATION STATUS:</strong></para>
/// <list type="bullet">
/// <item><description>✅ DefaultKeyBindingProfile: COMPLETE - All handler methods exist</description></item>
/// <item><description>✅ EmacsKeyBindingProfile: COMPLETE - All handlers including Ctrl+K, Ctrl+U</description></item>
/// <item><description>✅ ViKeyBindingProfile: COMPLETE - All handlers including Ctrl+W, Ctrl+U, Ctrl+K</description></item>
/// <item><description>✅ VSCodeKeyBindingProfile: COMPLETE - All handlers including Ctrl+K, Ctrl+Backspace</description></item>
/// </list>
///
/// <para>
/// These tests focus on profile instantiation, resolution, and basic smoke tests.
/// Detailed key binding tests are in repl-18-psreadline-keybindings.cs.
/// </para>
/// </remarks>
[TestTag("REPL")]
public class KeyBindingProfileTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<KeyBindingProfileTests>();

  // ============================================================================
  // Profile Instantiation Tests
  // ============================================================================

  public static void Should_instantiate_default_profile()
  {
    // Act
    DefaultKeyBindingProfile profile = new();

    // Assert
    profile.ShouldNotBeNull("Profile should be instantiated");
    profile.Name.ShouldBe("Default", "Profile name should be 'Default'");
  }

  public static void Should_instantiate_emacs_profile()
  {
    // Act
    EmacsKeyBindingProfile profile = new();

    // Assert
    profile.ShouldNotBeNull("Profile should be instantiated");
    profile.Name.ShouldBe("Emacs", "Profile name should be 'Emacs'");
  }

  public static void Should_instantiate_vi_profile()
  {
    // Act
    ViKeyBindingProfile profile = new();

    // Assert
    profile.ShouldNotBeNull("Profile should be instantiated");
    profile.Name.ShouldBe("Vi", "Profile name should be 'Vi'");
  }

  public static void Should_instantiate_vscode_profile()
  {
    // Act
    VSCodeKeyBindingProfile profile = new();

    // Assert
    profile.ShouldNotBeNull("Profile should be instantiated");
    profile.Name.ShouldBe("VSCode", "Profile name should be 'VSCode'");
  }

  // ============================================================================
  // Default Profile Tests (COMPLETE - All handlers exist)
  // ============================================================================

  [Skip("Help output format not yet finalized - see task 069")]
  public static async Task Default_profile_should_execute_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("help");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Default";
        options.WelcomeMessage = null; // Suppress for cleaner output
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Commands:")
      .ShouldBeTrue("Default profile should execute commands");
  }

  public static async Task Default_profile_should_support_ctrl_a_for_line_start()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("world");
    terminal.QueueKey(ConsoleKey.A, ctrl: true); // Ctrl+A to go to start
    terminal.QueueKeys("hello ");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Default";
        options.WelcomeMessage = null;
      })
      .Map("hello {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Hello, world!")
      .ShouldBeTrue("Ctrl+A should move cursor to beginning");
  }

  public static async Task Default_profile_should_support_arrow_keys()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.LeftArrow);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Default";
        options.WelcomeMessage = null;
      })
      .Map("tet")
        .WithHandler(() => "Arrow keys work!")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Arrow keys work!")
      .ShouldBeTrue("Arrow keys should allow navigation");
  }

  // ============================================================================
  // Emacs Profile Tests (PARTIAL - Missing HandleKillLine)
  // ============================================================================

  [Skip("Help output format not yet finalized - see task 069")]
  public static async Task Emacs_profile_should_execute_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("help");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Emacs";
        options.WelcomeMessage = null;
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Commands:")
      .ShouldBeTrue("Emacs profile should execute commands");
  }

  public static async Task Emacs_profile_should_support_ctrl_f_and_ctrl_b()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.B, ctrl: true); // Ctrl+B backward
    terminal.QueueKey(ConsoleKey.B, ctrl: true);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.F, ctrl: true); // Ctrl+F forward
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Emacs";
        options.WelcomeMessage = null;
      })
      .Map("helo")
        .WithHandler(() => "Emacs Ctrl+F/B work!")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Emacs Ctrl+F/B work!")
      .ShouldBeTrue("Emacs profile should support Ctrl+F and Ctrl+B");
  }

  public static async Task Emacs_profile_should_support_ctrl_p_for_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("version");
    terminal.QueueKey(ConsoleKey.P, ctrl: true); // Ctrl+P previous history
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Emacs";
        options.WelcomeMessage = null;
      })
      .Map("version")
        .WithHandler(() => "1.0.0")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("1.0.0")
      .ShouldBeTrue("Ctrl+P should recall previous command and execute it");
  }

  // ============================================================================
  // Vi Profile Tests (PARTIAL - Missing several handlers)
  // ============================================================================

  [Skip("Help output format not yet finalized - see task 069")]
  public static async Task Vi_profile_should_execute_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("help");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Vi";
        options.WelcomeMessage = null;
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Commands:")
      .ShouldBeTrue("Vi profile should execute commands");
  }

  public static async Task Vi_profile_should_support_arrow_keys()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("test");
    terminal.QueueKey(ConsoleKey.LeftArrow);
    terminal.QueueKey(ConsoleKey.Backspace);
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "Vi";
        options.WelcomeMessage = null;
      })
      .Map("tet")
        .WithHandler(() => "Vi arrow keys work!")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Vi arrow keys work!")
      .ShouldBeTrue("Vi profile should support arrow keys");
  }

  // ============================================================================
  // VSCode Profile Tests (PARTIAL - Missing handlers)
  // ============================================================================

  [Skip("Help output format not yet finalized - see task 069")]
  public static async Task VSCode_profile_should_execute_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("help");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "VSCode";
        options.WelcomeMessage = null;
      })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Commands:")
      .ShouldBeTrue("VSCode profile should execute commands");
  }

  public static async Task VSCode_profile_should_support_home_and_end()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("middle");
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKeys("start-");
    terminal.QueueKey(ConsoleKey.End);
    terminal.QueueKeys("-end");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "VSCode";
        options.WelcomeMessage = null;
      })
      .Map("start-middle-end")
        .WithHandler(() => "VSCode Home/End work!")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("VSCode Home/End work!")
      .ShouldBeTrue("VSCode profile should support Home/End keys");
  }

  public static async Task VSCode_profile_should_support_ctrl_arrow_for_words()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("one two");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true); // Ctrl+Left (word backward)
    terminal.QueueKeys("X");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options =>
      {
        options.KeyBindingProfileName = "VSCode";
        options.WelcomeMessage = null;
      })
      .Map("one Xtwo")
        .WithHandler(() => "VSCode word movement works!")
        .AsQuery()
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("VSCode word movement works!")
      .ShouldBeTrue("VSCode profile should support Ctrl+Arrow for word movement");
  }

  // ============================================================================
  // Profile Factory/Resolution Tests
  // ============================================================================

  public static void Should_resolve_default_profile_by_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
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
      NuruCoreApp app = NuruApp.CreateBuilder([])
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

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl() // No profile specified
      .Build();

    // Assert - Default profile is used implicitly
    app.ShouldNotBeNull("App should build with implicit Default profile");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.KeyBindingProfiles
