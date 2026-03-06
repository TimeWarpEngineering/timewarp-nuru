#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR REGRESSION TEST: Unit type fully-qualified in generated code (#442)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify async delegate handlers returning Unit.Value compile and run
// correctly. The source generator must emit the fully-qualified
// global::TimeWarp.Nuru.Unit type name (not just "Unit") to avoid ambiguity
// when other packages that define a Unit type are referenced.
//
// REGRESSION FOR:
// - Bug #442: GetUnwrappedReturnTypeName() strips namespace from Unit, emitting
//   bare "Unit" which is ambiguous when e.g. Mediator.Unit is also in scope.
//
// HOW IT WORKS:
// 1. Define async delegate handlers that return Unit.Value (no closures)
// 2. Verify they compile and execute correctly (exit code = 0)
// 3. The fix ensures the generator emits global::TimeWarp.Nuru.Unit, not Unit
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.UnitTypeAmbiguity
{
  /// <summary>
  /// Regression tests for bug #442: async delegate handlers returning Unit must
  /// use the fully-qualified global::TimeWarp.Nuru.Unit in generated code.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("Regression")]
  public class UnitTypeAmbiguityTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<UnitTypeAmbiguityTests>();

    /// <summary>
    /// Regression test for bug #442: async expression-body delegate returning Unit.Value
    /// must compile and run correctly.
    /// The generator emits "Unit result = await __handler();" — bare Unit is ambiguous
    /// when Mediator or other packages also define Unit. Fix: use global::TimeWarp.Nuru.Unit.
    /// </summary>
    public static async Task Should_support_async_expression_body_delegate_returning_unit()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("ping").WithHandler(async () => { await Task.Delay(1); return Unit.Value; })
        .WithDescription("Async handler returning Unit").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["ping"]);

      // Assert - verifies the generated code compiled and ran without ambiguity errors
      exitCode.ShouldBe(0);
    }

    /// <summary>
    /// Regression test for bug #442: block-body async delegate with a parameter returning Unit.Value
    /// must also compile and run correctly.
    /// </summary>
    public static async Task Should_support_async_block_body_delegate_with_param_returning_unit()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("echo {message}").WithHandler(async (string message) =>
        {
          await Task.Delay(1);
          return Unit.Value;
        }).WithDescription("Async handler with param returning Unit").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["echo", "hello"]);

      // Assert
      exitCode.ShouldBe(0);
    }
  }
}
