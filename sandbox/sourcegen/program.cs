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

// Run fluent chain extractor tests (Source 1)
result += FluentChainExtractorTests.Run();
Console.WriteLine();

// Run fluent route builder extractor tests (Source 2)
result += FluentRouteBuilderExtractorTests.Run();
Console.WriteLine();

// Run attributed route extractor tests (Source 3)
result += AttributedRouteExtractorTests.Run();
Console.WriteLine();

// Run mediator route extractor tests (Source 4)
result += MediatorRouteExtractorTests.Run();
Console.WriteLine();

// Run runtime code emitter tests (Step-4)
result += RuntimeCodeEmitterTests.Run();
Console.WriteLine();

// Run end-to-end emitter tests (Step-4)
result += EndToEndEmitterTests.Run();
Console.WriteLine();

// Run add command demo test (Step-4 key deliverable)
result += AddCommandDemoTest.Run();
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
