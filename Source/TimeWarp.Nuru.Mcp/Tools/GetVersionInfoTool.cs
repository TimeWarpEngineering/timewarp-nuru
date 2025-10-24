namespace TimeWarp.Nuru.Mcp.Tools;

using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// MCP tool that provides TimeWarp.Nuru version information including version number,
/// git commit hash, and build date from assembly metadata.
/// </summary>
internal sealed class GetVersionInfoTool
{
  [McpServerTool]
  [Description("Get TimeWarp.Nuru version, git commit hash, and build information")]
  public static Task<string> GetVersionInfoAsync()
  {
    Assembly assembly = typeof(NuruApp).Assembly;

    // Get version from AssemblyInformationalVersionAttribute (includes +commit if present)
    string? informationalVersion = assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
      .InformationalVersion;

    // Get simple version from AssemblyVersionAttribute
    string? version = assembly.GetName().Version?.ToString();

    // Try to get git commit hash from assembly metadata (injected by TimeWarp.Build.Tasks)
    string? commitHash = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(a => a.Key == "CommitHash")?
      .Value;

    // Try to get commit date from assembly metadata (injected by TimeWarp.Build.Tasks)
    string? commitDate = assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(a => a.Key == "CommitDate")?
      .Value;

    // Format the output
    var builder = new StringBuilder();
    builder.AppendLine("TimeWarp.Nuru Version Information");
    builder.AppendLine("==================================");
    builder.AppendLine();

    if (!string.IsNullOrEmpty(informationalVersion))
    {
      builder.Append(CultureInfo.InvariantCulture, $"Version: {informationalVersion}");
      builder.AppendLine();
    }
    else if (!string.IsNullOrEmpty(version))
    {
      builder.Append(CultureInfo.InvariantCulture, $"Version: {version}");
      builder.AppendLine();
    }
    else
    {
      builder.AppendLine("Version: Unknown");
    }

    if (!string.IsNullOrEmpty(commitHash))
    {
      builder.Append(CultureInfo.InvariantCulture, $"Commit:  {commitHash}");
      builder.AppendLine();
    }

    if (!string.IsNullOrEmpty(commitDate))
    {
      builder.Append(CultureInfo.InvariantCulture, $"Date:    {commitDate}");
      builder.AppendLine();
    }

    builder.AppendLine();
    builder.AppendLine("Assembly Information:");
    builder.Append(CultureInfo.InvariantCulture, $"  Location: {assembly.Location}");
    builder.AppendLine();
    builder.Append(CultureInfo.InvariantCulture, $"  Full Name: {assembly.FullName}");
    builder.AppendLine();

    return Task.FromResult(builder.ToString());
  }
}
