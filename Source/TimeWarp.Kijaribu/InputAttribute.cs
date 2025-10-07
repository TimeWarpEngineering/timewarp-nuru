namespace TimeWarp.Kijaribu;

/// <summary>
/// Provides input parameters for a parameterized test.
/// Can be applied multiple times to run the test with different inputs.
/// Similar to xUnit's [InlineData] or Fixie's [Input].
/// </summary>
/// <param name="parameters">The parameter values to pass to the test method.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InputAttribute(params object?[] parameters) : Attribute
{
  /// <summary>
  /// Gets the parameter values for this test input.
  /// </summary>
  public object?[] Parameters { get; } = parameters;
}
