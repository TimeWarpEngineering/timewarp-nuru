#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test HelpProvider displays message type indicators
return await RunTests<MessageTypeHelpOutputTests>(clearCache: true);

[TestTag("MessageType")]
[TestTag("Help")]
[ClearRunfileCache]
public class MessageTypeHelpOutputTests
{
  private static Endpoint CreateEndpoint(string pattern, string? description = null, MessageType messageType = MessageType.Command)
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

  public static async Task Should_display_query_indicator()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("list", "List all items", MessageType.Query));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should contain (Q) indicator for Query
    helpText.ShouldContain("(Q)");
    helpText.ShouldContain("list");

    await Task.CompletedTask;
  }

  public static async Task Should_display_command_indicator()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("create {name}", "Create item", MessageType.Command));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should contain (C) indicator for Command
    helpText.ShouldContain("(C)");

    await Task.CompletedTask;
  }

  public static async Task Should_display_idempotent_indicator()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("set {key} {value}", "Set config value", MessageType.IdempotentCommand));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should contain (I) indicator for IdempotentCommand
    helpText.ShouldContain("(I)");

    await Task.CompletedTask;
  }

  public static async Task Should_display_legend_in_help_output()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("list", "List items", MessageType.Query));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should contain legend explaining indicators
    helpText.ShouldContain("Legend:");
    helpText.ShouldContain("(Q) Query");
    helpText.ShouldContain("(I) Idempotent");
    helpText.ShouldContain("(C) Command");

    await Task.CompletedTask;
  }

  public static async Task Should_display_mixed_message_types()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("list", "List items", MessageType.Query));
    endpoints.Add(CreateEndpoint("create {name}", "Create item", MessageType.Command));
    endpoints.Add(CreateEndpoint("set {key} {value}", "Set config", MessageType.IdempotentCommand));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should contain all three indicators
    helpText.ShouldContain("(Q)");
    helpText.ShouldContain("(C)");
    helpText.ShouldContain("(I)");

    await Task.CompletedTask;
  }

  public static async Task Should_display_colored_indicators_when_color_enabled()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("list", "List items", MessageType.Query));
    endpoints.Add(CreateEndpoint("create {name}", "Create item", MessageType.Command));
    endpoints.Add(CreateEndpoint("set {key}", "Set config", MessageType.IdempotentCommand));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Should contain ANSI color codes
    // Query = Blue, IdempotentCommand = Yellow, Command = Red
    helpText.ShouldContain("\u001b["); // ANSI escape sequence start

    await Task.CompletedTask;
  }
}
