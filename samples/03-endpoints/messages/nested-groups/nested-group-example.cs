namespace Endpoints.Messages;

using TimeWarp.Nuru;

/// <summary>
/// Level 1: Top-level route group
/// </summary>
[NuruRouteGroup("cloud")]
public abstract class CloudGroupBase;

/// <summary>
/// Level 2: Nested route group inheriting from CloudGroupBase
/// This demonstrates multi-level group prefix concatenation.
/// Expected pattern: "cloud azure ..."
/// </summary>
[NuruRouteGroup("azure")]
public abstract class AzureGroupBase : CloudGroupBase;

/// <summary>
/// Level 3: Nested route group inheriting from AzureGroupBase
/// This demonstrates three-level group prefix concatenation.
/// Expected pattern: "cloud azure storage ..."
/// </summary>
[NuruRouteGroup("storage")]
public abstract class AzureStorageGroupBase : AzureGroupBase;

/// <summary>
/// Command: cloud azure storage upload {file}
/// Three-level nested route group example.
/// </summary>
[NuruRoute("upload", Description = "Upload a file to Azure storage")]
public sealed class AzureStorageUploadCommand : AzureStorageGroupBase, ICommand<Unit>
{
  [Parameter(Description = "File to upload")]
  public string File { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<AzureStorageUploadCommand, Unit>
  {
    public ValueTask<Unit> Handle(AzureStorageUploadCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Uploading file to Azure storage: {command.File}");
      return default;
    }
  }
}

/// <summary>
/// Command: cloud azure vm start {name}
/// Two-level nested route group example.
/// </summary>
[NuruRoute("vm start", Description = "Start an Azure VM")]
public sealed class AzureVmStartCommand : AzureGroupBase, ICommand<Unit>
{
  [Parameter(Description = "VM name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<AzureVmStartCommand, Unit>
  {
    public ValueTask<Unit> Handle(AzureVmStartCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Starting Azure VM: {command.Name}");
      return default;
    }
  }
}
