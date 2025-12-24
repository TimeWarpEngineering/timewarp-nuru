// Extracts route definitions from attributed request classes.
//
// Handles Source 3: [Route("pattern")] [Query] class Foo : IRequest<T> { properties }

namespace TimeWarp.Nuru.SourceGen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

/// <summary>
/// Extracts route definitions from attributed request classes.
/// </summary>
internal static class AttributedRouteExtractor
{
  /// <summary>
  /// Result of extracting from an attributed class.
  /// </summary>
  public record AttributedRouteResult(
    string? Pattern,
    string? Description,
    string? MessageType,
    string? RequestTypeName,
    string? ResponseTypeName,
    ImmutableArray<PropertyInfo> Properties,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Information about a property on the request class.
  /// </summary>
  public record PropertyInfo(
    string Name,
    string TypeName,
    bool IsRequired,
    string? DefaultValue);

  /// <summary>
  /// Extracts route information from a class declaration with route attributes.
  /// </summary>
  /// <param name="classDeclaration">The class declaration to extract from.</param>
  /// <returns>Extracted route information.</returns>
  public static AttributedRouteResult ExtractFromClass(ClassDeclarationSyntax classDeclaration)
  {
    string? pattern = null;
    string? description = null;
    string? messageType = null;
    string? requestTypeName = null;
    string? responseTypeName = null;
    List<PropertyInfo> properties = [];
    List<Diagnostic> diagnostics = [];

    // Get the full type name
    requestTypeName = GetFullTypeName(classDeclaration);

    // Extract attributes
    foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in attributeList.Attributes)
      {
        string? attributeName = GetAttributeName(attribute);

        switch (attributeName)
        {
          case "Route":
          case "RouteAttribute":
            pattern = ExtractFirstStringArgument(attribute);
            break;

          case "Description":
          case "DescriptionAttribute":
            description = ExtractFirstStringArgument(attribute);
            break;

          case "Query":
          case "QueryAttribute":
            messageType = "Query";
            break;

          case "Command":
          case "CommandAttribute":
            messageType = "Command";
            break;

          case "IdempotentCommand":
          case "IdempotentCommandAttribute":
            messageType = "IdempotentCommand";
            break;
        }
      }
    }

    // Extract response type from base type (IRequest<T>)
    responseTypeName = ExtractResponseType(classDeclaration);

    // Extract properties
    foreach (MemberDeclarationSyntax member in classDeclaration.Members)
    {
      if (member is PropertyDeclarationSyntax property)
      {
        PropertyInfo? propInfo = ExtractPropertyInfo(property);
        if (propInfo is not null)
        {
          properties.Add(propInfo);
        }
      }
    }

    return new AttributedRouteResult(
      Pattern: pattern,
      Description: description,
      MessageType: messageType ?? "Unspecified",
      RequestTypeName: requestTypeName,
      ResponseTypeName: responseTypeName,
      Properties: [.. properties],
      Diagnostics: [.. diagnostics]);
  }

  /// <summary>
  /// Gets the full type name including namespace from containing declarations.
  /// </summary>
  private static string GetFullTypeName(ClassDeclarationSyntax classDeclaration)
  {
    List<string> nameParts = [];

    // Add the class name
    nameParts.Add(classDeclaration.Identifier.Text);

    // Walk up to find namespace/containing types
    SyntaxNode? current = classDeclaration.Parent;
    while (current is not null)
    {
      switch (current)
      {
        case ClassDeclarationSyntax containingClass:
          nameParts.Insert(0, containingClass.Identifier.Text);
          break;

        case NamespaceDeclarationSyntax ns:
          nameParts.Insert(0, ns.Name.ToString());
          break;

        case FileScopedNamespaceDeclarationSyntax fileNs:
          nameParts.Insert(0, fileNs.Name.ToString());
          break;
      }

      current = current.Parent;
    }

    return $"global::{string.Join(".", nameParts)}";
  }

  /// <summary>
  /// Gets the attribute name without the "Attribute" suffix.
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
  /// Extracts the first string argument from an attribute.
  /// </summary>
  private static string? ExtractFirstStringArgument(AttributeSyntax attribute)
  {
    AttributeArgumentListSyntax? args = attribute.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    ExpressionSyntax expr = args.Arguments[0].Expression;
    return expr switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Extracts the response type from IRequest&lt;T&gt; base type.
  /// </summary>
  private static string? ExtractResponseType(ClassDeclarationSyntax classDeclaration)
  {
    BaseListSyntax? baseList = classDeclaration.BaseList;
    if (baseList is null)
    {
      return null;
    }

    foreach (BaseTypeSyntax baseType in baseList.Types)
    {
      TypeSyntax type = baseType.Type;

      // Look for IRequest<T> or MediatR.IRequest<T>
      if (type is GenericNameSyntax generic)
      {
        string name = generic.Identifier.Text;
        if (name == "IRequest" && generic.TypeArgumentList.Arguments.Count == 1)
        {
          TypeSyntax responseType = generic.TypeArgumentList.Arguments[0];
          return NormalizeTypeName(responseType.ToString());
        }
      }
      else if (type is QualifiedNameSyntax qualified &&
               qualified.Right is GenericNameSyntax qualifiedGeneric)
      {
        string name = qualifiedGeneric.Identifier.Text;
        if (name == "IRequest" && qualifiedGeneric.TypeArgumentList.Arguments.Count == 1)
        {
          TypeSyntax responseType = qualifiedGeneric.TypeArgumentList.Arguments[0];
          return NormalizeTypeName(responseType.ToString());
        }
      }
    }

    return null;
  }

  /// <summary>
  /// Extracts property information from a property declaration.
  /// </summary>
  private static PropertyInfo? ExtractPropertyInfo(PropertyDeclarationSyntax property)
  {
    // Skip private/protected properties
    bool isPublic = property.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
    if (!isPublic)
    {
      return null;
    }

    // Skip static properties
    bool isStatic = property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
    if (isStatic)
    {
      return null;
    }

    // Must have a setter (or init)
    bool hasSetter = property.AccessorList?.Accessors
      .Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration))
      ?? false;

    if (!hasSetter)
    {
      return null;
    }

    string name = property.Identifier.Text;
    string typeName = NormalizeTypeName(property.Type.ToString());

    // Check if required (C# 11 required keyword or [Required] attribute)
    bool isRequired = property.Modifiers.Any(m => m.IsKind(SyntaxKind.RequiredKeyword));
    if (!isRequired)
    {
      // Check for [Required] attribute
      foreach (AttributeListSyntax attrList in property.AttributeLists)
      {
        foreach (AttributeSyntax attr in attrList.Attributes)
        {
          string? attrName = GetAttributeName(attr);
          if (attrName == "Required" || attrName == "RequiredAttribute")
          {
            isRequired = true;
            break;
          }
        }
      }
    }

    // Check for default value
    string? defaultValue = property.Initializer?.Value.ToString();

    return new PropertyInfo(
      Name: name,
      TypeName: typeName,
      IsRequired: isRequired,
      DefaultValue: defaultValue);
  }

  /// <summary>
  /// Normalizes a type name to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    // Handle nullable types
    bool isNullable = typeName.EndsWith('?');
    string baseType = isNullable ? typeName[..^1] : typeName;

    string normalized = baseType switch
    {
      "int" => "global::System.Int32",
      "long" => "global::System.Int64",
      "short" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",
      "object" => "global::System.Object",
      "void" => "void",
      _ when baseType.StartsWith("global::", StringComparison.Ordinal) => baseType,
      _ => $"global::{baseType}"
    };

    return isNullable ? $"{normalized}?" : normalized;
  }

  /// <summary>
  /// Builds parameter bindings from properties, matching against pattern segments.
  /// </summary>
  public static ImmutableArray<ParameterBinding> BuildBindingsFromProperties(
    ImmutableArray<PropertyInfo> properties,
    ImmutableArray<SegmentDefinition> segments)
  {
    List<ParameterBinding> bindings = [];

    foreach (PropertyInfo prop in properties)
    {
      // Try to find matching segment
      string propNameLower = prop.Name.ToLowerInvariant();

      // Check parameters
      foreach (SegmentDefinition segment in segments)
      {
        if (segment is ParameterDefinition param &&
            string.Equals(param.Name, propNameLower, StringComparison.OrdinalIgnoreCase))
        {
          bindings.Add(ParameterBinding.FromParameter(
            parameterName: prop.Name,
            typeName: prop.TypeName,
            segmentName: param.Name,
            isOptional: !prop.IsRequired || param.IsOptional,
            defaultValue: prop.DefaultValue,
            requiresConversion: prop.TypeName != "global::System.String"));
          break;
        }

        if (segment is OptionDefinition option)
        {
          if (string.Equals(option.LongForm, propNameLower, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(option.ParameterName, propNameLower, StringComparison.OrdinalIgnoreCase))
          {
            if (option.IsFlag)
            {
              bindings.Add(ParameterBinding.FromFlag(
                parameterName: prop.Name,
                optionName: option.LongForm));
            }
            else
            {
              bindings.Add(ParameterBinding.FromOption(
                parameterName: prop.Name,
                typeName: prop.TypeName,
                optionName: option.LongForm,
                isOptional: !prop.IsRequired || option.IsOptional,
                isArray: false,
                defaultValue: prop.DefaultValue,
                requiresConversion: prop.TypeName != "global::System.String"));
            }

            break;
          }
        }
      }
    }

    return [.. bindings];
  }
}
