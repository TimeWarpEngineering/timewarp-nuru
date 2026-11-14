#region Purpose
// Emits JSON value assignment and conversion code.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{

  /// <param name="patternLiteral">The route pattern, already escaped for a regular string literal.</param>
  private static void EmitAssignFromJson(StringBuilder sb, Slot slot, string element, Fail fail, string patternLiteral, int indent)
  {
    string ind = new(' ', indent);
    if (slot.IsNullable)
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = null;");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitAssignFromJsonCore(sb, slot, element, fail, patternLiteral, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
    else
    {
      EmitAssignFromJsonCore(sb, slot, element, fail, patternLiteral, indent);
    }
  }

  private static void EmitAssignFromJsonCore(StringBuilder sb, Slot slot, string element, Fail fail, string patternLiteral, int indent)
  {
    string ind = new(' ', indent);
    switch (slot.Shape)
    {
      case ValueShape.Bool:
        sb.AppendLine($"{ind}{slot.VariableName} = {element}.GetBoolean();");
        break;

      case ValueShape.Number:
        string clr = slot.NumberClr ?? "int";
        string call = TryGetCall(slot, element, $"{clr} __num_{slot.Id}") ?? "false";
        sb.AppendLine($"{ind}if (!{call})");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}else");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  {slot.VariableName} = __num_{slot.Id};");
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Text:
      case ValueShape.Enum:
        sb.AppendLine($"{ind}string? __raw_{slot.Id} = {element}.GetString();");
        EmitAssignScalarFromString(sb, slot, $"__raw_{slot.Id}", fail, indent);
        break;

      case ValueShape.TextList:
      case ValueShape.NumberList:
      case ValueShape.EnumList:
        sb.AppendLine($"{ind}global::System.Collections.Generic.List<string> __items_{slot.Id} = new();");
        sb.AppendLine($"{ind}foreach (global::System.Text.Json.JsonElement __item in {element}.EnumerateArray())");
        sb.AppendLine($"{ind}{{");
        if (slot.Shape == ValueShape.NumberList)
        {
          string itemClr = slot.NumberClr ?? "int";
          sb.AppendLine($"{ind}  {itemClr} __itemNum;");
          string itemCall = TryGetCall(slot, "__item", "__itemNum") ?? "false";
          sb.AppendLine($"{ind}  if (!{itemCall})");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __ok = false;");
          sb.AppendLine($"{ind}    {fail.Assign()}");
          sb.AppendLine($"{ind}    break;");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__itemNum.ToString(global::System.Globalization.CultureInfo.InvariantCulture));");
        }
        else
        {
          if (IsBoolName(slot.BaseConstraint))
          {
            sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__item.ValueKind == global::System.Text.Json.JsonValueKind.True ? \"true\" : \"false\");");
          }
          else
          {
            sb.AppendLine($"{ind}  __items_{slot.Id}.Add(__item.GetString() ?? \"\");");
          }
        }

        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}if (__ok)");
        sb.AppendLine($"{ind}{{");
        EmitAssignListFromStrings(sb, slot, $"__items_{slot.Id}", fail, indent + 2);
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Json:
        // A value that fails inside the member (an unknown enum name, a nested type mismatch, an ID
        // that does not parse) is a bind error for that member, never a crash or a handler fault.
        string jsonType = slot.DeclaredType ?? "object";
        sb.AppendLine($"{ind}try");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  {slot.VariableName} = ({jsonType})global::System.Text.Json.JsonSerializer.Deserialize({element}, typeof({jsonType}), global::TimeWarp.Nuru.Generated.NuruJsonMemberContext.Default)!;");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}catch (global::System.Exception __jx_{slot.Id}) when (global::TimeWarp.Nuru.NuruBindError.IsJsonMemberFailure(__jx_{slot.Id}))");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  __fail ??= global::TimeWarp.Nuru.NuruBindError.ForJsonMember({fail.KeyExpression}, \"{patternLiteral}\", __jx_{slot.Id});");
        sb.AppendLine($"{ind}}}");
        break;
    }
  }

  private static void EmitAssignScalarFromString(StringBuilder sb, Slot slot, string raw, Fail fail, int indent)
  {
    string ind = new(' ', indent);
    if (slot.Unresolved)
    {
      EmitAssignUnresolved(sb, slot, ind);
      return;
    }

    if (slot.Shape == ValueShape.Enum && slot.EnumTypeName is not null)
    {
      sb.AppendLine($"{ind}global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enum_{slot.Id} = new();");
      sb.AppendLine($"{ind}if (!__enum_{slot.Id}.TryConvert({raw}, out object? __enumObj_{slot.Id}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      string validValues = $"\" \" + __enum_{slot.Id}.GetValidValuesMessage()";
      sb.AppendLine($"{ind}  {fail.Assign(validValues)}");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = ({slot.EnumTypeName})__enumObj_{slot.Id}!;");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.Converter is not null)
    {
      sb.AppendLine($"{ind}{slot.Converter.ConverterTypeName} __conv_{slot.Id} = new();");
      sb.AppendLine($"{ind}if ({raw} is null || !__conv_{slot.Id}.TryConvert({raw}, out object? __convObj_{slot.Id}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  {fail.Assign()}");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = ({slot.DeclaredType ?? slot.Converter.TargetTypeName})__convObj_{slot.Id}!;");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.BaseConstraint is not null && IsSpecialParse(slot.BaseConstraint))
    {
      EmitSpecialParse(sb, slot, raw, fail, indent);
      return;
    }

    (string ClrType, string TryParseCondition)? builtin = slot.BaseConstraint is null
      ? null
      : TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, raw, $"__parsed_{slot.Id}");
    if (builtin is not null && slot.NumberClr is null && !IsPlainString(slot))
    {
      (string clr, string condition) = builtin.Value;
      sb.AppendLine($"{ind}{clr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}if ({raw} is null || !({condition}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  {fail.Assign()}");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      return;
    }

    if (slot.Shape == ValueShape.Number && slot.BaseConstraint is not null)
    {
      (string clr, string condition) = TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, raw, $"__parsed_{slot.Id}")!.Value;
      sb.AppendLine($"{ind}{clr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}if ({raw} is null || !({condition}))");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  {fail.Assign()}");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  {slot.VariableName} = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      return;
    }

    sb.AppendLine($"{ind}{slot.VariableName} = {raw}!;");
  }

  private static void EmitAssignListFromStrings(StringBuilder sb, Slot slot, string list, Fail fail, int indent)
  {
    string ind = new(' ', indent);
    if (slot.Unresolved)
    {
      EmitAssignUnresolved(sb, slot, ind);
      return;
    }

    if (slot.Shape == ValueShape.TextList && IsPlainString(slot))
    {
      sb.AppendLine($"{ind}{slot.VariableName} = [.. {list}];");
      return;
    }

    if (slot.Shape == ValueShape.EnumList && slot.EnumTypeName is not null)
    {
      sb.AppendLine($"{ind}global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumList_{slot.Id} = new();");
      sb.AppendLine($"{ind}{slot.EnumTypeName}[] __enumArr_{slot.Id} = new {slot.EnumTypeName}[{list}.Count];");
      sb.AppendLine($"{ind}for (int __ei = 0; __ei < {list}.Count; __ei++)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  if (!__enumList_{slot.Id}.TryConvert({list}[__ei], out object? __enumObj))");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      string validValues = $"\" \" + __enumList_{slot.Id}.GetValidValuesMessage()";
      sb.AppendLine($"{ind}    {fail.Assign(validValues)}");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  __enumArr_{slot.Id}[__ei] = ({slot.EnumTypeName})__enumObj!;");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __enumArr_{slot.Id};");
      return;
    }

    if (slot.BaseConstraint is not null)
    {
      (string ClrType, string TryParseCondition)? typed = TypeConversionMap.GetBuiltInTryConversion(
        slot.BaseConstraint,
        $"{list}[__ni]",
        $"__parsed_{slot.Id}");
      if (typed is var (clr, condition))
      {
        sb.AppendLine($"{ind}{clr}[] __typed_{slot.Id} = new {clr}[{list}.Count];");
        sb.AppendLine($"{ind}for (int __ni = 0; __ni < {list}.Count; __ni++)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  {clr} __parsed_{slot.Id};");
        sb.AppendLine($"{ind}  if (!({condition}))");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    __ok = false;");
        sb.AppendLine($"{ind}    {fail.Assign()}");
        sb.AppendLine($"{ind}    break;");
        sb.AppendLine($"{ind}  }}");
        sb.AppendLine($"{ind}  __typed_{slot.Id}[__ni] = __parsed_{slot.Id};");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __typed_{slot.Id};");
        return;
      }
    }

    if (slot.Shape == ValueShape.NumberList && slot.BaseConstraint is not null && slot.NumberClr is not null)
    {
      sb.AppendLine($"{ind}{slot.NumberClr}[] __numArr_{slot.Id} = new {slot.NumberClr}[{list}.Count];");
      sb.AppendLine($"{ind}for (int __ni = 0; __ni < {list}.Count; __ni++)");
      sb.AppendLine($"{ind}{{");
      (string clr, string condition) = TypeConversionMap.GetBuiltInTryConversion(slot.BaseConstraint, $"{list}[__ni]", $"__parsed_{slot.Id}")!.Value;
      _ = clr;
      sb.AppendLine($"{ind}  {slot.NumberClr} __parsed_{slot.Id};");
      sb.AppendLine($"{ind}  if (!({condition}))");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    {fail.Assign()}");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  __numArr_{slot.Id}[__ni] = __parsed_{slot.Id};");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}if (__ok) {slot.VariableName} = __numArr_{slot.Id};");
      return;
    }

    sb.AppendLine($"{ind}{slot.VariableName} = [.. {list}];");
  }

  // NURU_S010 reports this constraint and stops the build. Assign default rather than a string so the
  // field (the handler's type) still compiles and NURU_S010 is the only error (review M1, task 584).
  private static void EmitAssignUnresolved(StringBuilder sb, Slot slot, string ind)
  {
    sb.AppendLine($"{ind}// NURU_S010: no conversion for type constraint '{slot.BaseConstraint}'.");
    sb.AppendLine($"{ind}{slot.VariableName} = default!;");
  }

  private static void EmitSpecialParse(StringBuilder sb, Slot slot, string raw, Fail fail, int indent)
  {
    string ind = new(' ', indent);
    string constraint = slot.BaseConstraint!.ToLowerInvariant();
    sb.AppendLine($"{ind}if ({raw} is null)");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __ok = false;");
    sb.AppendLine($"{ind}  {fail.Assign()}");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else");
    sb.AppendLine($"{ind}{{");
    if (constraint == "uri")
    {
      sb.AppendLine($"{ind}  if (!global::System.Uri.TryCreate({raw}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? __uri_{slot.Id}) || __uri_{slot.Id} is null)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    {fail.Assign()}");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  else");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = __uri_{slot.Id};");
      sb.AppendLine($"{ind}  }}");
    }
    else if (constraint == "fileinfo")
    {
      sb.AppendLine($"{ind}  try");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = new global::System.IO.FileInfo({raw});");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  catch (global::System.Exception)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    {fail.Assign()}");
      sb.AppendLine($"{ind}  }}");
    }
    else
    {
      sb.AppendLine($"{ind}  try");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    {slot.VariableName} = new global::System.IO.DirectoryInfo({raw});");
      sb.AppendLine($"{ind}  }}");
      sb.AppendLine($"{ind}  catch (global::System.Exception)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    {fail.Assign()}");
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }
}
