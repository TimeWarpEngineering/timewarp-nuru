namespace TimeWarp.Kijaribu.Tests;

using System;
using System.Threading.Tasks;
using TimeWarp.Kijaribu;

public static class SkipTests
{
    /// <summary>
    /// SKIP-01: Skipped test with reason - should skip and report reason.
    /// </summary>
    [Skip("WIP - Work in progress")]
    public static async Task SkippedTest()
    {
        Console.WriteLine("SkippedTest: Should not run");
        await Task.CompletedTask;
    }

    /// <summary>
    /// SKIP-02: Runtime exception - ArgumentException.
    /// </summary>
    public static async Task ExceptionTest()
    {
        throw new ArgumentException("Intentional runtime exception for SKIP-02");
    }

    /// <summary>
    /// SKIP-03: TargetInvocationException - wrapped exception.
    /// </summary>
    public static async Task InvocationExceptionTest()
    {
        // This will be invoked via reflection, so throw to trigger TargetInvocationException
        throw new InvalidOperationException("Inner exception for SKIP-03");
    }

    /// <summary>
    /// SKIP-04: Async exception after await.
    /// </summary>
    public static async Task AsyncExceptionTest()
    {
        await Task.Delay(1); // Await first
        throw new NotSupportedException("Async exception after await for SKIP-04");
    }

    /// <summary>
    /// Additional passing test to validate skipping doesn't affect others.
    /// </summary>
    public static async Task PassingTest()
    {
        Console.WriteLine("PassingTest: Running successfully");
        await Task.CompletedTask;
    }
}