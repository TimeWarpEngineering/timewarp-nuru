#!/usr/bin/env -S dotnet --
#region Purpose
// Classifies every Roslynk [McpServerTool] parameter against Nuru --json-args shapes.
#endregion

#region Design
// Nuru binds bool, number, string, enum, and arrays of those. It has no object, map, or
// array-of-object shape. CancellationToken is a host argument and is not an input.
// Token estimates use the common four-characters-per-token rule. The live --capabilities
// measurement is separate and uses the same rule on the generated document.
#endregion

using System.ComponentModel;
using System.Reflection;
using System.Text;
using ModelContextProtocol.Server;

Assembly assembly = typeof(Morris.Roslynk.ServicesRegistration).Assembly;
List<ToolRow> tools = [];

foreach (Type type in assembly.GetTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
{
  if (type.GetCustomAttribute<McpServerToolTypeAttribute>() is null)
  {
    continue;
  }

  foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
  {
    McpServerToolAttribute? tool = method.GetCustomAttribute<McpServerToolAttribute>();
    if (tool is null)
    {
      continue;
    }

    string name = tool.Name ?? method.Name;
    string description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
    List<ParameterRow> parameters = [];
    foreach (ParameterInfo parameter in method.GetParameters())
    {
      parameters.Add(Classify(parameter));
    }

    tools.Add(new ToolRow(name, description, parameters));
  }
}

tools.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));

int clean = 0;
int blocked = 0;
int host = 0;
Console.WriteLine($"tools={tools.Count}");
foreach (ToolRow tool in tools)
{
  Console.WriteLine($"tool={tool.Name} descriptionChars={tool.Description.Length}");
  foreach (ParameterRow parameter in tool.Parameters)
  {
    Console.WriteLine($"  {parameter.Name} type={parameter.Type} shape={parameter.Shape} maps={parameter.Maps}");
    switch (parameter.Maps)
    {
      case "clean":
        clean++;
        break;
      case "host":
        host++;
        break;
      default:
        blocked++;
        break;
    }
  }
}

Console.WriteLine($"parameters.clean={clean}");
Console.WriteLine($"parameters.blocked={blocked}");
Console.WriteLine($"parameters.host={host}");

string synthetic = BuildSyntheticCatalog(tools);
Console.WriteLine($"syntheticCapabilities.chars={synthetic.Length}");
Console.WriteLine($"syntheticCapabilities.tokensEst={EstimateTokens(synthetic)}");
string syntheticTwo = BuildSyntheticCatalog(tools.Where(static tool => tool.Name is "get_diagnostics" or "find_references").ToList());
Console.WriteLine($"syntheticTwo.chars={syntheticTwo.Length}");
Console.WriteLine($"syntheticTwo.tokensEst={EstimateTokens(syntheticTwo)}");
int descriptionChars = tools.Sum(static tool => tool.Description.Length);
Console.WriteLine($"descriptionChars={descriptionChars}");
Console.WriteLine($"descriptionTokensEst={EstimateTokens(new string('x', descriptionChars))}");

static int EstimateTokens(string text) => (text.Length + 3) / 4;

static ParameterRow Classify(ParameterInfo parameter)
{
  Type type = parameter.ParameterType;
  string typeName = type.ToString();
  string description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "";
  if (type == typeof(CancellationToken))
  {
    return new ParameterRow(parameter.Name ?? "", typeName, "cancellation", "host", description);
  }

  Type core = Nullable.GetUnderlyingType(type) ?? type;
  if (IsScalar(core))
  {
    return new ParameterRow(parameter.Name ?? "", typeName, ShapeName(core), "clean", description);
  }

  if (core.IsEnum)
  {
    return new ParameterRow(parameter.Name ?? "", typeName, "enum", "clean", description);
  }

  if (TryGetSequenceElement(core, out Type? element))
  {
    Type item = Nullable.GetUnderlyingType(element) ?? element;
    if (IsScalar(item) || item.IsEnum)
    {
      string itemShape = item.IsEnum ? "enum" : ShapeName(item);
      return new ParameterRow(parameter.Name ?? "", typeName, itemShape + "[]", "clean", description);
    }

    return new ParameterRow(parameter.Name ?? "", typeName, "object[]", "blocked", description);
  }

  if (IsMap(core))
  {
    return new ParameterRow(parameter.Name ?? "", typeName, "object", "blocked", description);
  }

  if (core.IsClass || core.IsValueType)
  {
    return new ParameterRow(parameter.Name ?? "", typeName, "object", "blocked", description);
  }

  return new ParameterRow(parameter.Name ?? "", typeName, "unknown", "blocked", description);
}

static bool IsScalar(Type type) =>
  type == typeof(string)
  || type == typeof(bool)
  || type == typeof(int)
  || type == typeof(long)
  || type == typeof(short)
  || type == typeof(byte)
  || type == typeof(double)
  || type == typeof(float)
  || type == typeof(decimal)
  || type == typeof(uint)
  || type == typeof(ulong);

static string ShapeName(Type type)
{
  if (type == typeof(string))
  {
    return "string";
  }

  if (type == typeof(bool))
  {
    return "bool";
  }

  return "number";
}

static bool TryGetSequenceElement(Type type, out Type element)
{
  if (type.IsArray)
  {
    element = type.GetElementType()!;
    return true;
  }

  if (type.IsGenericType)
  {
    Type definition = type.GetGenericTypeDefinition();
    if (definition == typeof(IReadOnlyList<>)
      || definition == typeof(List<>)
      || definition == typeof(IList<>)
      || definition == typeof(IEnumerable<>)
      || definition == typeof(ICollection<>))
    {
      element = type.GetGenericArguments()[0];
      return true;
    }
  }

  element = typeof(void);
  return false;
}

static bool IsMap(Type type)
{
  if (!type.IsGenericType)
  {
    return false;
  }

  Type definition = type.GetGenericTypeDefinition();
  return definition == typeof(IReadOnlyDictionary<,>)
    || definition == typeof(Dictionary<,>)
    || definition == typeof(IDictionary<,>);
}

static string BuildSyntheticCatalog(List<ToolRow> tools)
{
  List<SyntheticEndpoint> endpoints = [];
  foreach (ToolRow tool in tools)
  {
    List<SyntheticSlot> parameters = [];
    List<SyntheticSlot> options = [];
    foreach (ParameterRow parameter in tool.Parameters)
    {
      if (parameter.Maps == "host")
      {
        continue;
      }

      SyntheticSlot slot = new(parameter.Name, parameter.Shape, parameter.Maps, parameter.Description);
      if (parameter.Name is "solutionId" or "symbolName" or "solutionPath")
      {
        parameters.Add(slot);
      }
      else
      {
        options.Add(slot);
      }
    }

    endpoints.Add(new SyntheticEndpoint(tool.Name, tool.Description, parameters, options));
  }

  StringBuilder json = new();
  json.Append("{\"name\":\"roslynk-nuru\",\"endpoints\":[");
  for (int index = 0; index < endpoints.Count; index++)
  {
    if (index > 0)
    {
      json.Append(',');
    }

    SyntheticEndpoint endpoint = endpoints[index];
    json.Append("{\"pattern\":");
    AppendJson(json, endpoint.Pattern);
    json.Append(",\"description\":");
    AppendJson(json, endpoint.Description);
    json.Append(",\"parameters\":");
    AppendSlots(json, endpoint.Parameters);
    json.Append(",\"options\":");
    AppendSlots(json, endpoint.Options);
    json.Append('}');
  }

  json.Append("]}");
  return json.ToString();
}

static void AppendSlots(StringBuilder json, List<SyntheticSlot> slots)
{
  json.Append('[');
  for (int index = 0; index < slots.Count; index++)
  {
    if (index > 0)
    {
      json.Append(',');
    }

    SyntheticSlot slot = slots[index];
    json.Append("{\"name\":");
    AppendJson(json, slot.Name);
    json.Append(",\"type\":");
    AppendJson(json, slot.Type);
    json.Append(",\"maps\":");
    AppendJson(json, slot.Maps);
    json.Append(",\"description\":");
    AppendJson(json, slot.Description);
    json.Append('}');
  }

  json.Append(']');
}

static void AppendJson(StringBuilder json, string value)
{
  json.Append('"');
  foreach (char character in value)
  {
    switch (character)
    {
      case '"':
        json.Append("\\\"");
        break;
      case '\\':
        json.Append("\\\\");
        break;
      case '\n':
        json.Append("\\n");
        break;
      case '\r':
        json.Append("\\r");
        break;
      case '\t':
        json.Append("\\t");
        break;
      default:
        if (character < 0x20)
        {
          json.Append("\\u");
          json.Append(((int)character).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
          json.Append(character);
        }

        break;
    }
  }

  json.Append('"');
}

internal sealed record ToolRow(string Name, string Description, List<ParameterRow> Parameters);

internal sealed record ParameterRow(string Name, string Type, string Shape, string Maps, string Description);

internal sealed record SyntheticEndpoint(
  string Pattern,
  string Description,
  List<SyntheticSlot> Parameters,
  List<SyntheticSlot> Options);

internal sealed record SyntheticSlot(string Name, string Type, string Maps, string Description);
