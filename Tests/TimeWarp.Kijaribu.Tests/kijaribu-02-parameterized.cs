namespace TimeWarp.Kijaribu.Tests;

using System;
using System.Threading.Tasks;
using TimeWarp.Kijaribu;

public class ParamTests
{
    /// <summary>
    /// PARAM-03: No [Input] - should run once with empty parameters.
    /// </summary>
    public static async Task NoInputTest()
    {
        // Verify no params passed (could check via reflection or log)
        Console.WriteLine("NoInputTest: No parameters received");
        await Task.CompletedTask;
    }

    /// <summary>
    /// PARAM-01: Single [Input] with string and int args.
    /// Expects string param1, int param2.
    /// </summary>
    [Input("hello", 42)]
    public static async Task SingleInputTest(string param1, int param2)
    {
        // Self-verify: log expected values
        if (param1 == "hello" && param2 == 42)
        {
            Console.WriteLine($"SingleInputTest: Passed with {param1}, {param2}");
        }
        else
        {
            throw new InvalidOperationException("Parameter mismatch");
        }

        await Task.CompletedTask;
    }
    /// <summary>
    /// PARAM-02: Multiple [Input] - two invocations.
    /// </summary>
    [Input("first", 1)]
    [Input("second", 2)]
    public static async Task MultiInputTest(string param1, int param2)
    {
        // Will run twice; log to distinguish
        Console.WriteLine($"MultiInputTest: {param1}, {param2}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// PARAM-04: Type mismatch - expects int but [Input] provides string.
    /// Should fail invocation.
    /// </summary>
    [Input("not-an-int")]
    public static async Task TypeMismatchTest(int _)
    {
        // This won't reach here due to conversion failure
        Console.WriteLine("TypeMismatchTest: Unexpected success");
        await Task.CompletedTask;
    }

    /// <summary>
    /// PARAM-05: Null params for nullable types.
    /// </summary>
    [Input(null, null)]
    public static async Task NullParamTest(string? param1, int? param2)
    {
        if (param1 is null && param2 is null)
        {
            Console.WriteLine("NullParamTest: Null parameters handled");
        }
        else
        {
            throw new InvalidOperationException("Null mismatch");
        }
await Task.CompletedTask;
}

    /// <summary>
    /// PARAM-EDGE-01: [Input] with 0 params for method expecting 2.
    /// Should fail or run with defaults/nulls.
    /// </summary>
    [Input]
    public static async Task ZeroParamsForMultiTest(string param1, int param2)
    {
        // Expect failure or null/defaults
        Console.WriteLine($"ZeroParamsForMultiTest: {param1 ?? "null"}, {param2}");
        await Task.CompletedTask;
    }
}