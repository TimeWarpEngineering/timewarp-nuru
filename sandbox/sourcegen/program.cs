// sandbox/sourcegen/program.cs
// Entry point for running sourcegen tests.
//
// Agent: Amina
// Task: #242-step-3

using TimeWarp.Nuru.SourceGen.Tests;

Console.WriteLine("========================================");
Console.WriteLine("  TimeWarp.Nuru SourceGen Tests");
Console.WriteLine("========================================");
Console.WriteLine();

int result = 0;

// Run segment conversion tests
result += SegmentFromSyntaxTests.Run();
Console.WriteLine();

result += SegmentFromCompiledRouteTests.Run();
Console.WriteLine();

// Run handler builder tests
result += HandlerDefinitionBuilderTests.Run();
Console.WriteLine();

// Run integration tests
result += RouteDefinitionIntegrationTests.Run();
Console.WriteLine();

// Run fluent chain extractor tests
result += FluentChainExtractorTests.Run();
Console.WriteLine();

Console.WriteLine("========================================");
if (result == 0)
{
  Console.WriteLine("  ALL TEST SUITES PASSED");
}
else
{
  Console.WriteLine($"  {result} TEST SUITE(S) HAD FAILURES");
}
Console.WriteLine("========================================");

return result;
