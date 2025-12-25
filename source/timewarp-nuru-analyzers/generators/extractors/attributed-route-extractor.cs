// Extracts route definitions from classes decorated with [NuruRoute] attribute.
//
// Handles attributed routes pattern:
// - Read pattern from [NuruRoute("pattern")]
// - Check base class for [NuruRouteGroup] for prefix
// - Infer message type from interface (IQuery<T>, ICommand<T>)
// - Find [Parameter] and [Option] properties
// - Find nested Handler class

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Extracts route definitions from classes decorated with [NuruRoute] attribute.
/// </summary>
internal static class AttributedRouteExtractor
{
  private const string NuruRouteAttributeName = "NuruRoute";
  private const string NuruRouteGroupAttributeName = "NuruRouteGroup";
  private const string ParameterAttributeName = "Parameter";
  private const string OptionAttributeName = "Option";

  /// <summary>
  /// Extracts a RouteDefinition from a class with [NuruRoute] attribute.
  /// </summary>
  /// <param name="classDeclaration">The class declaration with [NuruRoute].</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The extracted route definition, or null if extraction fails.</returns>
  public static RouteDefinition? Extract
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Find the [NuruRoute] attribute
    (string? pattern, string? description) = ExtractNuruRouteAttribute(classDeclaration);
    if (pattern is null)
      return null;

    // Get the group prefix from base class, if any
    string? groupPrefix = ExtractGroupPrefix(classDeclaration, semanticModel, cancellationToken);

    // Infer message type from interfaces
    string messageType = InferMessageType(classDeclaration, semanticModel, cancellationToken);

    // Extract parameters and options from properties
    ImmutableArray<SegmentDefinition> segments = ExtractSegmentsFromProperties(classDeclaration, semanticModel, cancellationToken);

    // Parse the pattern to add any pattern-defined segments
    ImmutableArray<SegmentDefinition> patternSegments = PatternStringExtractor.ExtractSegments(pattern);

    // Merge segments (pattern segments first, then property segments that aren't duplicates)
    ImmutableArray<SegmentDefinition> mergedSegments = MergeSegments(patternSegments, segments);

    // Get handler info
    HandlerDefinition handler = ExtractHandler(classDeclaration, semanticModel, cancellationToken);

    // Calculate specificity
    int specificity = mergedSegments.Sum(s => s.SpecificityContribution);

    return RouteDefinition.Create(
      originalPattern: pattern,
      segments: mergedSegments,
      handler: handler,
      messageType: messageType,
      description: description,
      groupPrefix: groupPrefix,
      computedSpecificity: specificity);
  }

  /// <summary>
  /// Extracts pattern and description from [NuruRoute] attribute.
  /// </summary>
  private static (string? Pattern, string? Description) ExtractNuruRouteAttribute(ClassDeclarationSyntax classDeclaration)
  {
    foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in attributeList.Attributes)
      {
        string? attributeName = GetAttributeName(attribute);
        if (attributeName != NuruRouteAttributeName && attributeName != $"{NuruRouteAttributeName}Attribute")
          continue;

        string? pattern = ExtractPositionalStringArgument(attribute, 0);
        string? description = ExtractNamedStringArgument(attribute, "Description");

        return (pattern, description);
      }
    }

    return (null, null);
  }

  /// <summary>
  /// Extracts group prefix from base class with [NuruRouteGroup] attribute.
  /// </summary>
  private static string? ExtractGroupPrefix
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (classDeclaration.BaseList is null)
      return null;

    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol?.BaseType is null)
      return null;

    // Check base type for [NuruRouteGroup] attribute
    foreach (AttributeData attribute in classSymbol.BaseType.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;
      if (attributeName != NuruRouteGroupAttributeName && attributeName != $"{NuruRouteGroupAttributeName}Attribute")
        continue;

      // Get the prefix from the first constructor argument
      if (attribute.ConstructorArguments.Length > 0 &&
          attribute.ConstructorArguments[0].Value is string prefix)
      {
        return prefix;
      }
    }

    return null;
  }

  /// <summary>
  /// Infers message type from implemented interfaces.
  /// </summary>
  private static string InferMessageType
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return "Unspecified";

    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      string interfaceName = iface.Name;

      if (interfaceName == "IQuery" || interfaceName.StartsWith("IQuery`", StringComparison.Ordinal))
        return "Query";

      if (interfaceName == "IIdempotentCommand" || interfaceName.StartsWith("IIdempotentCommand`", StringComparison.Ordinal))
        return "IdempotentCommand";

      if (interfaceName == "ICommand" || interfaceName.StartsWith("ICommand`", StringComparison.Ordinal))
        return "Command";
    }

    return "Unspecified";
  }

  /// <summary>
  /// Extracts segments from properties with [Parameter] or [Option] attributes.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ExtractSegmentsFromProperties
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<SegmentDefinition>.Builder segments = ImmutableArray.CreateBuilder<SegmentDefinition>();
    int position = 100; // Start after pattern segments

    foreach (MemberDeclarationSyntax member in classDeclaration.Members)
    {
      if (member is not PropertyDeclarationSyntax property)
        continue;

      SegmentDefinition? segment = ExtractSegmentFromProperty(property, position++, semanticModel, cancellationToken);
      if (segment is not null)
        segments.Add(segment);
    }

    return segments.ToImmutable();
  }

  /// <summary>
  /// Extracts a segment from a property with [Parameter] or [Option] attribute.
  /// </summary>
  private static SegmentDefinition? ExtractSegmentFromProperty
  (
    PropertyDeclarationSyntax property,
    int position,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    IPropertySymbol? propertySymbol = semanticModel.GetDeclaredSymbol(property, cancellationToken);
    if (propertySymbol is null)
      return null;

    foreach (AttributeData attribute in propertySymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;

      if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
      {
        return ExtractParameterFromAttribute(propertySymbol, attribute, position);
      }

      if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
      {
        return ExtractOptionFromAttribute(propertySymbol, attribute, position);
      }
    }

    return null;
  }

  /// <summary>
  /// Extracts a ParameterDefinition from a [Parameter] attribute.
  /// </summary>
  private static ParameterDefinition ExtractParameterFromAttribute
  (
    IPropertySymbol property,
    AttributeData attribute,
    int position
  )
  {
    string name = property.Name;
    string? description = null;
    bool isCatchAll = false;
    bool isOptional = property.NullableAnnotation == NullableAnnotation.Annotated;

    // Check named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "Description":
          description = namedArg.Value.Value as string;
          break;
        case "IsCatchAll":
          isCatchAll = namedArg.Value.Value is true;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string? typeConstraint = GetTypeConstraintFromClrType(typeName);

    return new ParameterDefinition(
      Position: position,
      Name: name.ToLowerInvariant(),
      TypeConstraint: typeConstraint,
      Description: description,
      IsOptional: isOptional,
      IsCatchAll: isCatchAll,
      ResolvedClrTypeName: typeName);
  }

  /// <summary>
  /// Extracts an OptionDefinition from an [Option] attribute.
  /// </summary>
  private static OptionDefinition ExtractOptionFromAttribute
  (
    IPropertySymbol property,
    AttributeData attribute,
    int position
  )
  {
    string longForm = property.Name.ToLowerInvariant();
    string? shortForm = null;
    string? description = null;
    bool isOptional = true; // Options are optional by default
    bool isRepeated = false;

    // Check constructor arguments
    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorLongForm)
      longForm = ctorLongForm.TrimStart('-');

    if (attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value is string ctorShortForm)
      shortForm = ctorShortForm.TrimStart('-');

    // Check named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "LongName":
          longForm = (namedArg.Value.Value as string)?.TrimStart('-') ?? longForm;
          break;
        case "ShortName":
          shortForm = (namedArg.Value.Value as string)?.TrimStart('-');
          break;
        case "Description":
          description = namedArg.Value.Value as string;
          break;
        case "IsRequired":
          isOptional = namedArg.Value.Value is not true;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    bool isFlag = typeName == "global::System.Boolean";

    // Check for array types (repeated options)
    if (typeName.EndsWith("[]", StringComparison.Ordinal) ||
        typeName.Contains("IEnumerable", StringComparison.Ordinal) ||
        typeName.Contains("IList", StringComparison.Ordinal))
    {
      isRepeated = true;
    }

    return new OptionDefinition(
      Position: position,
      LongForm: longForm,
      ShortForm: shortForm,
      ParameterName: isFlag ? null : property.Name.ToLowerInvariant(),
      TypeConstraint: isFlag ? null : GetTypeConstraintFromClrType(typeName),
      Description: description,
      ExpectsValue: !isFlag,
      IsOptional: isOptional,
      IsRepeated: isRepeated,
      ParameterIsOptional: property.NullableAnnotation == NullableAnnotation.Annotated,
      ResolvedClrTypeName: typeName);
  }

  /// <summary>
  /// Extracts handler information for attributed routes.
  /// </summary>
  private static HandlerDefinition ExtractHandler
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
    {
      return HandlerDefinition.ForMediator(
        fullTypeName: "global::System.Object",
        parameters: [],
        returnType: HandlerReturnType.Void);
    }

    string fullTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Extract parameters from the route class properties
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();

    foreach (IPropertySymbol property in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      // Skip properties without setters
      if (property.SetMethod is null && property.IsReadOnly)
        continue;

      // Check for [Parameter] or [Option] attributes
      foreach (AttributeData attribute in property.GetAttributes())
      {
        string? attributeName = attribute.AttributeClass?.Name;

        if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          parameters.Add(ParameterBinding.FromParameter(
            parameterName: property.Name,
            typeName: typeName,
            segmentName: property.Name.ToLowerInvariant(),
            isOptional: property.NullableAnnotation == NullableAnnotation.Annotated,
            requiresConversion: typeName != "global::System.String"));
          break;
        }

        if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          if (typeName == "global::System.Boolean")
          {
            parameters.Add(ParameterBinding.FromFlag(property.Name, property.Name.ToLowerInvariant()));
          }
          else
          {
            parameters.Add(ParameterBinding.FromOption(
              parameterName: property.Name,
              typeName: typeName,
              optionName: property.Name.ToLowerInvariant(),
              isOptional: true,
              requiresConversion: typeName != "global::System.String"));
          }

          break;
        }
      }
    }

    // Infer return type from interface
    HandlerReturnType returnType = InferReturnTypeFromInterfaces(classSymbol);

    return HandlerDefinition.ForMediator(
      fullTypeName: fullTypeName,
      parameters: parameters.ToImmutable(),
      returnType: returnType);
  }

  /// <summary>
  /// Infers the return type from implemented interfaces.
  /// </summary>
  private static HandlerReturnType InferReturnTypeFromInterfaces(INamedTypeSymbol classSymbol)
  {
    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      if (!iface.IsGenericType)
        continue;

      string interfaceName = iface.Name;
      if (interfaceName != "IQuery" && interfaceName != "ICommand" && interfaceName != "IIdempotentCommand")
        continue;

      if (iface.TypeArguments.Length > 0)
      {
        ITypeSymbol resultType = iface.TypeArguments[0];
        string fullTypeName = resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string shortTypeName = resultType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);
      }
    }

    return HandlerReturnType.Task;
  }

  /// <summary>
  /// Merges pattern segments with property segments, avoiding duplicates.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> MergeSegments
  (
    ImmutableArray<SegmentDefinition> patternSegments,
    ImmutableArray<SegmentDefinition> propertySegments
  )
  {
    if (propertySegments.Length == 0)
      return patternSegments;

    if (patternSegments.Length == 0)
      return propertySegments;

    ImmutableArray<SegmentDefinition>.Builder merged = ImmutableArray.CreateBuilder<SegmentDefinition>();
    merged.AddRange(patternSegments);

    HashSet<string> existingNames = [];

    foreach (SegmentDefinition segment in patternSegments)
    {
      if (segment is ParameterDefinition param)
        existingNames.Add(param.Name.ToLowerInvariant());
      else if (segment is OptionDefinition option)
        existingNames.Add(option.LongForm.ToLowerInvariant());
    }

    // Add property segments that don't duplicate pattern segments
    foreach (SegmentDefinition segment in propertySegments)
    {
      string name = segment switch
      {
        ParameterDefinition param => param.Name.ToLowerInvariant(),
        OptionDefinition option => option.LongForm.ToLowerInvariant(),
        _ => ""
      };

      if (!string.IsNullOrEmpty(name) && !existingNames.Contains(name))
        merged.Add(segment);
    }

    return merged.ToImmutable();
  }

  /// <summary>
  /// Gets the attribute name from an AttributeSyntax.
  /// </summary>
  private static string? GetAttributeName(AttributeSyntax attribute)
  {
    return attribute.Name switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a positional string argument from an attribute.
  /// </summary>
  private static string? ExtractPositionalStringArgument(AttributeSyntax attribute, int index)
  {
    AttributeArgumentListSyntax? args = attribute.ArgumentList;
    if (args is null || args.Arguments.Count <= index)
      return null;

    AttributeArgumentSyntax arg = args.Arguments[index];

    // Skip named arguments
    if (arg.NameEquals is not null || arg.NameColon is not null)
      return null;

    return arg.Expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a named string argument from an attribute.
  /// </summary>
  private static string? ExtractNamedStringArgument(AttributeSyntax attribute, string name)
  {
    AttributeArgumentListSyntax? args = attribute.ArgumentList;
    if (args is null)
      return null;

    foreach (AttributeArgumentSyntax arg in args.Arguments)
    {
      if (arg.NameEquals?.Name.Identifier.Text == name)
      {
        return arg.Expression switch
        {
          LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
            => literal.Token.ValueText,
          _ => null
        };
      }
    }

    return null;
  }

  /// <summary>
  /// Gets a type constraint string from a CLR type name.
  /// </summary>
  private static string? GetTypeConstraintFromClrType(string clrTypeName)
  {
    return clrTypeName switch
    {
      "global::System.Int32" => "int",
      "global::System.Int64" => "long",
      "global::System.Int16" => "short",
      "global::System.Byte" => "byte",
      "global::System.Single" => "float",
      "global::System.Double" => "double",
      "global::System.Decimal" => "decimal",
      "global::System.Boolean" => "bool",
      "global::System.Char" => "char",
      "global::System.String" => null, // string is default
      "global::System.Guid" => "guid",
      "global::System.DateTime" => "datetime",
      "global::System.DateTimeOffset" => "datetimeoffset",
      "global::System.TimeSpan" => "timespan",
      "global::System.Uri" => "uri",
      "global::System.Version" => "version",
      _ => null
    };
  }
}
