#region Purpose
// Builds an endpoint instance so an omitted option keeps the property initializer.
#endregion

#region Design
// default! means the generator cannot copy the initializer. Capabilities omit it,
// and the object initializer leaves it out until the caller supplies a value, so the
// declared initializer runs. A required member is always in the object initializer:
// the route already rejects omission, and C# requires it there (CS9035).
// A settable member is assigned after construction when supplied. An init-only member
// reads a second instance built only for its defaults: P = supplied ? p : __commandDefaults.P.
// One extra instance keeps the emitted code linear in the number of such members.
// A type with no parameterless constructor is new(args) for constructor parameters
// that match bound properties.
// A member is written as declared (ParameterName holds the property identifier), so a
// camelCase contract property such as solutionId is assigned as solutionId. Locals stay
// lowercase because the route matcher declares them that way.
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
  private const string DefaultsVariable = "__commandDefaults";

  private readonly record struct Member(string Property, string Value, bool Required);

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

    List<Member> initialized = [];
    List<(string Property, string Variable, string Supplied)> assignedAfter = [];
    List<string> discards = [];
    bool needsDefaults = false;
    foreach (ParameterBinding param in handler.RouteParameters)
    {
      if (constructorProperties.Contains(param.ParameterName))
        continue;

      string property = CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);
      string variable = LocalName(param, routeIndex);
      OptionDefinition? option = OptionBindingMatcher.Match(route, param);
      bool required = option?.IsRequiredMember == true;
      if (option?.DefaultValueLiteral != EndpointExtractor.UnemittableDefaultLiteral || required)
      {
        initialized.Add(new Member(property, variable, required));
        continue;
      }

      string supplied = Supplied(route, option, routeIndex, jsonArgsPath);
      if (supplied == "false")
      {
        discards.Add(variable);
        continue;
      }

      if (option.IsInitOnly)
      {
        initialized.Add(new Member(property, $"{supplied} ? {variable} : {DefaultsVariable}.{property}", false));
        needsDefaults = true;
        continue;
      }

      assignedAfter.Add((property, variable, supplied));
    }

    sb.AppendLine($"{indent}// Create command instance with bound properties");
    if (needsDefaults)
    {
      sb.AppendLine($"{indent}// Declared initializers for init-only members the caller did not supply");
      EmitOne(sb, indent, commandTypeName, DefaultsVariable, constructorLocals, [.. initialized.Where(member => member.Required)]);
    }

    EmitOne(sb, indent, commandTypeName, "__command", constructorLocals, initialized);

    foreach ((string property, string variable, string supplied) in assignedAfter)
    {
      sb.AppendLine($"{indent}if ({supplied})");
      sb.AppendLine($"{indent}  __command.{property} = {variable};");
    }

    foreach (string discard in discards)
      sb.AppendLine($"{indent}_ = {discard};");

    sb.AppendLine();
  }

  private static void EmitOne(
    StringBuilder sb,
    string indent,
    string commandTypeName,
    string variable,
    List<string> constructorLocals,
    List<Member> members)
  {
    string arguments = string.Join(", ", constructorLocals);
    string target = $"{commandTypeName} {variable} = ";
    if (members.Count == 0)
    {
      sb.AppendLine($"{indent}{target}new({arguments});");
      return;
    }

    sb.AppendLine($"{indent}{target}new({arguments})");
    sb.AppendLine($"{indent}{{");
    foreach (Member member in members)
      sb.AppendLine($"{indent}  {member.Property} = {member.Value},");

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

  private static string LocalName(ParameterBinding param, int routeIndex) =>
    param.Source switch
    {
      BindingSource.CatchAll => $"__{CSharpIdentifierUtils.ToCamelCase(param.ParameterName)}_{routeIndex}",
      BindingSource.Flag => CSharpIdentifierUtils.EscapeIfKeyword(CSharpIdentifierUtils.ToCamelCase(param.SourceName)),
      _ => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant())
    };
}
