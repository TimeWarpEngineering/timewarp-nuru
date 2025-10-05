namespace TimeWarp.Kijaribu.Tests;

using System;
using System.Threading.Tasks;
using TimeWarp.Kijaribu;

[TestTag("feature1")]
public static class ClassTagged
{
    /// <summary>
    /// TAG-01: Method in matching class - should run when filter="feature1".
    /// </summary>
    public static async Task MethodInMatchingClass()
    {
        Console.WriteLine("ClassTagged.MethodInMatchingClass: Running");
        await Task.CompletedTask;
    }
}

[TestTag("other")]
public static class ClassMismatched
{
    /// <summary>
    /// TAG-02: Method in mismatched class - should skip entire class when filter="feature1".
    /// </summary>
    public static async Task MethodInMismatchedClass()
    {
        Console.WriteLine("ClassMismatched.MethodInMismatchedClass: Should not run");
        await Task.CompletedTask;
    }
}

public static class MethodLevelTagged
{
    /// <summary>
    /// TAG-03: Method with matching tag in untagged class - should run when filter="feature1".
    /// </summary>
    [TestTag("feature1")]
    public static async Task MethodWithMatchingTag()
    {
        Console.WriteLine("MethodWithMatchingTag: Running");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TAG-04: Method with mismatched tag - should skip when filter="feature1".
    /// </summary>
    [TestTag("other")]
    public static async Task MethodWithMismatchedTag()
    {
        Console.WriteLine("MethodWithMismatchedTag: Should not run");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TAG-05: Untagged method in filtered run - should run (implicit match).
    /// </summary>
    public static async Task UntaggedMethod()
    {
        Console.WriteLine("UntaggedMethod: Running (implicit)");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TAG-07: Case-insensitive matching - tag "Feature1" vs filter "feature1".
    /// </summary>
    [TestTag("Feature1")]
    public static async Task CaseInsensitiveMethod()
    {
        Console.WriteLine("CaseInsensitiveMethod: Running");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TAG-EDGE-01: Multiple tags on method - should match if any matches filter.
    /// </summary>
    [TestTag("feature1")]
    [TestTag("extra")]
    public static async Task MultiTagMethod()
    {
        Console.WriteLine("MultiTagMethod: Running (multiple tags)");
        await Task.CompletedTask;
    }
}

public static class EnvFilterTestClass
{
    /// <summary>
    /// TAG-06: Method for env var filtering test.
    /// </summary>
    [TestTag("envtag")]
    public static async Task EnvFilterMethod()
    {
        Console.WriteLine("EnvFilterMethod: Running with env filter");
        await Task.CompletedTask;
    }
}