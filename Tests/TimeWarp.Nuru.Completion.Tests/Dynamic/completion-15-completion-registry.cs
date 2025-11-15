#!/usr/bin/dotnet --

return await RunTests<CompletionRegistryTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CompletionRegistryTests
{
  public static async Task Should_register_and_retrieve_by_parameter_name()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source = new("test-source");

    // Act
    registry.RegisterForParameter("env", source);
    ICompletionSource? retrieved = registry.GetSourceForParameter("env");

    // Assert
    retrieved.ShouldNotBeNull();
    retrieved.ShouldBe(source);

    await Task.CompletedTask;
  }

  public static async Task Should_register_and_retrieve_by_type()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source = new("enum-source");

    // Act
    registry.RegisterForType(typeof(TestEnum), source);
    ICompletionSource? retrieved = registry.GetSourceForType(typeof(TestEnum));

    // Assert
    retrieved.ShouldNotBeNull();
    retrieved.ShouldBe(source);

    await Task.CompletedTask;
  }

  public static async Task Should_overwrite_existing_parameter_registration()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source1 = new("source1");
    TestCompletionSource source2 = new("source2");

    // Act
    registry.RegisterForParameter("env", source1);
    registry.RegisterForParameter("env", source2); // Overwrite
    ICompletionSource? retrieved = registry.GetSourceForParameter("env");

    // Assert
    retrieved.ShouldBe(source2); // Should return the second registration

    await Task.CompletedTask;
  }

  public static async Task Should_overwrite_existing_type_registration()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source1 = new("source1");
    TestCompletionSource source2 = new("source2");

    // Act
    registry.RegisterForType(typeof(TestEnum), source1);
    registry.RegisterForType(typeof(TestEnum), source2); // Overwrite
    ICompletionSource? retrieved = registry.GetSourceForType(typeof(TestEnum));

    // Assert
    retrieved.ShouldBe(source2); // Should return the second registration

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_unregistered_parameter_name()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act
    ICompletionSource? retrieved = registry.GetSourceForParameter("unknown");

    // Assert
    retrieved.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_return_null_for_unregistered_type()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act
    ICompletionSource? retrieved = registry.GetSourceForType(typeof(TestEnum));

    // Assert
    retrieved.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_parameter_name_on_register()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source = new("test");

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.RegisterForParameter(null!, source));

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_parameter_name_on_get()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.GetSourceForParameter(null!));

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_type_on_register()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource source = new("test");

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.RegisterForType(null!, source));

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_type_on_get()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.GetSourceForType(null!));

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_source_on_register_parameter()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.RegisterForParameter("env", null!));

    await Task.CompletedTask;
  }

  public static async Task Should_throw_ArgumentNullException_for_null_source_on_register_type()
  {
    // Arrange
    CompletionSourceRegistry registry = new();

    // Act & Assert
    Should.Throw<ArgumentNullException>(() =>
      registry.RegisterForType(typeof(TestEnum), null!));

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_independent_registrations()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource envSource = new("env-source");
    TestCompletionSource tagSource = new("tag-source");
    TestCompletionSource enumSource = new("enum-source");
    TestCompletionSource stringSource = new("string-source");

    // Act
    registry.RegisterForParameter("env", envSource);
    registry.RegisterForParameter("tag", tagSource);
    registry.RegisterForType(typeof(TestEnum), enumSource);
    registry.RegisterForType(typeof(string), stringSource);

    // Assert
    registry.GetSourceForParameter("env").ShouldBe(envSource);
    registry.GetSourceForParameter("tag").ShouldBe(tagSource);
    registry.GetSourceForType(typeof(TestEnum)).ShouldBe(enumSource);
    registry.GetSourceForType(typeof(string)).ShouldBe(stringSource);

    await Task.CompletedTask;
  }

  public static async Task Should_not_cross_contaminate_parameter_and_type_registrations()
  {
    // Arrange
    CompletionSourceRegistry registry = new();
    TestCompletionSource paramSource = new("param-source");
    TestCompletionSource typeSource = new("type-source");

    // Act - Register a source for parameter "env" and type string
    registry.RegisterForParameter("env", paramSource);
    registry.RegisterForType(typeof(string), typeSource);

    // Assert - Retrieving by parameter should only return parameter source
    ICompletionSource? byParam = registry.GetSourceForParameter("env");
    ICompletionSource? byType = registry.GetSourceForType(typeof(string));

    byParam.ShouldBe(paramSource);
    byType.ShouldBe(typeSource);
    byParam.ShouldNotBe(typeSource);

    await Task.CompletedTask;
  }
}

// =============================================================================
// Test Helpers
// =============================================================================

enum TestEnum
{
  Value1,
  Value2,
  Value3
}

sealed class TestCompletionSource(string id) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Return test candidates with source ID in description
    yield return new CompletionCandidate(
      Value: "test-value",
      Description: $"From {id}",
      Type: CompletionType.Parameter
    );
  }

  public override string ToString() => $"TestCompletionSource({id})";
}
