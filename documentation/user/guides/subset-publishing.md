# Subset Publishing

Creating specialized CLI editions from a shared command library using group-based endpoint filtering.

---

## Introduction

**Subset publishing** allows you to create multiple specialized editions of your CLI from a single shared command library. Instead of building one monolithic CLI with all features, you can publish focused, smaller executables for specific use cases.

### Why Use Subset Publishing?

**Example: The ganda CLI Tool**

The `ganda` CLI provides a great example of subset publishing in action. Instead of shipping one large executable with all functionality, ganda creates separate editions:

| Edition | Commands | Use Case |
|---------|----------|----------|
| `kanban.exe` | `kanban add`, `kanban move`, `kanban list` | Project management team |
| `git.exe` | `git commit`, `git push`, `git pull` | Developers |
| `ganda.exe` | All kanban + git + additional tools | Full administration |

**Benefits:**

- **Smaller binaries** - Each edition contains only the code it needs
- **Focused tools** - Users get exactly what they need, no clutter
- **Team-specific distributions** - Different teams receive different tool sets
- **Easier documentation** - Each edition has a clear, limited scope
- **Security** - Users can't accidentally run commands outside their scope
- **Faster startup** - Less code to load and initialize

---

## Quick Start

Create a specialized CLI edition in three steps:

### 1. Define Route Groups

Create abstract base classes decorated with `[NuruRouteGroup]` to organize your commands:

```csharp
// shared/Groups.cs
using TimeWarp.Nuru;

namespace MyApp.Shared;

[NuruRouteGroup("admin")]
public abstract class AdminGroupBase;

[NuruRouteGroup("user")]
public abstract class UserGroupBase;
```

### 2. Create Commands in Groups

Inherit from group bases to assign commands to groups:

```csharp
// shared/AdminCommands.cs
using MyApp.Shared;
using TimeWarp.Nuru;

namespace MyApp.Shared;

[NuruRoute("deploy", Description = "Deploy to production")]
public sealed class DeployCommand : AdminGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deploying to {command.Env}");
      return default;
    }
  }
}

// shared/UserCommands.cs
[NuruRoute("profile", Description = "View user profile")]
public sealed class ProfileQuery : UserGroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<ProfileQuery, Unit>
  {
    public ValueTask<Unit> Handle(ProfileQuery query, CancellationToken ct)
    {
      Console.WriteLine("User profile data");
      return default;
    }
  }
}
```

### 3. Create Specialized Editions

Create separate entry points that filter by group:

```csharp
// admin-cli/Program.cs
using MyApp.Shared;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints(typeof(AdminGroupBase))  // Only admin commands
  .Build();

return await app.RunAsync(args);
```

```csharp
// user-cli/Program.cs
using MyApp.Shared;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints(typeof(UserGroupBase))   // Only user commands
  .Build();

return await app.RunAsync(args);
```

**Result:** Two separate executables with different command sets from the same shared library.

---

## API Reference

### DiscoverEndpoints with Group Filtering

```csharp
NuruAppBuilder DiscoverEndpoints(params Type[] groupFilter)
```

Filters endpoint discovery to only include routes belonging to specified group types.

#### Syntax

```csharp
.DiscoverEndpoints(typeof(GroupType1), typeof(GroupType2), ...)
```

#### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `groupFilter` | `params Type[]` | One or more group base types to include. Routes must inherit from at least one of these types to be discovered. |

#### Return Value

Returns `NuruAppBuilder` for method chaining.

#### Examples

**Single Group Filter:**

```csharp
// Only endpoints inheriting from KanbanGroupBase
.DiscoverEndpoints(typeof(KanbanGroupBase))
```

**Multiple Group Filter:**

```csharp
// Endpoints from any of the specified groups
.DiscoverEndpoints(typeof(KanbanGroupBase), typeof(GitGroupBase))
```

**No Filter (All Endpoints):**

```csharp
// All endpoints in the assembly
.DiscoverEndpoints()
```

**Combining with Other Builder Methods:**

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints(typeof(KanbanGroupBase))
  .ConfigureServices(services =>
  {
    services.AddSingleton<ILogger, ConsoleLogger>();
  })
  .Build();
```

---

## How It Works

### Group Type Matching

The filtering system uses **inheritance-based matching**:

1. **Direct inheritance:** A command inherits directly from a group base class
   ```csharp
   public sealed class DeployCommand : AdminGroupBase, ICommand<Unit>
   ```

2. **Nested group inheritance:** A command inherits from a nested group that ultimately derives from a filtered group
   ```csharp
   [NuruRouteGroup("production")]
   public abstract class ProdAdminGroupBase : AdminGroupBase;

   // This command is included when filtering for AdminGroupBase
   public sealed class ProdDeployCommand : ProdAdminGroupBase, ICommand<Unit>
   ```

### Filtering Logic

When you call `DiscoverEndpoints(typeof(SomeGroup))`:

1. The source generator scans all `[NuruRoute]` classes
2. For each class, it walks the inheritance chain
3. If any base class in the chain matches a filter type, the route is **included**
4. Descendant groups are automatically included (inheritance is transitive)

```csharp
[NuruRouteGroup("cloud")]
public abstract class CloudGroupBase;

[NuruRouteGroup("aws")]
public abstract class AwsGroupBase : CloudGroupBase;

[NuruRouteGroup("s3")]
public abstract class S3GroupBase : AwsGroupBase;

// Filtering for CloudGroupBase includes:
// - Commands inheriting from CloudGroupBase
// - Commands inheriting from AwsGroupBase (transitive)
// - Commands inheriting from S3GroupBase (transitive)
.DiscoverEndpoints(typeof(CloudGroupBase))  // Gets all three levels
```

### Prefix Stripping Behavior

When filtering is applied, **the group prefix is stripped from the route pattern**:

**Full Edition (no filter):**

```bash
$ myapp admin deploy production
# Matches: [NuruRoute("deploy")] on AdminGroupBase with prefix "admin"
```

**Filtered Edition (AdminGroupBase filter):**

```bash
$ myapp deploy production
# Matches: Same endpoint, but "admin" prefix is stripped
# Command appears as top-level "deploy" in this edition
```

This means users of the Admin CLI don't type the redundant "admin" prefix - it's implied by the tool they're using.

---

## Examples

### Single Group Edition

Create a CLI with only kanban board commands:

```csharp
// Groups.cs
[NuruRouteGroup("kanban")]
public abstract class KanbanGroupBase;

// Commands.cs
[NuruRoute("add", Description = "Add a new task")]
public sealed class KanbanAddCommand : KanbanGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Task title")]
  public string Title { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<KanbanAddCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanAddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Added task: {command.Title}");
      return default;
    }
  }
}

// Program.cs (kanban edition)
NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints(typeof(KanbanGroupBase))
  .Build();
```

**Usage:**

```bash
$ kanban add "Fix bug"
# Full edition would require: ganda kanban add "Fix bug"
```

### Multiple Group Edition

Create a CLI combining kanban and git commands:

```csharp
// Program.cs (combined edition)
NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints(
    typeof(KanbanGroupBase),
    typeof(GitGroupBase)
  )
  .Build();
```

**Result:**

```bash
$ mytool add "Task"        # kanban add
$ mytool commit -m "msg"   # git commit
```

Commands from both groups appear as top-level commands with their prefixes stripped.

### Full Edition (No Filter)

Create a comprehensive CLI with all commands:

```csharp
// Program.cs (full edition)
NuruApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints()  // No parameters = all endpoints
  .Build();
```

**Usage:**

```bash
$ ganda kanban add "Task"  # Requires full prefixes
$ ganda git commit -m "msg"
$ ganda admin deploy prod
```

### Deep Nesting Example

Handle complex hierarchical command structures:

```csharp
// Groups.cs - Deep hierarchy
[NuruRouteGroup("cloud")]
public abstract class CloudGroupBase;

[NuruRouteGroup("aws")]
public abstract class AwsGroupBase : CloudGroupBase;

[NuruRouteGroup("ec2")]
public abstract class Ec2GroupBase : AwsGroupBase;

[NuruRouteGroup("s3")]
public abstract class S3GroupBase : AwsGroupBase;

// Commands at different levels
[NuruRoute("list", Description = "List all cloud resources")]
public sealed class CloudListQuery : CloudGroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<CloudListQuery, Unit>
  {
    public ValueTask<Unit> Handle(CloudListQuery query, CancellationToken ct)
    {
      Console.WriteLine("All cloud resources");
      return default;
    }
  }
}

[NuruRoute("status", Description = "EC2 instance status")]
public sealed class Ec2StatusQuery : Ec2GroupBase, IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<Ec2StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(Ec2StatusQuery query, CancellationToken ct)
    {
      Console.WriteLine("EC2 instances: running");
      return default;
    }
  }
}

[NuruRoute("upload", Description = "Upload file to S3")]
public sealed class S3UploadCommand : S3GroupBase, ICommand<Unit>
{
  [Parameter(Description = "File path")]
  public string File { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<S3UploadCommand, Unit>
  {
    public ValueTask<Unit> Handle(S3UploadCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Uploading {command.File} to S3");
      return default;
    }
  }
}
```

**Creating Different Editions:**

```csharp
// cloud-cli/Program.cs - Everything cloud-related
.DiscoverEndpoints(typeof(CloudGroupBase))
// Includes: cloud list, aws ec2 status, aws s3 upload
// All appear as: list, ec2 status, s3 upload

// aws-cli/Program.cs - Only AWS-specific
.DiscoverEndpoints(typeof(AwsGroupBase))
// Includes: ec2 status, s3 upload
// Appears as: ec2 status, s3 upload

// s3-cli/Program.cs - Only S3 operations
.DiscoverEndpoints(typeof(S3GroupBase))
// Includes: s3 upload only
// Appears as: upload (stripped "s3" prefix)
```

---

## Project Structure

### Runfile-Based (Recommended for Rapid Development)

Use a `Directory.Build.props` to share endpoint files across multiple runfile entry points:

```
my-cli/
├── Directory.Build.props       # Includes ganda/endpoints/** for all editions
├── ganda/
│   ├── ganda.cs                # Full edition entry point
│   └── endpoints/
│       ├── ganda-group.cs      # [NuruRouteGroup("ganda")] root group
│       ├── kanban-group.cs     # [NuruRouteGroup("kanban")] : GandaGroup
│       ├── git-group.cs        # [NuruRouteGroup("git")] : GandaGroup
│       ├── kanban-add-command.cs
│       ├── kanban-list-command.cs
│       ├── git-commit-command.cs
│       └── git-status-command.cs
├── kanban/
│   └── kanban.cs               # Kanban-only entry point
└── git/
    └── git.cs                  # Git-only entry point
```

The `Directory.Build.props` includes the shared endpoint files into every runfile's compilation:

```xml
<Project>
  <Import Project="$(MSBuildThisFileDirectory)../../Directory.Build.props" />

  <PropertyGroup>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RunfileGlobbingEnabled>true</RunfileGlobbingEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Include shared endpoint files for all editions -->
    <Compile Include="$(MSBuildThisFileDirectory)ganda/endpoints/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="$(MSBuildThisFileDirectory)../../../source/timewarp-nuru/timewarp-nuru.csproj" />
  </ItemGroup>
</Project>
```

Each entry point runfile is minimal:

```csharp
#!/usr/bin/dotnet --
using TimeWarp.Nuru;

// Kanban-only edition
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(KanbanGroup))
  .Build();

return await app.RunAsync(args);
```

### Build and Run

```bash
# Run each edition directly
dotnet run ganda/ganda.cs -- --help
dotnet run kanban/kanban.cs -- --help
dotnet run git/git.cs -- --help

# Publish AOT editions
dotnet publish ganda/ganda.cs -c Release -r linux-x64 -p:PublishAot=true
dotnet publish kanban/kanban.cs -c Release -r linux-x64 -p:PublishAot=true
```

---

## Best Practices

### Group Naming Conventions

**Use clear, domain-relevant names:**

```csharp
// Good
[NuruRouteGroup("git")]
[NuruRouteGroup("kanban")]
[NuruRouteGroup("docker")]

// Avoid
[NuruRouteGroup("group1")]
[NuruRouteGroup("misc")]
```

**Use hierarchical names for nested groups:**

```csharp
[NuruRouteGroup("cloud")]
[NuruRouteGroup("cloud-aws")]      // AWS under cloud
[NuruRouteGroup("cloud-azure")]    // Azure under cloud
[NuruRouteGroup("cloud-aws-ec2")]  // EC2 under AWS
```

### Hierarchical Design

**Design for inheritance:**

```csharp
// Create base groups for shared functionality
[NuruRouteGroup("shared")]
public abstract class SharedGroupBase
{
  [GroupOption("verbose", "v")]
  public bool Verbose { get; set; }
}

// Derive specific groups from shared base
[NuruRouteGroup("admin")]
public abstract class AdminGroupBase : SharedGroupBase;

[NuruRouteGroup("user")]
public abstract class UserGroupBase : SharedGroupBase;

// All commands get --verbose option automatically
```

**Group by domain, not by edition:**

```csharp
// Good: Group by what the commands do
[NuruRouteGroup("deployment")]
[NuruRouteGroup("monitoring")]

// Avoid: Group by who uses them
[NuruRouteGroup("admin-commands")]
[NuruRouteGroup("user-commands")]
```

### Testing Strategy

**Test the shared library once:**

```csharp
// Shared.Tests/CommandTests.cs
public class DeployCommandTests
{
  [Fact]
  public async Task DeployCommand_HandlesSuccessfully()
  {
    using TestTerminal terminal = new();
    // Test the command implementation directly
  }
}
```

**Test each edition's routing:**

```csharp
// Admin.Tests/RoutingTests.cs
public class AdminEditionRoutingTests
{
  [Fact]
  public async Task AdminEdition_ContainsDeployCommand()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .DiscoverEndpoints(typeof(AdminGroupBase))
      .Build();

    await app.RunAsync(["deploy", "production"]);
    terminal.OutputContains("Deploying to production").ShouldBeTrue();
  }

  [Fact]
  public async Task AdminEdition_DoesNotContainUserCommand()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .DiscoverEndpoints(typeof(AdminGroupBase))
      .Build();

    await app.RunAsync(["profile"]);
    terminal.OutputContains("Unknown command").ShouldBeTrue();
  }
}
```

---

## Troubleshooting

### Empty CLI Result

**Problem:** Running the filtered CLI shows no commands or "No commands available".

**Causes and Solutions:**

1. **Wrong group type specified**
   ```csharp
   // Wrong: Using a command type instead of group type
   .DiscoverEndpoints(typeof(DeployCommand))

   // Correct: Using the group base type
   .DiscoverEndpoints(typeof(AdminGroupBase))
   ```

2. **Commands not inheriting from group**
   ```csharp
   // Wrong: Command doesn't inherit from group
   [NuruRoute("deploy")]
   public sealed class DeployCommand : ICommand<Unit>;  // Missing AdminGroupBase

   // Correct: Explicit inheritance
   [NuruRoute("deploy")]
   public sealed class DeployCommand : AdminGroupBase, ICommand<Unit>;
   ```

3. **Group filter in wrong assembly**
   ```csharp
   // The group type must be accessible to the entry point
   // Ensure MyApp.Shared is referenced and the group type is public
   public abstract class AdminGroupBase;  // Must be public, not internal
   ```

### Missing Commands

**Problem:** Some commands expected in the filtered CLI are missing.

**Causes and Solutions:**

1. **Command in different group hierarchy**
   ```csharp
   // This command is NOT included in AdminGroupBase filtering
   [NuruRouteGroup("superadmin")]
   public abstract class SuperAdminGroupBase;  // Not related to AdminGroupBase

   public sealed class SecretCommand : SuperAdminGroupBase, ICommand<Unit>;

   // Include both groups for the edition that needs both
   .DiscoverEndpoints(typeof(AdminGroupBase), typeof(SuperAdminGroupBase))
   ```

2. **Multiple inheritance not supported**
   ```csharp
   // This doesn't work - C# doesn't support multiple inheritance
   public sealed class HybridCommand : AdminGroupBase, UserGroupBase, ICommand<Unit>;

   // Solution: Create a combined edition instead
   .DiscoverEndpoints(typeof(AdminGroupBase), typeof(UserGroupBase))
   ```

### Prefix Not Stripped

**Problem:** Users must still type the full prefixed command in filtered editions.

**Causes and Solutions:**

1. **Check that filtering is actually applied**
   ```csharp
   // Wrong: Forgot to pass parameters
   .DiscoverEndpoints()  // No filter - includes all with full prefixes

   // Correct: Pass the group type
   .DiscoverEndpoints(typeof(AdminGroupBase))
   ```

2. **Verify the group has a prefix attribute**
   ```csharp
   // Wrong: Missing [NuruRouteGroup] attribute
   public abstract class AdminGroupBase;  // No attribute = no prefix to strip

   // Correct: Add the attribute
   [NuruRouteGroup("admin")]
   public abstract class AdminGroupBase;
   ```

---

## See Also

- **Sample:** [samples/editions/01-group-filtering/](../../samples/editions/01-group-filtering/) - Complete working example with shared endpoints, three runfile editions
- **Route Groups:** [route-groups.md](route-groups.md) - Detailed documentation on creating hierarchical command structures
- **Testing:** [testing.md](testing.md) - Testing strategies for filtered CLI editions
- **AOT Publishing:** [aot-publishing.md](aot-publishing.md) - Building native executables for your editions

---

## Summary

Subset publishing with `DiscoverEndpoints(params Type[])` enables:

- **Single codebase, multiple CLIs** - Share all command logic
- **Automatic prefix stripping** - Clean command interfaces per edition
- **Inheritance-based filtering** - Include descendants automatically
- **Flexible deployment** - Ship exactly what each user needs

Start with a shared library containing your groups and commands, then create focused entry points that filter to specific groups for specialized editions.
