#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test MessageType fluent API for routes

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.MessageType
{

[TestTag("MessageType")]
public class MessageTypeFluentApiTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MessageTypeFluentApiTests>();

  private static Endpoint CreateEndpoint(string pattern, string? description = null, TimeWarp.Nuru.MessageType messageType = TimeWarp.Nuru.MessageType.Command)
  {
    CompiledRoute route = PatternParser.Parse(pattern);
    route.MessageType = messageType;

    return new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = route,
      Handler = () => 0,
      Description = description,
      MessageType = messageType
    };
  }

  public static async Task Should_default_message_type_to_command()
  {
    // Arrange - Create endpoint with default message type
    Endpoint endpoint = CreateEndpoint("create {name}", "Create something");

    // Assert - Default should be Command
    endpoint.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Command);
    endpoint.CompiledRoute.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Command);

    await Task.CompletedTask;
  }

  public static async Task Should_support_query_message_type()
  {
    // Arrange - Create endpoint with Query message type
    Endpoint endpoint = CreateEndpoint("list", "List items", TimeWarp.Nuru.MessageType.Query);

    // Assert
    endpoint.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Query);
    endpoint.CompiledRoute.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Query);

    await Task.CompletedTask;
  }

  public static async Task Should_support_idempotent_command_message_type()
  {
    // Arrange - Create endpoint with IdempotentCommand message type
    Endpoint endpoint = CreateEndpoint("set {key} {value}", "Set config", TimeWarp.Nuru.MessageType.IdempotentCommand);

    // Assert
    endpoint.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.IdempotentCommand);
    endpoint.CompiledRoute.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.IdempotentCommand);

    await Task.CompletedTask;
  }

  public static async Task Route_configurator_as_query_should_set_message_type()
  {
    // Arrange - Simulate what happens when AsQuery is called
    Endpoint endpoint = CreateEndpoint("users", "List users");

    // Act - Simulate EndpointBuilder.AsQuery() behavior
    endpoint.MessageType = TimeWarp.Nuru.MessageType.Query;
    endpoint.CompiledRoute.MessageType = TimeWarp.Nuru.MessageType.Query;

    // Assert
    endpoint.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Query);
    endpoint.CompiledRoute.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Query);

    await Task.CompletedTask;
  }

  public static async Task Route_configurator_as_idempotent_should_set_message_type()
  {
    // Arrange - Simulate what happens when AsIdempotentCommand is called
    Endpoint endpoint = CreateEndpoint("config set {key}", "Set config");

    // Act - Simulate EndpointBuilder.AsIdempotentCommand() behavior
    endpoint.MessageType = TimeWarp.Nuru.MessageType.IdempotentCommand;
    endpoint.CompiledRoute.MessageType = TimeWarp.Nuru.MessageType.IdempotentCommand;

    // Assert
    endpoint.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.IdempotentCommand);
    endpoint.CompiledRoute.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.IdempotentCommand);

    await Task.CompletedTask;
  }

  public static async Task Compiled_route_builder_should_support_message_type()
  {
    // Arrange & Act - Test CompiledRouteBuilder.WithMessageType
    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder()
      .WithLiteral("test")
      .WithMessageType(TimeWarp.Nuru.MessageType.Query)
      .Build();

    // Assert
    route.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Query);

    await Task.CompletedTask;
  }

  public static async Task Compiled_route_builder_should_default_to_command()
  {
    // Arrange & Act - Test default message type
    CompiledRoute route = new TimeWarp.Nuru.CompiledRouteBuilder()
      .WithLiteral("test")
      .Build();

    // Assert - Default should be Command
    route.MessageType.ShouldBe(TimeWarp.Nuru.MessageType.Command);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.MessageType
