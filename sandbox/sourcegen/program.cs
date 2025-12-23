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

// Run FromSyntax tests (full fidelity)
result += SegmentFromSyntaxTests.Run();
Console.WriteLine();

// Run FromCompiledRoute tests (documents gaps)
result += SegmentFromCompiledRouteTests.Run();
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
