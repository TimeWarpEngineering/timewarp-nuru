#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test --capabilities route and CapabilitiesResponse serialization

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.Capabilities
{

[TestTag("Capabilities")]
public class CapabilitiesBasicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesBasicTests>();

  public static async Task Should_hide_capabilities_route_from_help()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("--capabilities", "Machine-readable metadata"));

    HelpOptions options = new();

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    helpText.ShouldNotContain("--capabilities");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_capabilities_response_to_json()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Description = "A test CLI tool",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "users list",
          Description = "List all users",
          MessageType = "query",
          Parameters = [],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"name\": \"mytool\"");
    json.ShouldContain("\"version\": \"1.0.0\"");
    json.ShouldContain("\"description\": \"A test CLI tool\"");
    json.ShouldContain("\"pattern\": \"users list\"");
    json.ShouldContain("\"messageType\": \"query\"");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_parameters_in_capabilities_response()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "user create {name}",
          Description = "Create a new user",
          MessageType = "command",
          Parameters =
          [
            new ParameterCapability
            {
              Name = "name",
              Type = "string",
              Required = true,
              Description = "User name"
            }
          ],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"name\": \"name\"");
    json.ShouldContain("\"type\": \"string\"");
    json.ShouldContain("\"required\": true");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_options_in_capabilities_response()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "users list --format,-f {format}",
          Description = "List users with format",
          MessageType = "query",
          Parameters = [],
          Options =
          [
            new OptionCapability
            {
              Name = "format",
              Alias = "f",
              Type = "string",
              Required = false,
              Description = "Output format"
            }
          ]
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"name\": \"format\"");
    json.ShouldContain("\"alias\": \"f\"");
    json.ShouldContain("\"type\": \"string\"");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_all_message_types()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "query-cmd",
          MessageType = "query",
          Parameters = [],
          Options = []
        },
        new CommandCapability
        {
          Pattern = "command-cmd",
          MessageType = "command",
          Parameters = [],
          Options = []
        },
        new CommandCapability
        {
          Pattern = "idempotent-cmd",
          MessageType = "idempotent-command",
          Parameters = [],
          Options = []
        },
        new CommandCapability
        {
          Pattern = "unspecified-cmd",
          MessageType = "unspecified",
          Parameters = [],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"messageType\": \"query\"");
    json.ShouldContain("\"messageType\": \"command\"");
    json.ShouldContain("\"messageType\": \"idempotent-command\"");
    json.ShouldContain("\"messageType\": \"unspecified\"");

    await Task.CompletedTask;
  }

  public static async Task Should_omit_null_description_in_json()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Description = null, // Should be omitted
      Commands =
      [
        new CommandCapability
        {
          Pattern = "test",
          Description = null, // Should be omitted
          MessageType = "query",
          Parameters = [],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert - description should not appear when null
    int descCount = CountOccurrences(json, "\"description\"");
    descCount.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_catchall_parameter()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "exec {*args}",
          MessageType = "command",
          Parameters =
          [
            new ParameterCapability
            {
              Name = "args",
              Type = "string",
              Required = true,
              IsCatchAll = true
            }
          ],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"isCatchAll\": true");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_repeated_option()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Commands =
      [
        new CommandCapability
        {
          Pattern = "build --tag,-t {tag}*",
          MessageType = "command",
          Parameters = [],
          Options =
          [
            new OptionCapability
            {
              Name = "tag",
              Alias = "t",
              Type = "string",
              Required = false,
              IsRepeated = true
            }
          ]
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"isRepeated\": true");

    await Task.CompletedTask;
  }

  private static Endpoint CreateEndpoint(string pattern, string? description = null)
  {
    return new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern),
      Handler = () => 0,
      Description = description
    };
  }

  private static int CountOccurrences(string text, string pattern)
  {
    int count = 0;
    int index = 0;
    while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
    {
      count++;
      index += pattern.Length;
    }

    return count;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.Capabilities
