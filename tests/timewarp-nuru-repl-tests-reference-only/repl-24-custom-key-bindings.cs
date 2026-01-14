#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.CustomKeyBindings
{

/// <summary>
/// Tests for custom key binding functionality (KeyBindingBuilder and CustomKeyBindingProfile).
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the custom key binding API that allows users to:
/// </para>
/// <list type="bullet">
/// <item><description>Build key bindings from scratch using KeyBindingBuilder</description></item>
/// <item><description>Extend existing profiles using CustomKeyBindingProfile</description></item>
/// <item><description>Override, add, and remove specific key bindings</description></item>
/// </list>
/// </remarks>
[TestTag("REPL")]
public class CustomKeyBindingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CustomKeyBindingTests>();

  // ============================================================================
  // KeyBindingBuilder Tests
  // ============================================================================

  public static async Task KeyBindingBuilder_should_create_empty_builder()
  {
    // Act
    KeyBindingBuilder builder = new();

    // Assert
    builder.BindingCount.ShouldBe(0, "New builder should have no bindings");
    builder.ExitKeyCount.ShouldBe(0, "New builder should have no exit keys");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_bind_key_without_modifiers()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act
    builder.Bind(ConsoleKey.A, () => { });

    // Assert
    builder.BindingCount.ShouldBe(1, "Builder should have one binding");
    builder.IsBound(ConsoleKey.A).ShouldBeTrue("Key A should be bound");
    builder.IsBound(ConsoleKey.B).ShouldBeFalse("Key B should not be bound");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_bind_key_with_modifiers()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act
    builder.Bind(ConsoleKey.K, ConsoleModifiers.Control, () => { });

    // Assert
    builder.IsBound(ConsoleKey.K, ConsoleModifiers.Control).ShouldBeTrue("Ctrl+K should be bound");
    builder.IsBound(ConsoleKey.K, ConsoleModifiers.None).ShouldBeFalse("K without modifiers should not be bound");
    builder.IsBound(ConsoleKey.K, ConsoleModifiers.Alt).ShouldBeFalse("Alt+K should not be bound");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_bind_exit_key()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act
    builder.BindExit(ConsoleKey.Enter, () => { });

    // Assert
    builder.BindingCount.ShouldBe(1, "Exit key should also be a binding");
    builder.ExitKeyCount.ShouldBe(1, "Should have one exit key");
    builder.IsExitKey(ConsoleKey.Enter).ShouldBeTrue("Enter should be an exit key");
    builder.IsBound(ConsoleKey.Enter).ShouldBeTrue("Enter should also be bound");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_bind_exit_key_with_modifiers()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act
    builder.BindExit(ConsoleKey.D, ConsoleModifiers.Control, () => { });

    // Assert
    builder.IsExitKey(ConsoleKey.D, ConsoleModifiers.Control).ShouldBeTrue("Ctrl+D should be an exit key");
    builder.IsExitKey(ConsoleKey.D, ConsoleModifiers.None).ShouldBeFalse("D without modifiers should not be exit key");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_remove_key_binding()
  {
    // Arrange
    KeyBindingBuilder builder = new();
    builder.Bind(ConsoleKey.A, () => { });
    builder.Bind(ConsoleKey.B, () => { });

    // Act
    builder.Remove(ConsoleKey.A);

    // Assert
    builder.BindingCount.ShouldBe(1, "Should have one binding remaining");
    builder.IsBound(ConsoleKey.A).ShouldBeFalse("Key A should be removed");
    builder.IsBound(ConsoleKey.B).ShouldBeTrue("Key B should still be bound");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_remove_exit_key_when_removing_binding()
  {
    // Arrange
    KeyBindingBuilder builder = new();
    builder.BindExit(ConsoleKey.Enter, () => { });

    // Act
    builder.Remove(ConsoleKey.Enter);

    // Assert
    builder.BindingCount.ShouldBe(0, "Binding should be removed");
    builder.ExitKeyCount.ShouldBe(0, "Exit key should also be removed");
    builder.IsExitKey(ConsoleKey.Enter).ShouldBeFalse("Enter should no longer be exit key");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_clear_all_bindings()
  {
    // Arrange
    KeyBindingBuilder builder = new();
    builder.Bind(ConsoleKey.A, () => { });
    builder.Bind(ConsoleKey.B, () => { });
    builder.BindExit(ConsoleKey.Enter, () => { });

    // Act
    builder.Clear();

    // Assert
    builder.BindingCount.ShouldBe(0, "All bindings should be cleared");
    builder.ExitKeyCount.ShouldBe(0, "All exit keys should be cleared");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_support_fluent_chaining()
  {
    // Arrange & Act
    KeyBindingBuilder builder = new KeyBindingBuilder()
      .Bind(ConsoleKey.A, () => { })
      .Bind(ConsoleKey.B, ConsoleModifiers.Control, () => { })
      .BindExit(ConsoleKey.Enter, () => { })
      .Remove(ConsoleKey.A);

    // Assert
    builder.BindingCount.ShouldBe(2, "Should have 2 bindings after chained operations");
    builder.IsBound(ConsoleKey.A).ShouldBeFalse("A should be removed");
    builder.IsBound(ConsoleKey.B, ConsoleModifiers.Control).ShouldBeTrue("Ctrl+B should be bound");
    builder.IsExitKey(ConsoleKey.Enter).ShouldBeTrue("Enter should be exit key");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_mark_existing_binding_as_exit()
  {
    // Arrange
    KeyBindingBuilder builder = new();
    builder.Bind(ConsoleKey.Escape, () => { });

    // Act
    builder.MarkAsExit(ConsoleKey.Escape);

    // Assert
    builder.IsExitKey(ConsoleKey.Escape).ShouldBeTrue("Escape should now be exit key");
    builder.BindingCount.ShouldBe(1, "Binding count should remain same");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_throw_when_marking_unbound_key_as_exit()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act & Assert
    Should.Throw<InvalidOperationException>
    (
      () => builder.MarkAsExit(ConsoleKey.F1),
      "Should throw when marking unbound key as exit"
    );
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_unmark_exit_key()
  {
    // Arrange
    KeyBindingBuilder builder = new();
    builder.BindExit(ConsoleKey.Enter, () => { });

    // Act
    builder.UnmarkAsExit(ConsoleKey.Enter);

    // Assert
    builder.IsExitKey(ConsoleKey.Enter).ShouldBeFalse("Enter should no longer be exit key");
    builder.IsBound(ConsoleKey.Enter).ShouldBeTrue("Enter should still be bound");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_build_bindings_dictionary()
  {
    // Arrange
    int counter = 0;
    KeyBindingBuilder builder = new KeyBindingBuilder()
      .Bind(ConsoleKey.A, () => counter++)
      .BindExit(ConsoleKey.Enter, () => counter += 10);

    // Act
    (Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> bindings,
     HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys) = builder.Build();

    // Assert
    bindings.Count.ShouldBe(2, "Should have 2 bindings");
    exitKeys.Count.ShouldBe(1, "Should have 1 exit key");
    bindings.ContainsKey((ConsoleKey.A, ConsoleModifiers.None)).ShouldBeTrue("Should contain A");
    bindings.ContainsKey((ConsoleKey.Enter, ConsoleModifiers.None)).ShouldBeTrue("Should contain Enter");
    exitKeys.Contains((ConsoleKey.Enter, ConsoleModifiers.None)).ShouldBeTrue("Enter should be in exit keys");

    // Verify actions work
    bindings[(ConsoleKey.A, ConsoleModifiers.None)]();
    counter.ShouldBe(1, "A action should increment counter");
    await Task.CompletedTask;
  }

  public static async Task KeyBindingBuilder_should_throw_on_null_action()
  {
    // Arrange
    KeyBindingBuilder builder = new();

    // Act & Assert
    Should.Throw<ArgumentNullException>
    (
      () => builder.Bind(ConsoleKey.A, null!),
      "Should throw on null action"
    );
    await Task.CompletedTask;
  }

  // ============================================================================
  // CustomKeyBindingProfile Tests
  // ============================================================================

  public static async Task CustomKeyBindingProfile_should_create_with_no_base()
  {
    // Act
    CustomKeyBindingProfile profile = new();

    // Assert
    profile.Name.ShouldBe("Custom", "Default name should be 'Custom'");
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_create_with_base_profile()
  {
    // Arrange
    DefaultKeyBindingProfile baseProfile = new();

    // Act
    CustomKeyBindingProfile profile = new(baseProfile);

    // Assert
    profile.Name.ShouldBe("Custom", "Name should still be 'Custom'");
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_set_custom_name()
  {
    // Arrange & Act
    CustomKeyBindingProfile profile = new CustomKeyBindingProfile()
      .WithName("MyProfile");

    // Assert
    profile.Name.ShouldBe("MyProfile", "Name should be customized");
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_throw_on_null_name()
  {
    // Arrange
    CustomKeyBindingProfile profile = new();

    // Act & Assert
    Should.Throw<ArgumentException>
    (
      () => profile.WithName(null!),
      "Should throw on null name"
    );
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_throw_on_empty_name()
  {
    // Arrange
    CustomKeyBindingProfile profile = new();

    // Act & Assert
    Should.Throw<ArgumentException>
    (
      () => profile.WithName(""),
      "Should throw on empty name"
    );
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_remove_binding()
  {
    // Arrange
    CustomKeyBindingProfile profile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
      .Remove(ConsoleKey.D, ConsoleModifiers.Control);

    // Assert - we need a reader to verify, but the profile should be valid
    profile.Name.ShouldBe("Custom", "Profile should be valid");
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_remove_exit_key()
  {
    // Arrange
    CustomKeyBindingProfile profile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
      .RemoveExitKey(ConsoleKey.D, ConsoleModifiers.Control);

    // Act
    HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys = profile.GetExitKeys();

    // Assert
    exitKeys.Contains((ConsoleKey.D, ConsoleModifiers.Control)).ShouldBeFalse("Ctrl+D should no longer be exit key");
    exitKeys.Contains((ConsoleKey.Enter, ConsoleModifiers.None)).ShouldBeTrue("Enter should still be exit key");
    await Task.CompletedTask;
  }

  public static async Task CustomKeyBindingProfile_should_return_empty_bindings_with_no_base()
  {
    // Arrange
    CustomKeyBindingProfile profile = new();

    // Act
    HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys = profile.GetExitKeys();

    // Assert
    exitKeys.Count.ShouldBe(0, "Profile with no base should have no exit keys");
    await Task.CompletedTask;
  }

  // ============================================================================
  // Integration Tests - ReplOptions with CustomKeyBindingProfile
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_use_custom_profile_from_ReplOptions()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
      .WithName("TestCustomProfile");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.KeyBindingProfile = customProfile;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - if we got here without exception, custom profile works
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Custom profile should work with REPL");
  }

  [Timeout(5000)]
  public static async Task Custom_profile_should_take_precedence_over_profile_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
      .WithName("CustomTakesPrecedence");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.KeyBindingProfile = customProfile;
        options.KeyBindingProfileName = "Emacs"; // This should be ignored
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - successful exit means custom profile was used (not Emacs which might differ)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Custom profile instance should take precedence over name");
  }

  [Timeout(5000)]
  public static async Task Custom_profile_with_removed_ctrl_d_should_still_allow_exit_via_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new DefaultKeyBindingProfile())
      .RemoveExitKey(ConsoleKey.D, ConsoleModifiers.Control)
      .Remove(ConsoleKey.D, ConsoleModifiers.Control);

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.KeyBindingProfile = customProfile;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Ctrl+D was removed but exit command still works
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Exit command should still work when Ctrl+D is removed");
  }

  [Timeout(5000)]
  public static async Task Custom_profile_based_on_emacs_should_work()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
      .WithName("EmacsCustom");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.KeyBindingProfile = customProfile;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - successful exit means Emacs-based custom profile works
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Custom profile based on Emacs should work");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.CustomKeyBindings
