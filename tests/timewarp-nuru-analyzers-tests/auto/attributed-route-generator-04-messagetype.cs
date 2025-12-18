#!/usr/bin/dotnet run

// ═══════════════════════════════════════════════════════════════════════════════
// ATTRIBUTED ROUTE GENERATOR TESTS - MessageType Detection
// ═══════════════════════════════════════════════════════════════════════════════
// Tests that verify the NuruAttributedRouteGenerator correctly detects
// IQuery<T>, ICommand<T>, IIdempotent interfaces and emits appropriate
// WithMessageType() calls in the generated source code.
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
// MESSAGE TYPE DETECTION TESTS
// ═══════════════════════════════════════════════════════════════════════════════

// Test 1: IQuery<T> → MessageType.Query
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("list")]
    public sealed class ListQuery : IQuery<Unit>
    {
      public sealed class Handler : IQueryHandler<ListQuery, Unit>
      {
        public ValueTask<Unit> Handle(ListQuery query, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasQueryMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Query)") == true;

  if (hasQueryMessageType)
    Pass("Test 1: IQuery<T> generates .WithMessageType(MessageType.Query)");
  else
    Fail("Test 1: IQuery<T>", $"Expected .WithMessageType(MessageType.Query) in generated code. Got: {code?.Substring(0, Math.Min(500, code?.Length ?? 0))}");
}

// Test 2: ICommand<T> → MessageType.Command
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("deploy")]
    public sealed class DeployCommand : ICommand<Unit>
    {
      public sealed class Handler : ICommandHandler<DeployCommand, Unit>
      {
        public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasCommandMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Command)") == true;

  if (hasCommandMessageType)
    Pass("Test 2: ICommand<T> generates .WithMessageType(MessageType.Command)");
  else
    Fail("Test 2: ICommand<T>", $"Expected .WithMessageType(MessageType.Command)");
}

// Test 3: ICommand<T> + IIdempotent → MessageType.IdempotentCommand
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("set")]
    public sealed class SetCommand : ICommand<Unit>, IIdempotent
    {
      public sealed class Handler : ICommandHandler<SetCommand, Unit>
      {
        public ValueTask<Unit> Handle(SetCommand command, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasIdempotentMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.IdempotentCommand)") == true;

  if (hasIdempotentMessageType)
    Pass("Test 3: ICommand<T> + IIdempotent generates .WithMessageType(MessageType.IdempotentCommand)");
  else
    Fail("Test 3: ICommand<T> + IIdempotent", $"Expected .WithMessageType(MessageType.IdempotentCommand)");
}

// Test 4: IRequest<T> → MessageType.Unspecified
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("ping")]
    public sealed class PingRequest : IRequest<Unit>
    {
      public sealed class Handler : IRequestHandler<PingRequest, Unit>
      {
        public ValueTask<Unit> Handle(PingRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasUnspecifiedMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Unspecified)") == true;

  if (hasUnspecifiedMessageType)
    Pass("Test 4: IRequest<T> generates .WithMessageType(MessageType.Unspecified)");
  else
    Fail("Test 4: IRequest<T>", $"Expected .WithMessageType(MessageType.Unspecified)");
}

// Test 5: IRequest (non-generic) → MessageType.Unspecified
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("ping")]
    public sealed class PingRequest : IRequest
    {
      public sealed class Handler : IRequestHandler<PingRequest>
      {
        public ValueTask<Unit> Handle(PingRequest request, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasUnspecifiedMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Unspecified)") == true;

  if (hasUnspecifiedMessageType)
    Pass("Test 5: IRequest (non-generic) generates .WithMessageType(MessageType.Unspecified)");
  else
    Fail("Test 5: IRequest (non-generic)", $"Expected .WithMessageType(MessageType.Unspecified)");
}

// Test 6: No mediator interface → MessageType.Unspecified
{
  const string source = """
    using TimeWarp.Nuru;
    
    [NuruRoute("status")]
    public sealed class StatusRequest
    {
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasUnspecifiedMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Unspecified)") == true;

  if (hasUnspecifiedMessageType)
    Pass("Test 6: No mediator interface generates .WithMessageType(MessageType.Unspecified)");
  else
    Fail("Test 6: No mediator interface", $"Expected .WithMessageType(MessageType.Unspecified)");
}

// Test 7: Alias also gets same MessageType
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRoute("list")]
    [NuruRouteAlias("ls")]
    public sealed class ListQuery : IQuery<Unit>
    {
      public sealed class Handler : IQueryHandler<ListQuery, Unit>
      {
        public ValueTask<Unit> Handle(ListQuery query, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  // Both main route and alias should have Query MessageType
  // Count occurrences of WithMessageType(Query)
  int queryCount = 0;
  if (code != null)
  {
    int index = 0;
    while ((index = code.IndexOf(".WithMessageType(global::TimeWarp.Nuru.MessageType.Query)", index)) != -1)
    {
      queryCount++;
      index++;
    }
  }

  if (queryCount == 2)
    Pass("Test 7: Alias route also gets same MessageType (2 Query occurrences)");
  else
    Fail("Test 7: Alias MessageType", $"Expected 2 Query MessageTypes, found {queryCount}");
}

// Test 8: Group route with ICommand
{
  const string source = """
    using TimeWarp.Nuru;
    using Mediator;
    
    [NuruRouteGroup("docker")]
    public abstract class DockerRequestBase { }
    
    [NuruRoute("run")]
    public sealed class DockerRunCommand : DockerRequestBase, ICommand<Unit>
    {
      public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
      {
        public ValueTask<Unit> Handle(DockerRunCommand command, CancellationToken ct) => default;
      }
    }
    """;

  GeneratorDriverRunResult result = AttributedRouteTestHelpers.RunAttributedRouteGenerator(source);
  string? code = AttributedRouteTestHelpers.GetGeneratedAttributedRoutesSource(result);

  bool hasCommandMessageType = code?.Contains(".WithMessageType(global::TimeWarp.Nuru.MessageType.Command)") == true;

  if (hasCommandMessageType)
    Pass("Test 8: Group route with ICommand generates .WithMessageType(MessageType.Command)");
  else
    Fail("Test 8: Group route with ICommand", $"Expected .WithMessageType(MessageType.Command)");
}

// ═══════════════════════════════════════════════════════════════════════════════
// SUMMARY
// ═══════════════════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine($"Results: {passed} passed, {failed} failed");

return failed > 0 ? 1 : 0;
