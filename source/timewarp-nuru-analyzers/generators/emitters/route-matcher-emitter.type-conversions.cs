#region Purpose
// Emits parameter type conversions for routes.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Emits type conversion code for typed parameters.
  /// Pattern matching captures typed params with route-unique names.
  /// This method creates the properly typed variable with the original name.
  /// Uses TryParse for graceful error handling - route skips on conversion failure.
  /// </summary>
  private static void EmitTypeConversions(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    int indent)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterDefinition param in route.Parameters)
    {
      if (!param.HasTypeConstraint)
        continue;

      string varName = param.CamelCaseName;
      string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
      string uniqueVarName = $"__{varName}_{routeIndex}";

      // Normalize type constraint: strip '?' suffix to get base type
      string typeConstraint = param.TypeConstraint ?? "";
      string baseType = typeConstraint.EndsWith('?', StringComparison.Ordinal) ? typeConstraint[..^1] : typeConstraint;

      // Handle catchall parameters differently - they're string[] not string
      if (param.IsCatchAll)
      {
        // Skip string type - catchall is already string[], no conversion needed
        if (baseType.Equals("string", StringComparison.OrdinalIgnoreCase))
          continue;

        EmitCatchAllTypeConversion(sb, param, escapedVarName, uniqueVarName, baseType, indentStr);
        continue;
      }

      // Use shared type conversion mapping with TryParse for graceful error handling
      (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, uniqueVarName, escapedVarName);

      if (conversion is var (clrType, tryParseCondition))
      {
        if (param.IsOptional)
        {
          // Optional param: declare as nullable, check for null before parsing, error on parse failure
          // Need a temp variable for TryParse's out parameter since we need nullable
          string tempVarName = $"__{varName}_parsed_{routeIndex}";
          string tryParseWithTemp = TypeConversionMap.GetBuiltInTryConversion(baseType, uniqueVarName, tempVarName)!.Value.TryParseCondition;
          sb.AppendLine($"{indentStr}{clrType}? {escapedVarName} = null;");
          sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
          sb.AppendLine($"{indentStr}{{");
          sb.AppendLine($"{indentStr}  {clrType} {tempVarName};");
          sb.AppendLine($"{indentStr}  if (!{tryParseWithTemp})");
          sb.AppendLine($"{indentStr}  {{");
          sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: {baseType}\", \"{param.Name}\");");
          sb.AppendLine($"{indentStr}    return 1;");
          sb.AppendLine($"{indentStr}  }}");
          sb.AppendLine($"{indentStr}  {escapedVarName} = {tempVarName};");
          sb.AppendLine($"{indentStr}}}");
        }
        else
        {
          // Required param: TryParse with error message on failure
          sb.AppendLine($"{indentStr}{clrType} {escapedVarName};");
          sb.AppendLine($"{indentStr}if (!{tryParseCondition})");
          sb.AppendLine($"{indentStr}{{");
          sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: {baseType}\", \"{param.Name}\");");
          sb.AppendLine($"{indentStr}  return 1;");
          sb.AppendLine($"{indentStr}}}");
        }
      }
      else
      {
        // Special handling for types without TryParse (Uri, FileInfo, DirectoryInfo)
        // These need different patterns: Uri.TryCreate or constructor with try/catch

        // Check for Uri type - uses TryCreate instead of TryParse
        if (baseType.Equals("uri", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            string tempVarName = $"__{varName}_parsed_{routeIndex}";
            sb.AppendLine($"{indentStr}global::System.Uri? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  if (!global::System.Uri.TryCreate({uniqueVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {tempVarName}))");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: Uri\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  {escapedVarName} = {tempVarName};");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}if (!global::System.Uri.TryCreate({uniqueVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {escapedVarName}) || {escapedVarName} is null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: Uri\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Check for FileInfo type - uses constructor with try/catch
        if (baseType.Equals("fileinfo", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            sb.AppendLine($"{indentStr}global::System.IO.FileInfo? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  try");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    {escapedVarName} = new global::System.IO.FileInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  catch (global::System.Exception)");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: FileInfo\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}global::System.IO.FileInfo {escapedVarName};");
            sb.AppendLine($"{indentStr}try");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.FileInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}}}");
            sb.AppendLine($"{indentStr}catch (global::System.Exception)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: FileInfo\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Check for DirectoryInfo type - uses constructor with try/catch
        if (baseType.Equals("directoryinfo", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            sb.AppendLine($"{indentStr}global::System.IO.DirectoryInfo? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  try");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    {escapedVarName} = new global::System.IO.DirectoryInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  catch (global::System.Exception)");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: DirectoryInfo\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}global::System.IO.DirectoryInfo {escapedVarName};");
            sb.AppendLine($"{indentStr}try");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.DirectoryInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}}}");
            sb.AppendLine($"{indentStr}catch (global::System.Exception)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: DirectoryInfo\", \"{param.Name}\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Try to find a custom type converter for this constraint
        // Match by:
        // 1. Target type name (e.g., "EmailAddress")
        // 2. Simple target type name (e.g., "MyApp.Types.EmailAddress" -> "EmailAddress")
        // 3. Constraint alias (e.g., "email")
        // 4. Convention: ConverterTypeName minus "Converter" suffix (e.g., "EmailAddressConverter" -> "EmailAddress")
        CustomConverterDefinition? converter = customConverters.FirstOrDefault(c =>
          string.Equals(c.TargetTypeName, baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(GetSimpleTypeName(c.TargetTypeName), baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(c.ConstraintAlias, baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(GetTargetTypeFromConverterName(c.ConverterTypeName), baseType, StringComparison.OrdinalIgnoreCase));

        if (converter is not null)
        {
          // Generate custom converter instantiation and TryConvert call
          EmitCustomTypeConversion(sb, param, converter, escapedVarName, uniqueVarName, routeIndex, indentStr);
        }
        else
        {
          // Check if the handler parameter is an enum type
          // Match by lowercase name (route segment name matches handler parameter name)
          ParameterBinding? handlerParam = route.Handler.Parameters
            .FirstOrDefault(p => string.Equals(p.SourceName, param.Name, StringComparison.OrdinalIgnoreCase) && p.IsEnumType);

          if (handlerParam is not null)
          {
            // Generate EnumTypeConverter<T> code for enum types
            EmitEnumTypeConversion(sb, param, handlerParam.ParameterTypeName, escapedVarName, uniqueVarName, routeIndex, indentStr);
          }
          else if (param.ResolvedClrTypeName is not null)
          {
            // No converter found - emit warning comment
            sb.AppendLine(
              $"{indentStr}// WARNING: No converter found for type constraint '{baseType}'");
            sb.AppendLine(
              $"{indentStr}// Register a converter with: builder.AddTypeConverter<YourConverter>();");
          }
        }
      }
    }
  }

  /// <summary>
  /// Emits type conversion code for a typed catch-all parameter.
  /// Catch-all parameters are string[] and need to be converted to typed arrays.
  /// </summary>
  private static void EmitCatchAllTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    string varName,
    string uniqueVarName,
    string baseType,
    string indentStr)
  {
    string errorStatement =
      $"app.WriteArgumentError($\"Error: Invalid value in '{param.Name}'. Expected: {baseType}[]\", \"{param.Name}\");";
    bool converted = EmitBuiltInArrayConversion(
      sb,
      indentStr,
      baseType,
      varName,
      uniqueVarName,
      $"{uniqueVarName}.Length",
      $"{uniqueVarName}_i",
      $"{uniqueVarName}_s",
      $"{uniqueVarName}_parsed",
      errorStatement);
    if (!converted)
    {
      // Unknown type - emit as string[] (no conversion needed, just alias)
      sb.AppendLine($"{indentStr}string[] {varName} = {uniqueVarName};");
    }
  }

  /// <summary>
  /// Emits code for custom type conversion using a registered converter.
  /// Generates converter instantiation, TryConvert call, and error handling.
  /// </summary>
  private static void EmitCustomTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    CustomConverterDefinition converter,
    string escapedVarName,
    string uniqueVarName,
    int routeIndex,
    string indentStr)
  {
    string converterVarName = $"__converter_{param.CamelCaseName}_{routeIndex}";
    string tempVarName = $"__temp_{param.CamelCaseName}_{routeIndex}";

    // Use the converter's target type (resolved via semantic model)
    // For non-generic converters, derive target type from converter name by convention
    string targetType = !string.IsNullOrEmpty(converter.TargetTypeName)
      ? converter.TargetTypeName
      : $"global::{GetTargetTypeFromConverterName(converter.ConverterTypeName)}";

    if (param.IsOptional)
    {
      // Optional param: check for null before conversion
      sb.AppendLine($"{indentStr}{targetType}? {escapedVarName} = null;");
      sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"{indentStr}  if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}  {{");
      sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid {GetSimpleTypeName(targetType)} value for parameter '{param.Name}': '{{{uniqueVarName}}}'\", \"{param.Name}\");");
      sb.AppendLine($"{indentStr}    return 1;");
      sb.AppendLine($"{indentStr}  }}");
      sb.AppendLine($"{indentStr}  {escapedVarName} = ({targetType}){tempVarName}!;");
      sb.AppendLine($"{indentStr}}}");
    }
    else
    {
      // Required param: direct conversion with error handling
      sb.AppendLine($"{indentStr}var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"{indentStr}if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid {GetSimpleTypeName(targetType)} value for parameter '{param.Name}': '{{{uniqueVarName}}}'\", \"{param.Name}\");");
      sb.AppendLine($"{indentStr}  return 1;");
      sb.AppendLine($"{indentStr}}}");
      sb.AppendLine($"{indentStr}{targetType} {escapedVarName} = ({targetType}){tempVarName}!;");
    }
  }

  /// <summary>
  /// Emits code for enum type conversion using EnumTypeConverter.
  /// Generates converter instantiation, TryConvert call, and error handling with valid values.
  /// </summary>
  private static void EmitEnumTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    string enumTypeName,
    string escapedVarName,
    string uniqueVarName,
    int routeIndex,
    string indentStr)
  {
    string converterVarName = $"__enumConverter_{param.CamelCaseName}_{routeIndex}";
    string tempVarName = $"__temp_{param.CamelCaseName}_{routeIndex}";

    // A nullable handler parameter (`Env? env`) arrives as "Env?"; the converter needs the bare enum.
    if (enumTypeName.EndsWith('?', StringComparison.Ordinal))
      enumTypeName = enumTypeName[..^1];

    if (param.IsOptional)
    {
      // Optional param: check for null before conversion
      sb.AppendLine($"{indentStr}{enumTypeName}? {escapedVarName} = null;");
      sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{enumTypeName}>();");
      sb.AppendLine($"{indentStr}  if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}  {{");
      sb.AppendLine($"{indentStr}    app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{param.Name}\");");
      sb.AppendLine($"{indentStr}    return 1;");
      sb.AppendLine($"{indentStr}  }}");
      sb.AppendLine($"{indentStr}  {escapedVarName} = ({enumTypeName}){tempVarName}!;");
      sb.AppendLine($"{indentStr}}}");
    }
    else
    {
      // Required param: direct conversion with error handling
      sb.AppendLine($"{indentStr}var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{enumTypeName}>();");
      sb.AppendLine($"{indentStr}if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  app.WriteArgumentError($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{param.Name}\");");
      sb.AppendLine($"{indentStr}  return 1;");
      sb.AppendLine($"{indentStr}}}");
      sb.AppendLine($"{indentStr}{enumTypeName} {escapedVarName} = ({enumTypeName}){tempVarName}!;");
    }
  }
}
