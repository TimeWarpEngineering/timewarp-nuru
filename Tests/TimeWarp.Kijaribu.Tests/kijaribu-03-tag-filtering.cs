#!/usr/bin/dotnet --

return await RunTests<TagFilteringTests>(clearCache: true);

[TestTag("Kijaribu")]
public class TagFilteringTests
{
  /// <summary>
  /// TAG-01: Method with matching tag - should run when filter="feature1".
  /// </summary>
  [TestTag("feature1")]
  public static async Task MethodWithMatchingTag()
  {
    WriteLine("MethodWithMatchingTag: Running");
    await Task.CompletedTask;
  }

  /// <summary>
  /// TAG-02: Method with mismatched tag - should skip when filter="feature1".
  /// </summary>
  [TestTag("other")]
  public static async Task MethodWithMismatchedTag()
  {
    WriteLine("MethodWithMismatchedTag: Should not run");
    await Task.CompletedTask;
  }

  /// <summary>
  /// TAG-03: Untagged method in filtered run - should run (implicit match).
  /// </summary>
  public static async Task UntaggedMethod()
  {
    WriteLine("UntaggedMethod: Running (implicit)");
    await Task.CompletedTask;
  }

  /// <summary>
  /// TAG-04: Case-insensitive matching - tag "Feature1" vs filter "feature1".
  /// </summary>
  [TestTag("Feature1")]
  public static async Task CaseInsensitiveMethod()
  {
    WriteLine("CaseInsensitiveMethod: Running");
    await Task.CompletedTask;
  }

  /// <summary>
  /// TAG-EDGE-01: Multiple tags on method - should match if any matches filter.
  /// </summary>
  [TestTag("feature1")]
  [TestTag("extra")]
  public static async Task MultiTagMethod()
  {
    WriteLine("MultiTagMethod: Running (multiple tags)");
    await Task.CompletedTask;
  }

  /// <summary>
  /// TAG-06: Method for env var filtering test.
  /// </summary>
  [TestTag("envtag")]
  public static async Task EnvFilterMethod()
  {
    WriteLine("EnvFilterMethod: Running with env filter");
    await Task.CompletedTask;
  }
}