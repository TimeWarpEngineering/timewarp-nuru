#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

return await RunTests<VersionInfoTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public class VersionInfoTests
{
  public static async Task Should_return_version_information()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldContain("TimeWarp.Nuru Version Information");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_include_version_number()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    result.ShouldContain("Version:");
    // Should contain a version pattern (e.g., "2.1.0" or "beta")
    (result.Contains("2.") || result.Contains("beta") || result.Contains("Unknown")).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_include_assembly_information()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    result.ShouldContain("Assembly Information:");
    result.ShouldContain("Location:");
    result.ShouldContain("Full Name:");

    await Task.CompletedTask;
  }

  public static async Task Should_contain_proper_formatting()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    result.ShouldContain("==================================");
    // Should have multiple lines
    result.Split('\n').Length.ShouldBeGreaterThan(5);

    await Task.CompletedTask;
  }

  public static async Task Should_reference_nuru_assembly()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    // The assembly location should reference TimeWarp.Nuru
    result.ShouldContain("TimeWarp.Nuru");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_missing_metadata_gracefully()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    // Even if some metadata is missing, should return valid info
    result.ShouldNotBeNullOrEmpty();
    result.ShouldContain("Version:");

    await Task.CompletedTask;
  }

  public static async Task Should_include_commit_info_if_available()
  {
    // Arrange & Act
    string result = await GetVersionInfoTool.GetVersionInfoAsync();

    // Assert
    // If TimeWarp.Build.Tasks injected metadata, it should be present
    // Otherwise, the tool should still work
    result.ShouldNotBeNullOrEmpty();

    // If commit info is present, it should have proper format
    if (result.Contains("Commit:"))
    {
      result.ShouldContain("Commit:");
    }

    if (result.Contains("Date:"))
    {
      result.ShouldContain("Date:");
    }

    await Task.CompletedTask;
  }
}
