namespace TimeWarp.Nuru;

/// <summary>
/// Extraction methods for extracting route, parameter, option, and group option information
/// from attributed classes.
/// </summary>
public partial class NuruAttributedRouteGenerator
{
  private static AttributedRouteInfo? ExtractRouteInfo(INamedTypeSymbol classSymbol)
  {
    // Get the [NuruRoute] attribute
    AttributeData? routeAttribute = classSymbol.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NuruRouteAttributeName);

    if (routeAttribute is null)
      return null;

    // Extract pattern from attribute constructor
    string pattern = routeAttribute.ConstructorArguments.Length > 0
      ? routeAttribute.ConstructorArguments[0].Value?.ToString() ?? ""
      : "";

    // Extract Description from named arguments
    string? description = null;
    foreach (KeyValuePair<string, TypedConstant> namedArg in routeAttribute.NamedArguments)
    {
      if (namedArg.Key == "Description")
        description = namedArg.Value.Value?.ToString();
    }

    // Check for [NuruRouteAlias]
    List<string> aliases = [];
    AttributeData? aliasAttribute = classSymbol.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NuruRouteAliasAttributeName);

    if (aliasAttribute is { ConstructorArguments.Length: > 0 })
    {
      TypedConstant aliasArg = aliasAttribute.ConstructorArguments[0];
      if (aliasArg.Kind == TypedConstantKind.Array)
      {
        foreach (TypedConstant item in aliasArg.Values)
        {
          if (item.Value is string alias)
            aliases.Add(alias);
        }
      }
    }

    // Check for [NuruRouteGroup] on base classes
    string? groupPrefix = null;
    List<GroupOptionInfo> groupOptions = [];
    INamedTypeSymbol? currentType = classSymbol.BaseType;

    while (currentType is not null && currentType.SpecialType != SpecialType.System_Object)
    {
      AttributeData? groupAttribute = currentType.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NuruRouteGroupAttributeName);

      if (groupAttribute is not null)
      {
        // Extract prefix from constructor
        if (groupAttribute.ConstructorArguments.Length > 0)
          groupPrefix = groupAttribute.ConstructorArguments[0].Value?.ToString();

        // Extract group options from base class properties
        foreach (ISymbol member in currentType.GetMembers())
        {
          if (member is IPropertySymbol property)
          {
            AttributeData? groupOptionAttr = property.GetAttributes()
              .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == GroupOptionAttributeName);

            if (groupOptionAttr is not null)
            {
              GroupOptionInfo groupOption = ExtractGroupOptionInfo(property, groupOptionAttr);
              groupOptions.Add(groupOption);
            }
          }
        }

        break; // Found the group, stop searching
      }

      currentType = currentType.BaseType;
    }

    // Extract parameters and options from class properties
    List<ParameterInfo> parameters = [];
    List<OptionInfo> options = [];

    foreach (ISymbol member in classSymbol.GetMembers())
    {
      if (member is not IPropertySymbol property)
        continue;

      // Check for [Parameter]
      AttributeData? paramAttr = property.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ParameterAttributeName);

      if (paramAttr is not null)
      {
        ParameterInfo paramInfo = ExtractParameterInfo(property, paramAttr);
        parameters.Add(paramInfo);
        continue;
      }

      // Check for [Option]
      AttributeData? optionAttr = property.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == OptionAttributeName);

      if (optionAttr is not null)
      {
        OptionInfo optionInfo = ExtractOptionInfo(property, optionAttr);
        options.Add(optionInfo);
      }
    }

    // Sort parameters by Order (parameters with Order >= 0 come first, sorted by Order)
    parameters.Sort((a, b) => a.Order.CompareTo(b.Order));

    // Infer MessageType from implemented interfaces
    string inferredMessageType = InferMessageType(classSymbol);

    return new AttributedRouteInfo(
      FullTypeName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      TypeName: classSymbol.Name,
      Pattern: pattern,
      Description: description,
      Aliases: aliases,
      GroupPrefix: groupPrefix,
      GroupOptions: groupOptions,
      Parameters: parameters,
      Options: options,
      InferredMessageType: inferredMessageType
    );
  }

  private static ParameterInfo ExtractParameterInfo(IPropertySymbol property, AttributeData attr)
  {
    string? name = null;
    string? description = null;
    bool isCatchAll = false;
    int order = -1; // -1 means unset

    foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "Name":
          name = namedArg.Value.Value?.ToString();
          break;
        case "Description":
          description = namedArg.Value.Value?.ToString();
          break;
        case "IsCatchAll":
          isCatchAll = namedArg.Value.Value is true;
          break;
        case "Order":
          order = namedArg.Value.Value is int orderValue ? orderValue : -1;
          break;
      }
    }

    // Default name to property name in camelCase
    name ??= ToCamelCase(property.Name);

    // Determine if optional from nullability
    bool isOptional = property.NullableAnnotation == NullableAnnotation.Annotated ||
                      IsNullableValueType(property.Type);

    // Get type name for typed parameters
    string? typeName = GetSimpleTypeName(property.Type);

    return new ParameterInfo(
      Name: name,
      PropertyName: property.Name,
      Description: description,
      IsOptional: isOptional,
      IsCatchAll: isCatchAll,
      TypeName: typeName,
      Order: order
    );
  }

  private static OptionInfo ExtractOptionInfo(IPropertySymbol property, AttributeData attr)
  {
    string longForm = "";
    string? shortForm = null;
    string? description = null;
    bool isRepeated = false;

    // Constructor arguments
    if (attr.ConstructorArguments.Length > 0)
      longForm = attr.ConstructorArguments[0].Value?.ToString() ?? "";
    if (attr.ConstructorArguments.Length > 1)
      shortForm = attr.ConstructorArguments[1].Value?.ToString();

    // Named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "Description":
          description = namedArg.Value.Value?.ToString();
          break;
        case "IsRepeated":
          isRepeated = namedArg.Value.Value is true;
          break;
      }
    }

    // Determine if this is a flag (bool) or valued option
    bool isFlag = property.Type.SpecialType == SpecialType.System_Boolean;

    // Determine if the value is optional (nullable type)
    bool isValueOptional = !isFlag && (
      property.NullableAnnotation == NullableAnnotation.Annotated ||
      IsNullableValueType(property.Type));

    // Get type name for typed options
    string? typeName = isFlag ? null : GetSimpleTypeName(property.Type);

    return new OptionInfo(
      LongForm: longForm,
      ShortForm: shortForm,
      PropertyName: property.Name,
      Description: description,
      IsFlag: isFlag,
      IsValueOptional: isValueOptional,
      IsRepeated: isRepeated,
      TypeName: typeName
    );
  }

  private static GroupOptionInfo ExtractGroupOptionInfo(IPropertySymbol property, AttributeData attr)
  {
    string longForm = "";
    string? shortForm = null;
    string? description = null;

    // Constructor arguments
    if (attr.ConstructorArguments.Length > 0)
      longForm = attr.ConstructorArguments[0].Value?.ToString() ?? "";
    if (attr.ConstructorArguments.Length > 1)
      shortForm = attr.ConstructorArguments[1].Value?.ToString();

    // Named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
    {
      if (namedArg.Key == "Description")
        description = namedArg.Value.Value?.ToString();
    }

    bool isFlag = property.Type.SpecialType == SpecialType.System_Boolean;
    bool isValueOptional = !isFlag && (
      property.NullableAnnotation == NullableAnnotation.Annotated ||
      IsNullableValueType(property.Type));

    string? typeName = isFlag ? null : GetSimpleTypeName(property.Type);

    return new GroupOptionInfo(
      LongForm: longForm,
      ShortForm: shortForm,
      PropertyName: property.Name,
      Description: description,
      IsFlag: isFlag,
      IsValueOptional: isValueOptional,
      TypeName: typeName
    );
  }
}
