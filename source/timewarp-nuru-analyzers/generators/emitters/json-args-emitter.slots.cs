#region Purpose
// Builds the slot model for JSON binding.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class JsonArgsEmitter
{
  private static List<Slot> BuildSlots(
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> converters)
  {
    if (converters.IsDefault)
      converters = [];

    List<Slot> slots = [];
    HashSet<string> ids = new(StringComparer.Ordinal);
    foreach (ParameterDefinition param in route.Parameters)
    {
      slots.Add(CreateParameterSlot(route, param, converters, ids));
    }

    foreach (OptionDefinition option in route.Options)
    {
      slots.Add(CreateOptionSlot(route, option, converters, ids));
    }

    AssignVariables(route, routeIndex, slots);
    return slots;
  }

  private static Slot CreateParameterSlot(
    RouteDefinition route,
    ParameterDefinition param,
    ImmutableArray<CustomConverterDefinition> converters,
    HashSet<string> ids)
  {
    _ = route;
    string? constraint = StripQuestion(param.TypeConstraint);
    bool nullable = (param.TypeConstraint?.EndsWith('?', StringComparison.Ordinal) ?? false);
    ValueShape shape = ShapeFor(constraint, isFlag: false, isList: param.IsCatchAll, isEnum: false, converters: converters);
    if (param.IsCatchAll && shape == ValueShape.Text)
      shape = ValueShape.TextList;

    CustomConverterDefinition? converter = FindConverter(constraint, converters);
    return new Slot
    {
      Key = param.Name,
      Id = AllocId(param.Name, ids),
      IsOption = false,
      IsFlag = false,
      IsList = param.IsCatchAll,
      IsCatchAll = param.IsCatchAll,
      Required = !param.IsOptional && !param.IsCatchAll,
      RequiredFlag = false,
      IsNullable = nullable,
      BaseConstraint = constraint,
      NumberClr = NumberClr(constraint),
      NumberKeyword = NumberClr(constraint),
      Converter = converter,
      Shape = shape,
      ExpectedType = Expected(constraint, shape, null),
      Parameter = param
    };
  }

  private static Slot CreateOptionSlot(
    RouteDefinition route,
    OptionDefinition option,
    ImmutableArray<CustomConverterDefinition> converters,
    HashSet<string> ids)
  {
    string key = option.LongForm ?? option.ShortForm ?? "option";
    if (option.IsJsonValue)
    {
      return new Slot
      {
        Key = key,
        Id = AllocId(key, ids),
        IsOption = true,
        IsFlag = false,
        IsList = false,
        IsCatchAll = false,
        Required = option.CapabilityRequired == true,
        RequiredFlag = false,
        IsNullable = option.ParameterIsOptional,
        DeclaredType = option.ResolvedClrTypeName,
        Option = option,
        Shape = ValueShape.Json,
        ExpectedType = option.IsJsonStringValue ? "string" : "json"
      };
    }

    string? constraint = StripQuestion(option.TypeConstraint);
    bool nullable = (option.TypeConstraint?.EndsWith('?', StringComparison.Ordinal) ?? false) || option.ParameterIsOptional;
    bool requiredFlag = option.IsFlag && !option.IsOptional && !option.IsFlagBound(route);
    bool isList = option.IsRepeated;
    ValueShape shape;
    if (option.IsFlag)
    {
      shape = ValueShape.Bool;
    }
    else
    {
      shape = ShapeFor(constraint, isFlag: false, isList: isList, isEnum: false, converters: converters);
      if (isList && shape == ValueShape.Text)
        shape = ValueShape.TextList;
      if (isList && shape == ValueShape.Number)
        shape = ValueShape.NumberList;
    }

    return new Slot
    {
      Key = key,
      Id = AllocId(key, ids),
      IsOption = true,
      IsFlag = option.IsFlag,
      IsList = isList,
      IsCatchAll = false,
      Required = !option.IsFlag && !option.IsOptional && !option.ParameterIsOptional,
      RequiredFlag = requiredFlag,
      IsNullable = nullable && !option.IsFlag,
      BaseConstraint = option.IsFlag ? "bool" : constraint,
      NumberClr = NumberClr(constraint),
      NumberKeyword = NumberClr(constraint),
      Converter = FindConverter(constraint, converters),
      Option = option,
      Shape = shape,
      ExpectedType = option.IsFlag ? "bool" : Expected(constraint, shape, null)
    };
  }

  private static void AssignVariables(RouteDefinition route, int routeIndex, List<Slot> slots)
  {
    if (route.Handler.Parameters.IsDefaultOrEmpty)
      return;

    List<ParameterDefinition> routeParams = [.. route.Parameters];
    int routeParamIndex = 0;
    foreach (ParameterBinding param in route.Handler.Parameters)
    {
      if (param.Source is BindingSource.Service or BindingSource.CancellationToken)
        continue;

      Slot? slot = null;
      ParameterDefinition? routeParam = null;
      OptionDefinition? option = null;
      if (param.Source is BindingSource.Parameter or BindingSource.CatchAll)
      {
        if (routeParamIndex < routeParams.Count)
        {
          routeParam = routeParams[routeParamIndex];
          slot = slots.FirstOrDefault(s => !s.IsOption && ReferenceEquals(s.Parameter, routeParam));
          routeParamIndex++;
        }
      }
      else if (param.Source is BindingSource.Option or BindingSource.Flag)
      {
        // Exact name first. --Quiet and --quiet are different options. The same matcher
        // drives command construction, so both agree on the slot and its __supplied_ flag.
        OptionDefinition? matched = OptionBindingMatcher.Match(route, param);
        slot = matched is not null
          ? slots.FirstOrDefault(s => s.IsOption && ReferenceEquals(s.Option, matched))
          : null;
        slot ??= slots.FirstOrDefault(s => s.IsOption && OptionSlotMatches(s, param, StringComparison.Ordinal))
          ?? slots.FirstOrDefault(s =>
            s.IsOption && s.VariableName is null && OptionSlotMatches(s, param, StringComparison.OrdinalIgnoreCase));
        option = slot?.Option;
      }

      if (slot is null)
        continue;

      string variable = CaptureName(route, routeIndex, param, routeParam, option);
      bool isEnum = param.IsEnumType;
      string? enumType = isEnum ? StripQuestion(param.ParameterTypeName) : slot.EnumTypeName;
      if (isEnum && slot.IsList)
      {
        enumType = StripQuestion(UnwrapElement(param.ParameterTypeName ?? string.Empty));
      }

      ValueShape shape = slot.Shape;
      if (isEnum)
      {
        shape = slot.IsList ? ValueShape.EnumList : ValueShape.Enum;
      }

      bool nullable = slot.IsNullable || (param.ParameterTypeName?.EndsWith('?', StringComparison.Ordinal) ?? false);
      string? declared = param.ParameterTypeName ?? slot.DeclaredType ?? "string";
      string? constraint = slot.BaseConstraint;
      if (isEnum)
        constraint ??= enumType;

      slots[slots.IndexOf(slot)] = new Slot
      {
        Key = slot.Key,
        Id = slot.Id,
        IsOption = slot.IsOption,
        IsFlag = slot.IsFlag || param.Source == BindingSource.Flag,
        IsList = slot.IsList || param.IsArray,
        IsCatchAll = slot.IsCatchAll || param.Source == BindingSource.CatchAll,
        Required = slot.Required,
        RequiredFlag = slot.RequiredFlag,
        IsNullable = nullable,
        BaseConstraint = constraint,
        DeclaredType = declared,
        VariableName = variable,
        EnumTypeName = enumType,
        NumberClr = slot.NumberClr ?? NumberClr(StripQuestion(declared)),
        NumberKeyword = slot.NumberKeyword ?? NumberClr(StripQuestion(declared)),
        Converter = slot.Converter ?? FindConverter(StripQuestion(declared), []),
        Option = slot.Option,
        Shape = shape,
        ExpectedType = isEnum ? (enumType ?? "enum") : slot.ExpectedType,
        Parameter = slot.Parameter
      };
    }
  }

  private static bool OptionSlotMatches(Slot slot, ParameterBinding param, StringComparison comparison)
  {
    return string.Equals(slot.Key, param.SourceName, comparison)
      || string.Equals(slot.Option?.ShortForm, param.SourceName, comparison)
      || string.Equals(slot.Option?.ParameterName, param.ParameterName, comparison);
  }

  /// <summary>
  /// True when command construction reads <c>__supplied_{id}</c> for this option.
  /// Constructor arguments are always passed, so they do not need the flag.
  /// </summary>
  private static bool NeedsJsonSuppliedFlag(RouteDefinition route, OptionDefinition option)
  {
    // A required member is always in the object initializer, so it reads no flag.
    if (option.DefaultValueLiteral != EndpointExtractor.UnemittableDefaultLiteral || option.IsRequiredMember)
      return false;

    ParameterBinding? match = route.Handler.RouteParameters.FirstOrDefault(param =>
      ReferenceEquals(OptionBindingMatcher.Match(route, param), option));
    if (match is null)
      return false;

    if (!route.Handler.ConstructorArguments.IsDefaultOrEmpty)
    {
      foreach (string propertyName in route.Handler.ConstructorArguments)
      {
        if (string.Equals(match.ParameterName, propertyName, StringComparison.Ordinal))
          return false;
      }
    }

    return true;
  }

  private static string CaptureName(
    RouteDefinition route,
    int routeIndex,
    ParameterBinding param,
    ParameterDefinition? routeParam,
    OptionDefinition? option)
  {
    if (route.Handler.HandlerKind == HandlerKind.Method)
      return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);

    if (route.Handler.HandlerKind == HandlerKind.Command)
    {
      return param.Source switch
      {
        BindingSource.CatchAll => $"__{CSharpIdentifierUtils.ToCamelCase(param.ParameterName)}_{routeIndex}",
        BindingSource.Flag => CSharpIdentifierUtils.EscapeIfKeyword(CSharpIdentifierUtils.ToCamelCase(param.SourceName)),
        BindingSource.Option => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant()),
        _ => CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName.ToLowerInvariant())
      };
    }

    if (param.Source is BindingSource.Parameter or BindingSource.CatchAll)
    {
      if (routeParam is null)
        return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);

      bool isStringTypedCatchAll = routeParam.IsCatchAll
        && TypeConversionMap.IsStringConstraint(routeParam.TypeConstraint);
      if (routeParam.IsCatchAll && (!routeParam.HasTypeConstraint || isStringTypedCatchAll))
        return $"__{routeParam.CamelCaseName}_{routeIndex}";

      return CSharpIdentifierUtils.EscapeIfKeyword(routeParam.CamelCaseName);
    }

    if (option is not null)
    {
      string optVarName = option.ExpectsValue
        ? CSharpIdentifierUtils.ToCamelCase(option.ParameterName ?? option.LongForm ?? option.ShortForm ?? param.ParameterName)
        : CSharpIdentifierUtils.ToCamelCase(option.LongForm ?? option.ShortForm ?? param.ParameterName);
      return CSharpIdentifierUtils.EscapeIfKeyword(optVarName);
    }

    return CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);
  }

  private static ValueShape ShapeFor(
    string? constraint,
    bool isFlag,
    bool isList,
    bool isEnum,
    ImmutableArray<CustomConverterDefinition> converters)
  {
    _ = converters;
    if (isFlag || IsBoolName(constraint))
      return isList ? ValueShape.TextList : ValueShape.Bool;

    if (isEnum)
      return isList ? ValueShape.EnumList : ValueShape.Enum;

    if (NumberClr(constraint) is not null)
      return isList ? ValueShape.NumberList : ValueShape.Number;

    return isList ? ValueShape.TextList : ValueShape.Text;
  }
}
