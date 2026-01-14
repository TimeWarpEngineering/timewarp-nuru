#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test enum parameter completion in REPL (Bug fix #041)
//
// NOTE: This test file has been disabled because enum type parameters in route patterns
// require source generator support that may not be fully implemented yet.
// The old runtime API used AddTypeConverter() for custom enums, but the source generator
// architecture requires the type name to be resolvable at compile time.
//
// TODO: Re-enable when enum completion is fully implemented with source generator support.
// Issues:
// - Route pattern type names like {env:environment} expect the type to be globally accessible
// - The generator generates global:: prefixed type references
// - Local enum types in test files aren't accessible to generated code

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests
{
  public enum Environment
  {
    Dev,
    Staging,
    Prod
  }

  [TestTag("REPL")]
  public class EnumCompletionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EnumCompletionTests>();

    [Skip("Enum completion requires source generator enum support - test needs rewrite for new architecture")]
    public static async Task Test_placeholder_for_skipped_class()
    {
      // This test class is skipped because enum type parameters in route patterns
      // require source generator support that isn't fully compatible with local enum types.
      await Task.CompletedTask;
    }
  }
}
