#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests that unbound boolean flags act as route discriminators (required to match),
/// allowing "list --all" + "list {filter?}" to coexist without a false NURU_R003.
/// </summary>
[TestTag("Routing")]
public class UnboundFlagDiscriminatorTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UnboundFlagDiscriminatorTests>();

  public static async Task Should_not_warn_nuru_r003_for_unbound_flag_discriminator()
  {
    // Arrange - list --all (unbound flag => required discriminator) and
    // list {filter?} (optional param) both reduce differently now, so no NURU_R003.
    // If the false positive regressed, this app would fail to compile under
    // TreatWarningsAsErrors (R003 is Error severity).
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("list --all").WithHandler(() => "all-items").AsQuery().Done()
      .Map("list {filter?}").WithHandler((string? filter) => $"filtered:{filter ?? "none"}").AsQuery().Done()
      .Build();

    int exitCode = await app.RunAsync(["list", "--all"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("all-items").ShouldBeTrue();

    terminal.ClearOutput();
    exitCode = await app.RunAsync(["list", "foo"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("filtered:foo").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
