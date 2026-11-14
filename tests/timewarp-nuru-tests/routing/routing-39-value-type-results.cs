#!/usr/bin/env -S dotnet --

#region Purpose
// Pins how the generated invoker writes a handler result that is a user value type: an enum prints its
// member name, a user struct is written with no null guard, and a Nullable<TEnum> null writes nothing.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.ValueTypeResults
{

// Task 583: the invoker decided "value type" from the type name, so only BCL value types skipped the
// `result is not null` guard and the `result?.ToString()` JSON fallback. A user enum or struct result got
// both and the generated code failed with CS0037 / CS0023. Each test here only compiles when the generator
// reads the result type from the semantic model.
[TestTag("Routing")]
public class ValueTypeResultTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ValueTypeResultTests>();

  public static async Task Should_print_enum_result_member_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("vtr-mode {shade:VtrShade}").WithHandler((VtrShade shade) => shade).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-mode", "Dark"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("Dark");
  }

  public static async Task Should_print_async_enum_result_member_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("vtr-async")
        .WithHandler(async () =>
        {
          await Task.Yield();
          return VtrShade.Light;
        })
        .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-async"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("Light");
  }

  public static async Task Should_print_enum_result_from_method_group()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("vtr-pick").WithHandler(VtrHandlers.Pick).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-pick"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("Dark");
  }

  public static async Task Should_print_enum_result_from_query_endpoint()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<VtrShadeQuery>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-shade-query"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBe("Light");
  }

  public static async Task Should_write_user_struct_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("vtr-spot").WithHandler(() => new VtrSpot(3, 4)).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-spot"]);

    // Assert: JSON from a generated context, or the ToString() fallback; either names both members.
    exitCode.ShouldBe(0);
    terminal.Output.ShouldContain("3");
    terminal.Output.ShouldContain("4");
  }

  public static async Task Should_write_nothing_for_null_nullable_enum_result()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("vtr-maybe").WithHandler(() => (VtrShade?)null).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["vtr-maybe"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.Output.Trim().ShouldBeEmpty();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing.ValueTypeResults

// The result types live in the global namespace: the invoker declares a delegate result local by its short
// type name, so a type in a namespace the generated file does not import fails with CS0246 (a separate defect).
public enum VtrShade { Light, Dark }

public readonly record struct VtrSpot(int X, int Y);

public static class VtrHandlers
{
  public static VtrShade Pick() => VtrShade.Dark;
}

[NuruRoute("vtr-shade-query", Description = "Returns an enum")]
public sealed class VtrShadeQuery : IQuery<VtrShade>
{
  public sealed class Handler : IRequestHandler<VtrShadeQuery, VtrShade>
  {
    public ValueTask<VtrShade> Handle(VtrShadeQuery query, CancellationToken cancellationToken) =>
      ValueTask.FromResult(VtrShade.Light);
  }
}
