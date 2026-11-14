#region Purpose
// Declares route values whose type constraint NURU_S010 reports, so the generated code still compiles.
#endregion

#region Design
// NURU_S010 stops the build at the route, but Stage B still emits from the model. A value with no
// conversion must still be declared with a type the handler call accepts, or the build also reports
// CS0103 (an undeclared positional) or CS0029 (a string assigned to the handler's type) inside
// generated code (review M1, task 584). The declaration takes the bound handler parameter's type and
// `default!`. A value that no handler parameter binds keeps the raw capture (string? or string[]).
// UnknownTypeConstraintValidator.IsUnresolved decides which values take this path, so the emitters
// and the diagnostic agree.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Declares a positional or catch-all parameter whose constraint has no conversion.
  /// </summary>
  private static void EmitUnresolvedParameter(
    StringBuilder sb,
    RouteDefinition route,
    ParameterDefinition param,
    string varName,
    string uniqueVarName,
    string indentStr)
  {
    string rawType = param.IsCatchAll ? "string[]" : "string?";
    EmitUnresolvedDeclaration(sb, indentStr, param.TypeConstraint!, FindPositionalBinding(route, param), varName, rawType, uniqueVarName);
  }

  /// <summary>
  /// Declares a value or repeated option whose constraint has no conversion.
  /// </summary>
  private static void EmitUnresolvedOption(
    StringBuilder sb,
    RouteDefinition route,
    OptionDefinition option,
    string varName,
    string rawType,
    string rawExpression)
  {
    EmitUnresolvedDeclaration(sb, "      ", option.TypeConstraint!, FindOptionBinding(route, option), varName, rawType, rawExpression);
  }

  private static void EmitUnresolvedDeclaration(
    StringBuilder sb,
    string indentStr,
    string typeConstraint,
    ParameterBinding? handlerParam,
    string varName,
    string rawType,
    string rawExpression)
  {
    sb.AppendLine($"{indentStr}// NURU_S010: no conversion for type constraint '{typeConstraint.TrimEnd('?')}'. The build reports the route;");
    sb.AppendLine($"{indentStr}// this declaration only keeps the generated code compiling.");
    if (handlerParam is not null)
      sb.AppendLine($"{indentStr}{handlerParam.ParameterTypeName} {varName} = default!;");
    else
      sb.AppendLine($"{indentStr}{rawType} {varName} = {rawExpression};");
  }

  // Handler parameters bind route parameters by position (HandlerInvokerEmitter.BuildArgumentListFromRoute).
  private static ParameterBinding? FindPositionalBinding(RouteDefinition route, ParameterDefinition param)
  {
    int routeIndex = -1;
    int index = 0;
    foreach (ParameterDefinition candidate in route.Parameters)
    {
      if (ReferenceEquals(candidate, param))
      {
        routeIndex = index;
        break;
      }

      index++;
    }

    if (routeIndex < 0)
      return null;

    int position = 0;
    foreach (ParameterBinding binding in route.Handler.Parameters)
    {
      if (binding.Source is not (BindingSource.Parameter or BindingSource.CatchAll))
        continue;

      if (position == routeIndex)
        return binding;

      position++;
    }

    return null;
  }

  // Same rule as HandlerInvokerEmitter.BuildArgumentListFromRoute: a handler parameter takes the first
  // option whose parameter name, long form, short form, or camel-cased long form equals its name.
  private static ParameterBinding? FindOptionBinding(RouteDefinition route, OptionDefinition option) =>
    route.Handler.Parameters.FirstOrDefault(binding =>
      binding.Source is BindingSource.Option or BindingSource.Flag &&
      ReferenceEquals(route.Options.FirstOrDefault(candidate => OptionNamesMatch(candidate, binding.ParameterName)), option));

  private static bool OptionNamesMatch(OptionDefinition option, string parameterName) =>
    string.Equals(option.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(option.LongForm, parameterName, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(option.ShortForm, parameterName, StringComparison.OrdinalIgnoreCase) ||
    ToCamelCase(option.LongForm ?? "").Equals(parameterName, StringComparison.OrdinalIgnoreCase);
}
