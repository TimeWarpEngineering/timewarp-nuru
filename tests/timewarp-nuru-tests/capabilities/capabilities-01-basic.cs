#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test CapabilitiesResponse serialization

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

  public static async Task Should_serialize_capabilities_response_to_json()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Description = "A test CLI tool",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "users list",
          Description = "List all users",
          Kind = EndpointKind.Query,
          GroupPath = [],
          Aliases = [],
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
    json.ShouldContain("\"kind\": \"query\"");

    await Task.CompletedTask;
  }

  public static async Task Should_serialize_parameters_in_capabilities_response()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "user create {name}",
          Description = "Create a new user",
          Kind = EndpointKind.Command,
          GroupPath = [],
          Aliases = [],
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
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "users list --format,-f {format}",
          Description = "List users with format",
          Kind = EndpointKind.Query,
          GroupPath = [],
          Aliases = [],
          Parameters = [],
          Options =
          [
            new OptionCapability
            {
              Name = "format",
              Alias = "f",
              Type = "string",
              Required = false
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

  public static async Task Should_serialize_all_endpoint_kinds()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "query-cmd",
          Kind = EndpointKind.Query,
          GroupPath = [],
          Parameters = [],
          Options = []
        },
        new EndpointCapability
        {
          Pattern = "command-cmd",
          Kind = EndpointKind.Command,
          GroupPath = [],
          Parameters = [],
          Options = []
        },
        new EndpointCapability
        {
          Pattern = "idempotent-cmd",
          Kind = EndpointKind.IdempotentCommand,
          GroupPath = [],
          Parameters = [],
          Options = []
        },
        new EndpointCapability
        {
          Pattern = "unspecified-cmd",
          Kind = EndpointKind.Unspecified,
          GroupPath = [],
          Parameters = [],
          Options = []
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert - UseStringEnumConverter with CamelCase policy produces camelCase enum values
    json.ShouldContain("\"kind\": \"query\"");
    json.ShouldContain("\"kind\": \"command\"");
    json.ShouldContain("\"kind\": \"idempotentCommand\"");
    json.ShouldContain("\"kind\": \"unspecified\"");

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
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "test",
          Description = null, // Should be omitted
          Kind = EndpointKind.Query,
          GroupPath = [],
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
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "exec {*args}",
          Kind = EndpointKind.Command,
          GroupPath = [],
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
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "build --tag,-t {tag}*",
          Kind = EndpointKind.Command,
          GroupPath = [],
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

  public static async Task Should_serialize_is_flag_option()
  {
    // Arrange
    CapabilitiesResponse response = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "build --verbose",
          Kind = EndpointKind.Command,
          GroupPath = [],
          Parameters = [],
          Options =
          [
            new OptionCapability
            {
              Name = "verbose",
              Type = "bool",
              Required = false,
              IsFlag = true
            }
          ]
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(response, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    json.ShouldContain("\"isFlag\": true");

    await Task.CompletedTask;
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
