#region Purpose
// Builds an endpoint instance so an omitted option keeps the property initializer.
#endregion

#region Design
// default! means the generator cannot copy the initializer. Capabilities omit it,
// and the object initializer omits it until the caller supplies a value, so the
// declared initializer runs. Init-only members stay inside the initializer.
// Settable members are assigned after construction. A type with no parameterless
// constructor is new(args) for constructor parameters that match bound properties.
// On --json-args the supplied test is __supplied_{id}. That flag is declared in
// the route case and assigned inside the bind attempt, where __av_ and __jsonMap
// are still in scope.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits <c>new</c> for an endpoint command, including primary-constructor arguments.
/// </summary>
internal static class CommandConstructionEmitter
{
  private readonly record struct Member(string Property, string Variable, string Supplied, bool InitOnly);

  internal static void Emit(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string indent,
    bool jsonArgsPath)
  {
    HandlerDefinition handler = route.Handler;
    string commandTypeName = handler.FullTypeName ?? "UnknownCommand";
    HashSet<string> constructorProperties = new(StringComparer.Ordinal);
    List<string> constructorLocals = [];
    if (!handler.ConstructorArguments.IsDefaultOrEmpty)
    {
      foreach (string propertyName in handler.ConstructorArguments)
      {
        ParameterBinding? binding = handler.RouteParameters.FirstOrDefault(candidate =>
          string.Equals(candidate.ParameterName, propertyName, StringComparison.Ordinal));
        if (binding is null)
          continue;

        constructorProperties.Add(propertyName);
        constructorLocals.Add(LocalName(binding, routeIndex));
      }
    }

    List<Member> always = [];
    List<Member> conditional = [];
    List<string> discards = [];
    foreach (ParameterBinding param in handler.RouteParameters)
    {
      if (constructorProperties.Contains(param.ParameterName))
        continue;

      string property = ToPascalCase(param.ParameterName);
      string variable = LocalName(param, routeIndex);
      OptionDefinition? option = MatchOption(route, param);
      if (option?.DefaultValueLiteral == EndpointExtractor.UnemittableDefaultLiteral)
      {
        string supplied = Supplied(route, option, routeIndex, jsonArgsPath);
        if (supplied == "false")
        {
          discards.Add(variable);
          continue;
        }

        conditional.Add(new Member(property, variable, supplied, option.IsInitOnly));
        continue;
      }

      always.Add(new Member(property, variable, "true", false));
    }

    List<Member> initConditional = [.. conditional.Where(member => member.InitOnly)];
    List<Member> setConditional = [.. conditional.Where(member => !member.InitOnly)];
    sb.AppendLine($"{indent}// Create command instance with bound properties");
    if (initConditional.Count == 0)
    {
      EmitOne(sb, indent, commandTypeName, constructorLocals, always, declare: true);
    }
    else
    {
      sb.AppendLine($"{indent}{commandTypeName} __command;");
      int combinations = 1 << initConditional.Count;
      for (int mask = combinations - 1; mask >= 0; mask--)
      {
        bool last = mask == 0;
        string keyword = mask == combinations - 1 ? "if" : last ? "else" : "else if";
        List<string> terms = [];
        List<Member> included = [.. always];
        for (int index = 0; index < initConditional.Count; index++)
        {
          bool on = (mask & (1 << index)) != 0;
          Member member = initConditional[index];
          if (!last)
            terms.Add(on ? member.Supplied : Negate(member.Supplied));

          if (on)
            included.Add(member);
        }

        if (last)
          sb.AppendLine($"{indent}{keyword}");
        else
          sb.AppendLine($"{indent}{keyword} ({string.Join(" && ", terms)})");

        EmitOne(sb, indent, commandTypeName, constructorLocals, included, declare: false);
      }
    }

    foreach (Member member in setConditional)
    {
      sb.AppendLine($"{indent}if ({member.Supplied})");
      sb.AppendLine($"{indent}  __command.{member.Property} = {member.Variable};");
    }

    foreach (string discard in discards)
      sb.AppendLine($"{indent}_ = {discard};");

    sb.AppendLine();
  }

  private static void EmitOne(
    StringBuilder sb,
    string indent,
    string commandTypeName,
    List<string> constructorLocals,
    List<Member> members,
    bool declare)
  {
    string arguments = string.Join(", ", constructorLocals);
    string target = declare ? $"{commandTypeName} __command = " : "__command = ";
    if (members.Count == 0)
    {
      sb.AppendLine($"{indent}{target}new({arguments});");
      return;
    }

    sb.AppendLine($"{indent}{target}new({arguments})");
    sb.AppendLine($"{indent}{{");
    foreach (Member member in members)
      sb.AppendLine($"{indent}  {member.Property} = {member.Variable},");

    sb.AppendLine($"{indent}}};");
  }

  private static string Supplied(RouteDefinition route, OptionDefinition option, int routeIndex, bool jsonArgsPath)
  {
    if (jsonArgsPath)
    {
      string id = JsonArgsEmitter.OptionSlotId(route, option);
      return $"__supplied_{id}";
    }

    if (option.IsJsonValue)
      return "false";

    string rawName = option.ParameterName ?? CSharpIdentifierUtils.ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    if (option.IsFlag)
      return CSharpIdentifierUtils.EscapeIfKeyword(CSharpIdentifierUtils.ToCamelCase(option.LongForm ?? "flag"));

    if (option.IsRepeated)
      return $"__{rawName}_list_{routeIndex}.Count > 0";

    return $"__{rawName}_flagFound_{routeIndex}";
  }

  private static OptionDefinition? MatchOption(RouteDefinition route, ParameterBinding param)
  {
    if (param.Source is not (BindingSource.Option or BindingSource.Flag))
      return null;

    foreach (OptionDefinition option in route.Options)
    {
      if (string.Equals(option.LongForm, param.SourceName, StringComparison.OrdinalIgnoreCase)
          || string.Equals(option.ParameterName, param.ParameterName, StringComparison.OrdinalIgnoreCase))
      {
        return option;
      }
    }

    return null;
  }

  private static string LocalName(ParameterBinding param, int routeIndex) =>
    param.Source switch
    {
      BindingSource.CatchAll => $"__{CSharpIdentifierUtils.ToCamelCase(param.ParameterName)}_{routeIndex}",
      BindingSource.Flag => CSharpIdentifierUtils.EscapeIfKeyword(CSharpIdentifierUtils.ToCamelCase(param.SourceName)),
      _ => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant())
    };

  private static string Negate(string expression) => $"!({expression})";

  private static string ToPascalCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    return char.ToUpperInvariant(value[0]) + value[1..];
  }
}
