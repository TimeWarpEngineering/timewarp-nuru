#region Purpose
// Emits JSON unknown-key, kind-gate, required-option, and fill code.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{
  private static void EmitUnknownKeys(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string known = slots.Count == 0
      ? "(none)"
      : string.Join(", ", slots.Select(s => s.Key).Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal));
    known = ForInterpolation(Escape(known));
    Fail unknown = new("UnknownKey", "__prop.Key", $"$\"Error: Unknown key '{{__prop.Key}}' for '{pattern}'. Known names: {known}.\"");
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    if (slots.Count == 0)
    {
      sb.AppendLine($"{ind}  foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Text.Json.JsonElement> __prop in __jsonMap)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    __ok = false;");
      sb.AppendLine($"{ind}    {unknown.Assign()}");
      sb.AppendLine($"{ind}    break;");
      sb.AppendLine($"{ind}  }}");
    }
    else
    {
      string arms = string.Join(" or ", slots.Select(s => s.Key).Distinct(StringComparer.Ordinal).Select(k => $"\"{Escape(k)}\""));
      sb.AppendLine($"{ind}  foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Text.Json.JsonElement> __prop in __jsonMap)");
      sb.AppendLine($"{ind}  {{");
      sb.AppendLine($"{ind}    if (__prop.Key is not ({arms}))");
      sb.AppendLine($"{ind}    {{");
      sb.AppendLine($"{ind}      __ok = false;");
      sb.AppendLine($"{ind}      {unknown.Assign()}");
      sb.AppendLine($"{ind}      break;");
      sb.AppendLine($"{ind}    }}");
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitKindGates(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    foreach (Slot slot in slots)
    {
      sb.AppendLine($"{ind}  if (__ok && !__av_{slot.Id} && __jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __el_{slot.Id}))");
      sb.AppendLine($"{ind}  {{");
      EmitKindGate(sb, slot, pattern, indent + 4);
      sb.AppendLine($"{ind}  }}");
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitKindGate(StringBuilder sb, Slot slot, string pattern, int indent)
  {
    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string element = $"__el_{slot.Id}";
    string expected = Escape(slot.ExpectedType);
    Fail fail = new("WrongType", KeyLiteral(slot.Key), $"$\"Error: Key '{Escape(slot.Key)}' for '{pattern}' has the wrong JSON type. Expected {expected}.\"");
    if (slot.IsNullable)
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitKindGateCore(sb, slot, element, fail, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
    else
    {
      sb.AppendLine($"{ind}if ({element}.ValueKind == global::System.Text.Json.JsonValueKind.Null)");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  __ok = false;");
      sb.AppendLine($"{ind}  {fail.Assign()}");
      sb.AppendLine($"{ind}}}");
      sb.AppendLine($"{ind}else");
      sb.AppendLine($"{ind}{{");
      EmitKindGateCore(sb, slot, element, fail, indent + 2);
      sb.AppendLine($"{ind}}}");
    }
  }

  private static void EmitKindGateCore(StringBuilder sb, Slot slot, string element, Fail fail, int indent)
  {
    string ind = new(' ', indent);
    switch (slot.Shape)
    {
      case ValueShape.Bool:
        sb.AppendLine($"{ind}if ({element}.ValueKind is not (global::System.Text.Json.JsonValueKind.True or global::System.Text.Json.JsonValueKind.False))");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Number:
        string tryGet = TryGetCall(slot, element, "global::System.Index _unused") ?? "false";
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{TryGetDiscard(slot, element)})");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        _ = tryGet;
        break;

      case ValueShape.Text:
      case ValueShape.Enum:
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.String)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        if (slot.Shape == ValueShape.Enum && slot.EnumTypeName is not null)
        {
          sb.AppendLine($"{ind}else");
          sb.AppendLine($"{ind}{{");
          sb.AppendLine($"{ind}  global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumGate_{slot.Id} = new();");
          sb.AppendLine($"{ind}  if (!__enumGate_{slot.Id}.TryConvert({element}.GetString(), out _))");
          sb.AppendLine($"{ind}  {{");
          sb.AppendLine($"{ind}    __ok = false;");
          string validValues = $"\" \" + __enumGate_{slot.Id}.GetValidValuesMessage()";
          sb.AppendLine($"{ind}    {fail.Assign(validValues)}");
          sb.AppendLine($"{ind}  }}");
          sb.AppendLine($"{ind}}}");
        }

        break;

      case ValueShape.TextList:
      case ValueShape.NumberList:
      case ValueShape.EnumList:
        sb.AppendLine($"{ind}if ({element}.ValueKind != global::System.Text.Json.JsonValueKind.Array)");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        sb.AppendLine($"{ind}else");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  foreach (global::System.Text.Json.JsonElement __item in {element}.EnumerateArray())");
        sb.AppendLine($"{ind}  {{");
        if (IsBoolName(slot.BaseConstraint))
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind is not (global::System.Text.Json.JsonValueKind.True or global::System.Text.Json.JsonValueKind.False))");
        }
        else if (slot.Shape == ValueShape.TextList || slot.Shape == ValueShape.EnumList)
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind != global::System.Text.Json.JsonValueKind.String)");
        }
        else
        {
          sb.AppendLine($"{ind}    if (__item.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{TryGetDiscard(slot, "__item")})");
        }

        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}      __ok = false;");
        sb.AppendLine($"{ind}      {fail.Assign()}");
        sb.AppendLine($"{ind}      break;");
        sb.AppendLine($"{ind}    }}");
        if (slot.Shape == ValueShape.EnumList && slot.EnumTypeName is not null)
        {
          sb.AppendLine($"{ind}    else");
          sb.AppendLine($"{ind}    {{");
          sb.AppendLine($"{ind}      global::TimeWarp.Nuru.EnumTypeConverter<{slot.EnumTypeName}> __enumItem_{slot.Id} = new();");
          sb.AppendLine($"{ind}      if (!__enumItem_{slot.Id}.TryConvert(__item.GetString(), out _))");
          sb.AppendLine($"{ind}      {{");
          sb.AppendLine($"{ind}        __ok = false;");
          string validValues = $"\" \" + __enumItem_{slot.Id}.GetValidValuesMessage()";
          sb.AppendLine($"{ind}        {fail.Assign(validValues)}");
          sb.AppendLine($"{ind}        break;");
          sb.AppendLine($"{ind}      }}");
          sb.AppendLine($"{ind}    }}");
        }

        sb.AppendLine($"{ind}  }}");
        sb.AppendLine($"{ind}}}");
        break;

      case ValueShape.Json:
        // A typed ID member is written as a JSON string by its own converter.
        string accepted = slot.Option?.IsJsonStringValue == true
          ? "global::System.Text.Json.JsonValueKind.String"
          : "(global::System.Text.Json.JsonValueKind.Object or global::System.Text.Json.JsonValueKind.Array)";
        sb.AppendLine($"{ind}if ({element}.ValueKind is not {accepted})");
        sb.AppendLine($"{ind}{{");
        sb.AppendLine($"{ind}  __ok = false;");
        sb.AppendLine($"{ind}  {fail.Assign()}");
        sb.AppendLine($"{ind}}}");
        break;
    }
  }

  private static void EmitRequiredOptions(StringBuilder sb, List<Slot> slots, string pattern, int indent)
  {
    string ind = new(' ', indent);
    sb.AppendLine($"{ind}if (__ok && __litOk)");
    sb.AppendLine($"{ind}{{");
    foreach (Slot slot in slots.Where(s => s.IsOption))
    {
      if (slot.RequiredFlag)
      {
        sb.AppendLine($"{ind}  if (!__av_{slot.Id})");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    if (!__jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __flagEl_{slot.Id}) || __flagEl_{slot.Id}.ValueKind != global::System.Text.Json.JsonValueKind.True)");
        sb.AppendLine($"{ind}    {{");
        sb.AppendLine($"{ind}      __ok = false;");
        sb.AppendLine($"{ind}      {MissingValue(slot, pattern).Assign()}");
        sb.AppendLine($"{ind}    }}");
        sb.AppendLine($"{ind}  }}");
      }
      else if (slot.Required)
      {
        sb.AppendLine($"{ind}  if (!__av_{slot.Id} && !__jsonMap.ContainsKey(\"{Escape(slot.Key)}\"))");
        sb.AppendLine($"{ind}  {{");
        sb.AppendLine($"{ind}    __ok = false;");
        sb.AppendLine($"{ind}    {MissingValue(slot, pattern).Assign()}");
        sb.AppendLine($"{ind}  }}");
      }
    }

    sb.AppendLine($"{ind}}}");
  }

  private static void EmitFill(StringBuilder sb, Slot slot, string pattern, int indent)
  {
    if (slot.VariableName is null)
      return;

    string patternLiteral = pattern;
    pattern = ForInterpolation(pattern);
    string ind = new(' ', indent);
    string expected = Escape(slot.ExpectedType);
    Fail jsonFail = new("WrongType", KeyLiteral(slot.Key), $"$\"Error: Key '{Escape(slot.Key)}' for '{pattern}' has the wrong JSON type. Expected {expected}.\"");
    Fail argvFail = new("InvalidValue", KeyLiteral(slot.Key), $"$\"Error: Invalid value for '{Escape(slot.Key)}' on '{pattern}'. Expected {expected}.\"");
    sb.AppendLine($"{ind}if (__av_{slot.Id})");
    sb.AppendLine($"{ind}{{");
    if (slot.Shape == ValueShape.Json)
    {
      string form = slot.Option?.LongForm ?? slot.Key;
      sb.AppendLine($"{ind}  __ok = false;");
      Fail argvJson = new("InvalidValue", KeyLiteral(slot.Key), $"\"Error: --{Escape(form)} is JSON. Pass it with --json-args.\"");
      sb.AppendLine($"{ind}  {argvJson.Assign()}");
    }
    else if (slot.IsList)
    {
      EmitAssignListFromStrings(sb, slot, $"__al_{slot.Id}", argvFail, indent + 2);
    }
    else if (slot.IsFlag)
    {
      sb.AppendLine($"{ind}  {slot.VariableName} = true;");
    }
    else
    {
      EmitAssignScalarFromString(sb, slot, $"__ar_{slot.Id}", argvFail, indent + 2);
    }

    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}else if (__jsonMap.TryGetValue(\"{Escape(slot.Key)}\", out global::System.Text.Json.JsonElement __fill_{slot.Id}))");
    sb.AppendLine($"{ind}{{");
    EmitAssignFromJson(sb, slot, $"__fill_{slot.Id}", jsonFail, patternLiteral, indent + 2);
    sb.AppendLine($"{ind}}}");
  }

  // Flags stay false and repeated options stay empty, matching the argv matcher.
  // Other absent keys keep the property or parameter default.
  private static string DefaultInitializer(Slot slot)
  {
    string? literal = slot.Option?.DefaultValueLiteral ?? slot.Parameter?.DefaultValue;
    if (!string.IsNullOrEmpty(literal))
      return literal;

    return "default!";
  }
}
