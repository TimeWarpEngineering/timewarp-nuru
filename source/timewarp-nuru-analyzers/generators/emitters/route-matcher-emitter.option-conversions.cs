#region Purpose
// Emits option value type conversions for routes.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;
using TimeWarp.Nuru.Validation;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Emits type conversion code for a typed option.
  /// Uses TryParse with clear error messages on conversion failure.
  /// </summary>
  private static void EmitOptionTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string varName,
    string rawVarName,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    // NURU_S010 reports a constraint with no conversion; declare the value so the output still compiles
    if (UnknownTypeConstraintValidator.IsUnresolved(route, option, customConverters))
    {
      EmitUnresolvedOption(sb, route, option, varName, "string?", rawVarName);
      return;
    }

    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?', StringComparison.Ordinal) ? typeConstraint[..^1] : typeConstraint;
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Use shared type conversion mapping with TryParse for graceful error handling
    bool isNullableValueType = typeConstraint.EndsWith('?', StringComparison.Ordinal);
    (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, rawVarName, varName);

    if (conversion is var (clrType, tryParseCondition))
    {
      // Handle nullable value types (long?, int?, etc.) - declare as nullable and only parse if value provided
      if (isNullableValueType)
      {
        string tempVarName = $"__{varName}_parsed_{routeIndex}";
        sb.AppendLine($"      {clrType}? {varName} = null;");
        sb.AppendLine($"      if ({rawVarName} is not null)");
        sb.AppendLine("      {");
        sb.AppendLine($"        if ({tryParseCondition.Replace($"out {varName}", $"out {clrType} {tempVarName}", StringComparison.Ordinal)})");
        sb.AppendLine("        {");
        sb.AppendLine($"          {varName} = {tempVarName};");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: {baseType}\", \"{optionDisplay}\");");
        sb.AppendLine("          return 1;");
        sb.AppendLine("        }");
        sb.AppendLine("      }");
      }
      else
      {
        // Non-nullable value types - existing logic
        // Determine the default value to use
        string defaultValue = option.DefaultValueLiteral ?? "default";

        if (option.ParameterIsOptional || option.DefaultValueLiteral is not null)
        {
          // Optional option value OR has default: declare with default, error only if value provided but invalid
          sb.AppendLine($"      {clrType} {varName} = {defaultValue};");
          sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: {baseType}\", \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }
        else
        {
          // Required option value (no default): TryParse with error message on failure
          sb.AppendLine($"      {clrType} {varName} = default;");
          sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: {baseType}\", \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }
      }
    }
    else
    {
      // Special handling for types without TryParse (Uri, FileInfo, DirectoryInfo)
      // These need different patterns: Uri.TryCreate or constructor with try/catch

      // Check for Uri type - uses TryCreate instead of TryParse
      if (baseType.Equals("uri", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          string tempVarName = $"__{varName}_parsed_{routeIndex}";
          sb.AppendLine($"      global::System.Uri? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        if (!global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {tempVarName}))");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: Uri\", \"{optionDisplay}\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine($"        {varName} = {tempVarName};");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      global::System.Uri? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null && !global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out {varName}))");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: Uri\", \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Check for FileInfo type - uses constructor with try/catch
      if (baseType.Equals("fileinfo", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          sb.AppendLine($"      global::System.IO.FileInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine("        try");
          sb.AppendLine("        {");
          sb.AppendLine($"          {varName} = new global::System.IO.FileInfo({rawVarName});");
          sb.AppendLine("        }");
          sb.AppendLine("        catch (global::System.Exception)");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: FileInfo\", \"{optionDisplay}\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      global::System.IO.FileInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Missing value for option '{optionDisplay}'. Expected: FileInfo\", global::TimeWarp.Nuru.NuruBindErrorKind.MissingValue, \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
          sb.AppendLine("      try");
          sb.AppendLine("      {");
          sb.AppendLine($"        {varName} = new global::System.IO.FileInfo({rawVarName});");
          sb.AppendLine("      }");
          sb.AppendLine("      catch (global::System.Exception)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: FileInfo\", \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Check for DirectoryInfo type - uses constructor with try/catch
      if (baseType.Equals("directoryinfo", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          sb.AppendLine($"      global::System.IO.DirectoryInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine("        try");
          sb.AppendLine("        {");
          sb.AppendLine($"          {varName} = new global::System.IO.DirectoryInfo({rawVarName});");
          sb.AppendLine("        }");
          sb.AppendLine("        catch (global::System.Exception)");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: DirectoryInfo\", \"{optionDisplay}\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      global::System.IO.DirectoryInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Missing value for option '{optionDisplay}'. Expected: DirectoryInfo\", global::TimeWarp.Nuru.NuruBindErrorKind.MissingValue, \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
          sb.AppendLine("      try");
          sb.AppendLine("      {");
          sb.AppendLine($"        {varName} = new global::System.IO.DirectoryInfo({rawVarName});");
          sb.AppendLine("      }");
          sb.AppendLine("      catch (global::System.Exception)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: DirectoryInfo\", \"{optionDisplay}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Try to find a custom type converter for this constraint
      CustomConverterDefinition? converter = CustomConverterMatcher.Find(baseType, customConverters);

      if (converter is not null)
      {
        // Generate custom converter code for option
        EmitOptionCustomTypeConversion(sb, option, converter, varName, rawVarName, routeIndex);
      }
      else
      {
        // Check if the handler parameter is an enum type
        // Match by option name (LongForm or ShortForm)
        string optionName = option.LongForm ?? option.ShortForm!;
        ParameterBinding? handlerParam = route.Handler.Parameters
          .FirstOrDefault(p =>
            string.Equals(p.SourceName, optionName, StringComparison.OrdinalIgnoreCase) &&
            p.IsEnumType);

        if (handlerParam is not null)
        {
          // Generate EnumTypeConverter<T> code for enum types
          EmitOptionEnumTypeConversion(sb, option, handlerParam.ParameterTypeName, varName, rawVarName, routeIndex);
        }
        else
        {
          // Unknown type - keep as string (fallback)
          sb.AppendLine(
            $"      string? {varName} = {rawVarName};");
        }
      }
    }
  }

  /// <summary>
  /// Emits custom type conversion code for a typed option.
  /// </summary>
  private static void EmitOptionCustomTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    CustomConverterDefinition converter,
    string varName,
    string rawVarName,
    int routeIndex)
  {
    string converterVarName = $"__converter_{varName}_{routeIndex}";
    string tempVarName = $"__temp_{varName}_{routeIndex}";

    // Use the converter's target type (resolved via semantic model)
    // For non-generic converters, derive target type from converter name by convention
    string targetType = !string.IsNullOrEmpty(converter.TargetTypeName)
      ? converter.TargetTypeName
      : $"global::{GetTargetTypeFromConverterName(converter.ConverterTypeName)}";

    if (option.ParameterIsOptional)
    {
      // Optional option: check for null before conversion
      sb.AppendLine($"      {targetType}? {varName} = null;");
      sb.AppendLine($"      if ({rawVarName} is not null)");
      sb.AppendLine("      {");
      sb.AppendLine($"        var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"        if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid {targetType} value for option '{option.LongForm ?? option.ShortForm}': '{{{rawVarName}}}'\", \"{option.LongForm ?? option.ShortForm}\");");
      sb.AppendLine("          return 1;");
      sb.AppendLine("        }");
      sb.AppendLine($"        {varName} = ({targetType}){tempVarName}!;");
      sb.AppendLine("      }");
    }
    else
    {
      // Required option: direct conversion with error handling
      sb.AppendLine($"      var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"      object? {tempVarName} = null;");
      sb.AppendLine($"      if ({rawVarName} is not null && !{converterVarName}.TryConvert({rawVarName}, out {tempVarName}))");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid {targetType} value for option '{option.LongForm ?? option.ShortForm}': '{{{rawVarName}}}'\", \"{option.LongForm ?? option.ShortForm}\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      sb.AppendLine($"      {targetType} {varName} = {tempVarName} is not null ? ({targetType}){tempVarName} : default;");
    }
  }

  /// <summary>
  /// Emits enum type conversion code for a typed option.
  /// Uses EnumTypeConverter with helpful error messages showing valid values.
  /// </summary>
  private static void EmitOptionEnumTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string enumTypeName,
    string varName,
    string rawVarName,
    int routeIndex)
  {
    string converterVarName = $"__enumConverter_{varName}_{routeIndex}";
    string tempVarName = $"__temp_{varName}_{routeIndex}";
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Strip nullable suffix from the type name for the converter and casts
    // EnumTypeConverter<T> requires the non-nullable enum type
    string baseEnumType = enumTypeName.EndsWith('?', StringComparison.Ordinal) ? enumTypeName[..^1] : enumTypeName;

    if (option.ParameterIsOptional)
    {
      // Nullable enum: omission keeps the initializer (null when there is none).
      sb.AppendLine($"      {baseEnumType}? {varName} = {option.DefaultValueLiteral ?? "null"};");
      sb.AppendLine($"      if ({rawVarName} is not null)");
      sb.AppendLine("      {");
      sb.AppendLine($"        var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
      sb.AppendLine($"        if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{optionDisplay}\");");
      sb.AppendLine("          return 1;");
      sb.AppendLine("        }");
      sb.AppendLine($"        {varName} = ({baseEnumType}){tempVarName}!;");
      sb.AppendLine("      }");
    }
    else if (option.DefaultValueLiteral is not null)
    {
      // Non-nullable enum with an initializer. Omission keeps that initializer.
      sb.AppendLine($"      {baseEnumType} {varName} = {option.DefaultValueLiteral};");
      sb.AppendLine($"      if ({rawVarName} is not null)");
      sb.AppendLine("      {");
      sb.AppendLine($"        var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
      sb.AppendLine($"        if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{optionDisplay}\");");
      sb.AppendLine("          return 1;");
      sb.AppendLine("        }");
      sb.AppendLine($"        {varName} = ({baseEnumType}){tempVarName}!;");
      sb.AppendLine("      }");
    }
    else
    {
      // Required option: direct conversion with error handling
      sb.AppendLine($"      var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
      sb.AppendLine($"      object? {tempVarName} = null;");
      sb.AppendLine($"      if ({rawVarName} is not null && !{converterVarName}.TryConvert({rawVarName}, out {tempVarName}))");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.WriteArgumentError($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{optionDisplay}\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      sb.AppendLine($"      {baseEnumType} {varName} = {tempVarName} is not null ? ({baseEnumType}){tempVarName} : default;");
    }
  }
}
