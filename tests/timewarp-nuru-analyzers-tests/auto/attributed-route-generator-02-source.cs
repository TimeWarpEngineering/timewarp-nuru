#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTE GENERATOR TESTS - Source Code Verification
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that verify the NuruAttributedRouteGenerator produces correct
// CompiledRouteBuilder code. These tests inspect the generated source code
// rather than just verifying routes are registered.
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.CodeAnalysis;

int passed = 0;
int failed = 0;

void Pass(string testName)
{
  Console.WriteLine($"✓ {testName}");
  passed++;
}

void Fail(string testName, string message)
{
  Console.WriteLine($"✗ {testName}: {message}");
  failed++;
}

// ═══════════════════════════════════════════════════════════════════════════════
// LITERAL GENERATION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 1: Simple literal route
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("status")]
    public sealed class StatusRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<StatusRequest>
      {
        public ValueTask<Unit> Handle(StatusRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  if (code != null && code.Contains(".WithLiteral(\"status\")"))
    Pass("Test 1: Simple literal generates .WithLiteral(\"status\")");
  else
    Fail("Test 1: Simple literal", $"Expected .WithLiteral(\"status\"), got: {code?.Substring(0, Math.Min(200, code?.Length ?? 0))}");
}

// Test 2: Empty default route
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("")]
    public sealed class DefaultRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<DefaultRequest>
      {
        public ValueTask<Unit> Handle(DefaultRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  // Empty route should NOT have any WithLiteral calls for the pattern
  // But it should still have a route constant
  bool hasRouteConstant = code?.Contains("__Route_DefaultRequest") == true;
  bool hasNoLiteralForPattern = code != null && !code.Contains(".WithLiteral(\"\")");

  if (hasRouteConstant && hasNoLiteralForPattern)
    Pass("Test 2: Empty default route has no .WithLiteral() for empty pattern");
  else
    Fail("Test 2: Empty default route", $"hasRouteConstant={hasRouteConstant}, hasNoLiteralForPattern={hasNoLiteralForPattern}");
}

// Test 3: Multi-word literal
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("docker compose up")]
    public sealed class DockerComposeUpRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<DockerComposeUpRequest>
      {
        public ValueTask<Unit> Handle(DockerComposeUpRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasDocker = code?.Contains(".WithLiteral(\"docker\")") == true;
  bool hasCompose = code?.Contains(".WithLiteral(\"compose\")") == true;
  bool hasUp = code?.Contains(".WithLiteral(\"up\")") == true;

  if (hasDocker && hasCompose && hasUp)
    Pass("Test 3: Multi-word literal generates three .WithLiteral() calls");
  else
    Fail("Test 3: Multi-word literal", $"docker={hasDocker}, compose={hasCompose}, up={hasUp}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// PARAMETER GENERATION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 4: Required parameter
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("greet")]
    public sealed class GreetRequest : IRequest
    {
      [Parameter]
      public string Name { get; set; } = string.Empty;
      
      public sealed class Handler : IRequestHandler<GreetRequest>
      {
        public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasParameter = code?.Contains(".WithParameter(\"name\"") == true;
  bool notOptional = code != null && !code.Contains("isOptional: true");

  if (hasParameter && notOptional)
    Pass("Test 4: Required parameter generates .WithParameter(\"name\") without isOptional");
  else
    Fail("Test 4: Required parameter", $"hasParameter={hasParameter}, notOptional={notOptional}");
}

// Test 5: Optional parameter from nullability
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("greet")]
    public sealed class GreetRequest : IRequest
    {
      [Parameter]
      public string? Name { get; set; }
      
      public sealed class Handler : IRequestHandler<GreetRequest>
      {
        public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasOptionalParameter = code?.Contains("isOptional: true") == true;

  if (hasOptionalParameter)
    Pass("Test 5: Nullable parameter generates isOptional: true");
  else
    Fail("Test 5: Optional parameter from nullability", $"Expected isOptional: true in generated code");
}

// Test 6: Typed parameter (int)
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("count")]
    public sealed class CountRequest : IRequest
    {
      [Parameter]
      public int Count { get; set; }
      
      public sealed class Handler : IRequestHandler<CountRequest>
      {
        public ValueTask<Unit> Handle(CountRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasTypedParameter = code?.Contains("type: \"int\"") == true;

  if (hasTypedParameter)
    Pass("Test 6: Int parameter generates type: \"int\"");
  else
    Fail("Test 6: Typed parameter", $"Expected type: \"int\" in generated code");
}

// Test 7: Catch-all parameter
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("exec")]
    public sealed class ExecRequest : IRequest
    {
      [Parameter(IsCatchAll = true)]
      public string[] Args { get; set; } = [];
      
      public sealed class Handler : IRequestHandler<ExecRequest>
      {
        public ValueTask<Unit> Handle(ExecRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasCatchAll = code?.Contains(".WithCatchAll(\"args\"") == true;

  if (hasCatchAll)
    Pass("Test 7: Catch-all parameter generates .WithCatchAll(\"args\")");
  else
    Fail("Test 7: Catch-all parameter", $"Expected .WithCatchAll(\"args\")");
}

// Test 8: Parameter with description
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("greet")]
    public sealed class GreetRequest : IRequest
    {
      [Parameter(Description = "Name to greet")]
      public string Name { get; set; } = string.Empty;
      
      public sealed class Handler : IRequestHandler<GreetRequest>
      {
        public ValueTask<Unit> Handle(GreetRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasDescription = code?.Contains("description: \"Name to greet\"") == true;

  if (hasDescription)
    Pass("Test 8: Parameter description generates description: \"Name to greet\"");
  else
    Fail("Test 8: Parameter with description", $"Expected description: \"Name to greet\"");
}

// ═══════════════════════════════════════════════════════════════════════════════
// OPTION GENERATION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 9: Bool flag option
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("force", "f")]
      public bool Force { get; set; }
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasOption = code?.Contains(".WithOption(\"force\"") == true;
  bool hasShortForm = code?.Contains("shortForm: \"f\"") == true;
  // Bool flags should NOT have expectsValue: true
  bool noExpectsValue = code != null && !code.Contains("expectsValue: true");

  if (hasOption && hasShortForm && noExpectsValue)
    Pass("Test 9: Bool flag generates .WithOption(\"force\", shortForm: \"f\") without expectsValue");
  else
    Fail("Test 9: Bool flag option", $"hasOption={hasOption}, hasShortForm={hasShortForm}, noExpectsValue={noExpectsValue}");
}

// Test 10: Valued option (string)
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("config", "c")]
      public string Config { get; set; } = string.Empty;
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasExpectsValue = code?.Contains("expectsValue: true") == true;

  if (hasExpectsValue)
    Pass("Test 10: String option generates expectsValue: true");
  else
    Fail("Test 10: Valued option", $"Expected expectsValue: true");
}

// Test 11: Optional valued option (nullable string)
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("config", "c")]
      public string? Config { get; set; }
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasParameterIsOptional = code?.Contains("parameterIsOptional: true") == true;

  if (hasParameterIsOptional)
    Pass("Test 11: Nullable option generates parameterIsOptional: true");
  else
    Fail("Test 11: Optional valued option", $"Expected parameterIsOptional: true");
}

// Test 12: Typed valued option (int)
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("replicas", "r")]
      public int Replicas { get; set; } = 1;
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasParameterType = code?.Contains("parameterType: \"int\"") == true;

  if (hasParameterType)
    Pass("Test 12: Int option generates parameterType: \"int\"");
  else
    Fail("Test 12: Typed valued option", $"Expected parameterType: \"int\"");
}

// Test 13: Long form only (no short form)
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("verbose")]
      public bool Verbose { get; set; }
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasOption = code?.Contains(".WithOption(\"verbose\"") == true;
  // Should not have shortForm parameter when not provided
  bool noShortForm = code != null && !code.Contains("shortForm:");

  if (hasOption && noShortForm)
    Pass("Test 13: Long form only generates .WithOption without shortForm");
  else
    Fail("Test 13: Long form only", $"hasOption={hasOption}, noShortForm={noShortForm}");
}

// Test 14: Repeated option
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("build")]
    public sealed class BuildRequest : IRequest
    {
      [Option("include", "I", IsRepeated = true)]
      public string[] Includes { get; set; } = [];
      
      public sealed class Handler : IRequestHandler<BuildRequest>
      {
        public ValueTask<Unit> Handle(BuildRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasRepeated = code?.Contains("isRepeated: true") == true;

  if (hasRepeated)
    Pass("Test 14: Repeated option generates isRepeated: true");
  else
    Fail("Test 14: Repeated option", $"Expected isRepeated: true");
}

// Test 15: Option with description
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Option("force", "f", Description = "Skip confirmation")]
      public bool Force { get; set; }
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasDescription = code?.Contains("description: \"Skip confirmation\"") == true;

  if (hasDescription)
    Pass("Test 15: Option description generates description: \"Skip confirmation\"");
  else
    Fail("Test 15: Option with description", $"Expected description: \"Skip confirmation\"");
}

// ═══════════════════════════════════════════════════════════════════════════════
// GROUP AND ALIAS GENERATION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 16: Group prefix
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRouteGroup("docker")]
    public abstract class DockerRequestBase { }
    
    [NuruRoute("run")]
    public sealed class DockerRunRequest : DockerRequestBase, IRequest
    {
      public sealed class Handler : IRequestHandler<DockerRunRequest>
      {
        public ValueTask<Unit> Handle(DockerRunRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  // Group prefix "docker" should appear as a literal BEFORE "run"
  bool hasDocker = code?.Contains(".WithLiteral(\"docker\")") == true;
  bool hasRun = code?.Contains(".WithLiteral(\"run\")") == true;

  if (hasDocker && hasRun)
    Pass("Test 16: Group prefix generates .WithLiteral(\"docker\") before .WithLiteral(\"run\")");
  else
    Fail("Test 16: Group prefix", $"hasDocker={hasDocker}, hasRun={hasRun}");
}

// Test 17: Group options
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRouteGroup("docker")]
    public abstract class DockerRequestBase
    {
      [GroupOption("debug", "D")]
      public bool Debug { get; set; }
    }
    
    [NuruRoute("run")]
    public sealed class DockerRunRequest : DockerRequestBase, IRequest
    {
      public sealed class Handler : IRequestHandler<DockerRunRequest>
      {
        public ValueTask<Unit> Handle(DockerRunRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasDebugOption = code?.Contains(".WithOption(\"debug\"") == true;
  bool hasShortFormD = code?.Contains("shortForm: \"D\"") == true;

  if (hasDebugOption && hasShortFormD)
    Pass("Test 17: Group option generates .WithOption(\"debug\", shortForm: \"D\")");
  else
    Fail("Test 17: Group options", $"hasDebugOption={hasDebugOption}, hasShortFormD={hasShortFormD}");
}

// Test 17: Alias routes
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("goodbye")]
    [NuruRouteAlias("bye", "cya")]
    public sealed class GoodbyeRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<GoodbyeRequest>
      {
        public ValueTask<Unit> Handle(GoodbyeRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasMainRoute = code?.Contains("__Route_GoodbyeRequest") == true;
  bool hasByeAlias = code?.Contains("__Route_GoodbyeRequest_Alias_bye") == true;
  bool hasCyaAlias = code?.Contains("__Route_GoodbyeRequest_Alias_cya") == true;

  if (hasMainRoute && hasByeAlias && hasCyaAlias)
    Pass("Test 18: Aliases generate separate __Route_*_Alias_* constants");
  else
    Fail("Test 17: Alias routes", $"hasMain={hasMainRoute}, hasBye={hasByeAlias}, hasCya={hasCyaAlias}");
}

// ═══════════════════════════════════════════════════════════════════════════════
// INFRASTRUCTURE GENERATION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 18: Module initializer
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("status")]
    public sealed class StatusRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<StatusRequest>
      {
        public ValueTask<Unit> Handle(StatusRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasModuleInitializer = code?.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]") == true;

  if (hasModuleInitializer)
    Pass("Test 19: Generated code includes [ModuleInitializer] attribute");
  else
    Fail("Test 18: Module initializer", $"Expected [ModuleInitializer] attribute");
}

// Test 20: Pattern string generation
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployRequest : IRequest
    {
      [Parameter]
      public string Env { get; set; } = string.Empty;
      
      [Option("force", "f")]
      public bool Force { get; set; }
      
      public sealed class Handler : IRequestHandler<DeployRequest>
      {
        public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  // Pattern string should contain "deploy {env} --force,-f"
  bool hasPatternString = code?.Contains("__Pattern_DeployRequest") == true;
  bool hasEnvParam = code?.Contains("{env}") == true;
  bool hasForceOption = code?.Contains("--force,-f") == true;

  if (hasPatternString && hasEnvParam && hasForceOption)
    Pass("Test 20: Pattern string contains \"deploy {env} --force,-f\"");
  else
    Fail("Test 20: Pattern string", $"hasPattern={hasPatternString}, hasEnv={hasEnvParam}, hasForce={hasForceOption}");
}

// Test 21: Route description in Register() call
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("status", Description = "Check system status")]
    public sealed class StatusRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<StatusRequest>
      {
        public ValueTask<Unit> Handle(StatusRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasDescriptionInRegister = code?.Contains("\"Check system status\"") == true;

  if (hasDescriptionInRegister)
    Pass("Test 21: Route description included in Register() call");
  else
    Fail("Test 21: Route description", $"Expected \"Check system status\" in Register() call");
}

// ═══════════════════════════════════════════════════════════════════════════════
// SUMMARY
// ═══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");

return failed > 0 ? 1 : 0;
