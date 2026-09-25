# TimeWarp.Nuru Samples

> 🚀 **DSL-First Organization**: Samples organized by DSL paradigm for clarity

## Quick Start

Choose your paradigm based on your needs:

| If you need... | Go to |
|----------------|-------|
| **Testability & DI** | [`endpoints/`](./endpoints/) |
| **Simple scripts** | [`fluent/`](./fluent/) |
| **Migration examples** | [`hybrid/`](./hybrid/) | ⚠️ Edge Cases |

## DSL Paradigms Explained

### ⭐ Endpoint DSL

**Class-based, attribute-driven approach**

```csharp
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetCommand : IQuery<Unit>
{
  [Parameter] public string Name { get; set; } = "";

  public sealed class Handler : IQueryHandler<GreetCommand, Unit>
  {
    public Task<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {c.Name}!");
      return Unit.Task;
    }
  }
}

// In your main file:
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();
```

**Best for:**
- ✅ Production applications
- ✅ Complex business logic
- ✅ Unit testing
- ✅ Dependency injection
- ✅ Team collaboration

**Learning Path:**
1. `endpoints/01-hello-world/` - Start here
2. `endpoints/02-calculator/` - Complex example
3. `endpoints/03-syntax/` - All route patterns

---

### 🚀 Fluent DSL

**Functional, inline approach**

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .AsQuery()
    .Done()
  .Build();
```

**Best for:**
- ✅ Quick scripts
- ✅ Simple tools
- ✅ One-off utilities
- ✅ Maximum performance

**Learning Path:**
1. `fluent/01-hello-world/` - Start here
2. `fluent/02-calculator/` - Full calculator
3. `fluent/03-syntax/` - All route patterns

---

### ⚠️ Hybrid

**Mixing both paradigms** (rarely needed)

Only use hybrid patterns for:
- Migration scenarios (temporary)
- Performance optimization (measured)
- Educational demonstrations

**See:** `hybrid/03-when-to-mix/hybrid-decision-guide.md`

> **Warning**: Most applications should pick ONE paradigm. Mixing adds complexity.

---

## Directory Structure

```
samples/
├── fluent/           # 24+ Fluent DSL samples
│   ├── 01-hello-world/
│   ├── 02-calculator/
│   ├── 03-syntax/
│   ├── 04-async/
│   ├── 05-pipeline/
│   ├── 06-testing/
│   ├── 07-configuration/
│   ├── 08-type-converters/
│   ├── 09-repl/
│   ├── 10-logging/
│   ├── 11-completion/
│   └── 12-runtime-di/
│
├── endpoints/        # 24+ Endpoint DSL samples
│   ├── 01-hello-world/
│   ├── 02-calculator/
│   ├── 03-syntax/
│   ├── 04-async/
│   ├── 05-pipeline/
│   ├── 06-testing/
│   ├── 07-configuration/
│   ├── 08-type-converters/
│   ├── 09-repl/
│   ├── 10-logging/
│   ├── 11-discovery/
│   ├── 12-completion/
│   └── 13-runtime-di/
│
└── hybrid/           # 5 edge case samples
    ├── 01-migration/
    ├── 02-unified-pipeline/
    └── 03-when-to-mix/
```

---

## Examples Index

| Feature | Fluent | Endpoint |
|---------|--------|----------|
| Hello World | `fluent-hello-world-lambda.cs` | `endpoint-hello-world.cs` |
| Calculator | `fluent-calculator-delegate.cs` | `endpoint-calculator.cs` |
| Route Syntax | `fluent-syntax-examples.cs` | `endpoint-syntax-examples.cs` |
| Async | `fluent-async-examples.cs` | `endpoint-async-examples.cs` |
| Pipeline | `fluent-pipeline-*.cs` (6 files) | `endpoint-pipeline-*.cs` (6 files) |
| Testing | `fluent-testing-*.cs` (3 files) | `endpoint-testing-*.cs` (3 files) |
| Configuration | `fluent-configuration-*.cs` (4 files) | `endpoint-configuration-*.cs` (4 files) |
| Type Converters | `fluent-type-converters-*.cs` (2 files) | `endpoint-type-converters-*.cs` (2 files) |
| REPL | `fluent-repl-*.cs` (4 files) | `endpoint-repl-*.cs` (4 files) |
| Logging | `fluent-logging-*.cs` (2 files) | `endpoint-logging-*.cs` (2 files) |
| Completion | `fluent-completion.cs` | `endpoint-completion.cs` |
| Runtime DI | `fluent-runtime-di-*.cs` (2 files) | `endpoint-runtime-di-*.cs` (2 files) |

---

## Choosing a DSL

### Use **Endpoint DSL** when:
- Building production systems
- You need unit tests
- Working with a team
- Complex dependency injection
- Long-term maintainability

### Use **Fluent DSL** when:
- Writing one-off scripts
- Maximum performance is critical
- Simple tools and utilities
- Quick prototyping

### Use **Hybrid** when:
- Migrating from one paradigm to another (temporary)
- Specific measured performance needs
- Educational purposes

---

## Running Samples

All samples are runfiles (single-file executables):

```bash
# Run any sample
dotnet run samples/endpoints/01-hello-world/endpoint-hello-world.cs
dotnet run samples/fluent/01-hello-world/fluent-hello-world-lambda.cs

# Most samples support --help
dotnet run samples/endpoints/02-calculator/endpoint-calculator.cs -- --help
```

---

## Additional Resources

- **Documentation**: See `/docs/` folder
- **API Reference**: Run `dotnet run -- --help` on any sample
- **Testing**: See `tests/timewarp-nuru-core-tests/`

---

## Philosophy

> Developers typically pick ONE DSL paradigm and commit to it, similar to ASP.NET Minimal APIs vs Controllers.

The DSL-first organization helps you:
- **Identify** the pattern from the path alone
- **Learn** by comparing equivalent Fluent and Endpoint samples
- **Choose** the right paradigm for your use case
