#!/usr/bin/env -S dotnet --
#pragma warning disable CA1062 // Handlers trust the mediator request instance
#pragma warning disable IDE1006 // camelCase members are the MCP parameter names on purpose
#pragma warning disable CA1805 // `= false` is the flag initializer under test
#pragma warning disable CA1002 // List<T> response members are the STJ collision under test
#pragma warning disable CA2227 // Response members are settable DTO properties
#pragma warning disable CA1819 // A JSON member is an array on purpose

// [Operation] requests whose members are declared in camelCase (task 508-006).
// The generator must write each member as declared on the argv, --json-args and TryBind paths.
// Two operations that each nest a type named Response (with their own Item and List<Item>)
// must build without SYSLIB1031. Nested --json-args members match ignoring case.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto.CamelCase
{
using System.Text.Json;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

[TestTag("Auto")]
public class OperationCamelCaseMemberTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationCamelCaseMemberTests>();

  // Build() and RunAsync must share a field so the generator records the intercept sites.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T5086CamelQuery>()
      .Map<T5086GetSymbol.Query>()
      .Map<T5086GetMembers.Query>()
      .Map<T5086MultiQuery>()
      .Build();
  }

  public static async Task Should_bind_camel_case_members_from_argv()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
      ["t5086camel", "--solutionId", "s1", "--symbolName", "Foo", "--maxDepth", "3", "--checkOnly", "--class", "k"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("solutionId:s1|symbolName:Foo|maxDepth:3|checkOnly:True|class:k").ShouldBeTrue();
  }

  public static async Task Should_keep_camel_case_initializers_when_members_are_omitted()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t5086camel", "--solutionId", "s1"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("solutionId:s1|symbolName:plain|maxDepth:41|checkOnly:False|class:<null>").ShouldBeTrue();
  }

  public static async Task Should_bind_camel_case_members_from_json_args()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
      ["t5086camel", "--json-args", """{"solutionId":"s2","maxDepth":7,"checkOnly":true}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("solutionId:s2|symbolName:plain|maxDepth:7|checkOnly:True|class:<null>").ShouldBeTrue();
  }

  public static async Task Should_bind_camel_case_members_through_try_bind()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    using JsonDocument doc = JsonDocument.Parse("""{"solutionId":"s3","symbolName":"Bar","class":"c"}""");
    App.TryBind("t5086camel", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error)
      .ShouldBeTrue(error?.Message);
    object? result = await App.InvokeAsync(request);

    result.ShouldBe("solutionId:s3|symbolName:Bar|maxDepth:41|checkOnly:False|class:c");
  }

  public static async Task Should_bind_nested_json_members_ignoring_case()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
      ["t5086multi", "--json-args", """{"operations":[{"tool":"get_symbol","arguments":"a"},{"Tool":"find","Arguments":"b"}]}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("get_symbol(a);find(b)").ShouldBeTrue();
  }

  public static async Task Should_serialize_two_nested_response_types_that_share_a_name()
  {
    using TestTerminal symbol = new();
    CreateApp(symbol);
    (await App.RunAsync(["t5086symbol", "--symbolName", "Foo"])).ShouldBe(0);
    symbol.OutputContains("\"name\":\"Foo\"").ShouldBeTrue();
    symbol.OutputContains("\"kind\":\"method\"").ShouldBeTrue();

    using TestTerminal members = new();
    CreateApp(members);
    (await App.RunAsync(["t5086members", "--typeName", "Bar"])).ShouldBe(0);
    members.OutputContains("\"count\":2").ShouldBeTrue();
    members.OutputContains("\"order\":5").ShouldBeTrue();
  }
}

public static class T5086Seed
{
  public static int Next() => 41;
}

[Operation("t5086camel", Description = "camelCase members")]
public sealed class T5086CamelQuery : IQuery<string>
{
  public required string solutionId { get; set; }

  public string symbolName { get; set; } = "plain";

  // Not copyable, so the generator reads it from a defaults instance.
  public int maxDepth { get; init; } = T5086Seed.Next();

  public bool checkOnly { get; set; } = false;

  public string? @class { get; set; }

  public sealed class Handler : IQueryHandler<T5086CamelQuery, string>
  {
    public Task<string> Handle(T5086CamelQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(
        $"solutionId:{query.solutionId}|symbolName:{query.symbolName}|maxDepth:{query.maxDepth.ToString(CultureInfo.InvariantCulture)}"
        + $"|checkOnly:{query.checkOnly}|class:{query.@class ?? "<null>"}");
    }
  }
}

[Operation("t5086multi")]
public sealed class T5086MultiQuery : IQuery<string>
{
  public T5086Operation[] operations { get; set; } = [];

  public sealed class Handler : IQueryHandler<T5086MultiQuery, string>
  {
    public Task<string> Handle(T5086MultiQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(string.Join(";", query.operations.Select(operation => $"{operation.Tool}({operation.Arguments})")));
    }
  }
}

public sealed class T5086Operation
{
  public string Tool { get; set; } = "";

  public string Arguments { get; set; } = "";
}

public static class T5086GetSymbol
{
  [Operation("t5086symbol")]
  public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
  {
    public required string symbolName { get; set; }

    public sealed class Handler : IQueryHandler<Query, OneOf<Response, SharedProblemDetails>>
    {
      public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        OneOf<Response, SharedProblemDetails> response = new Response { Name = query.symbolName, Items = [new Item { Kind = "method" }] };
        return Task.FromResult(response);
      }
    }
  }

  public sealed class Response
  {
    public string Name { get; set; } = "";

    public List<Item> Items { get; set; } = [];
  }

  public sealed class Item
  {
    public string Kind { get; set; } = "";
  }
}

public static class T5086GetMembers
{
  [Operation("t5086members")]
  public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
  {
    public required string typeName { get; set; }

    public sealed class Handler : IQueryHandler<Query, OneOf<Response, SharedProblemDetails>>
    {
      public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        OneOf<Response, SharedProblemDetails> response = new Response { Type = query.typeName, Count = 2, Items = [new Item { Order = 5 }] };
        return Task.FromResult(response);
      }
    }
  }

  public sealed class Response
  {
    public string Type { get; set; } = "";

    public int Count { get; set; }

    public List<Item> Items { get; set; } = [];
  }

  public sealed class Item
  {
    public int Order { get; set; }
  }
}
}
