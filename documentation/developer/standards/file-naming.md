# File Naming

Use kebab-case for all file and folder names in the repository.

---

## Apply to

- Source files (`.cs`)
- Project files (`.csproj`)
- Solution files (`.slnx`)
- Documentation (`.md`)
- Configuration files
- Folder names

---

## Examples

✓ Correct:
```
timewarp-nuru.slnx
timewarp-nuru.csproj
global-usings.cs
command-builder.cs
shell-runner.cs
source/timewarp-nuru/
documentation/developer/standards/
```

✗ Incorrect:
```
TimeWarp.Nuru.slnx
TimeWarp.Nuru.csproj
GlobalUsings.cs
CommandBuilder.cs
ShellRunner.cs
source/TimeWarp.Nuru/
Documentation/Developer/Standards/
```

---

## Exceptions

C# namespaces and type names remain PascalCase as required by the language:

```csharp
// File: source/timewarp-nuru/shell/shell-runner.cs
namespace TimeWarp.Nuru;

public class ShellRunner
{
  // ...
}
```

---

## Rationale

- Consistent with web and modern tooling conventions
- Avoids case-sensitivity issues across operating systems
- Matches URL-friendly naming patterns
- Enforced by analyzers in the build process
