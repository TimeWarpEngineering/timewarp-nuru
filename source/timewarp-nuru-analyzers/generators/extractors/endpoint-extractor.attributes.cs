#region Purpose
// EndpointExtractor partial: [NuruRoute], alias and example attribute extraction and attribute-syntax helpers.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// Extracts and combines aliases from the command class and group hierarchy.
  /// </summary>
  private static ImmutableArray<string> ExtractAndCombineAliases(
    INamedTypeSymbol classSymbol,
    GroupInfo groupInfo,
    string pattern)
  {
    List<string> allAliases = [];

    // Extract direct aliases from command class
    ImmutableArray<string> directAliases = ExtractNuruRouteAliasAttribute(classSymbol);
    allAliases.AddRange(directAliases);

    // Generate full alias patterns from group aliases. GroupPrefixIndex selects a GROUP
    // (not a word), so the alias replaces that group's ENTIRE prefix — correct even when the
    // prefix is multi-word (e.g. a group "git remote" aliased to "gr" yields "gr {pattern}").
    if (!groupInfo.GroupAliases.IsDefaultOrEmpty && !groupInfo.GroupPrefixes.IsDefaultOrEmpty)
    {
      ImmutableArray<string> groupPrefixes = groupInfo.GroupPrefixes;

      foreach (GroupAliasDefinition groupAlias in groupInfo.GroupAliases)
      {
        if (groupAlias.GroupPrefixIndex < 0 || groupAlias.GroupPrefixIndex >= groupPrefixes.Length)
          continue;

        string aliasPrefix = string.Join(
          " ",
          groupPrefixes.Select((prefix, index) => index == groupAlias.GroupPrefixIndex ? groupAlias.Alias : prefix));

        string fullAliasPattern = string.IsNullOrEmpty(pattern)
          ? aliasPrefix
          : $"{aliasPrefix} {pattern}";

        allAliases.Add(fullAliasPattern);
      }
    }

    return [.. allAliases];
  }

  /// <summary>
  /// Validates that a route pattern is a single literal identifier or empty string.
  /// Returns a diagnostic if the pattern is invalid, null if valid.
  /// </summary>
  private static Diagnostic? ValidateRoutePattern(string pattern, Location attributeLocation)
  {
    // Empty string is valid (root/default route)
    if (string.IsNullOrEmpty(pattern))
      return null;

    // Parse the pattern to see what it contains
    ImmutableArray<SegmentDefinition> segments = PatternStringExtractor.ExtractSegments(pattern);

    // Valid: exactly one segment that is a literal
    if (segments.Length == 1 && segments[0] is LiteralDefinition)
      return null;

    // Invalid: zero segments (shouldn't happen), multiple segments, or non-literal segment
    return Diagnostic.Create(
      DiagnosticDescriptors.InvalidNuruRoutePattern,
      attributeLocation,
      pattern);
  }

  /// <summary>
  /// Extracts pattern, description, and location from [NuruRoute] attribute.
  /// </summary>
  private static (string? Pattern, string? Description, Location Location) ExtractNuruRouteAttribute(TypeDeclarationSyntax classDeclaration)
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
        Location location = attribute.GetLocation();

        return (pattern, description, location);
      }
    }

    return (null, null, Location.None);
  }

  /// <summary>
  /// Extracts aliases from [NuruRouteAlias] attribute on the command class.
  /// </summary>
  private static ImmutableArray<string> ExtractNuruRouteAliasAttribute(INamedTypeSymbol classSymbol)
  {
    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;
      if (attributeName != NuruRouteAliasAttributeName && attributeName != $"{NuruRouteAliasAttributeName}Attribute")
        continue;

      if (attribute.ConstructorArguments.Length > 0)
      {
        TypedConstant argsArray = attribute.ConstructorArguments[0];

        List<string> aliases = [];

        if (argsArray.Values.Length > 0)
        {
          foreach (TypedConstant value in argsArray.Values)
          {
            if (value.Value is string alias)
              aliases.Add(alias);
          }
        }
        else if (argsArray.Value is string singleAlias)
        {
          aliases.Add(singleAlias);
        }

        return [.. aliases];
      }
    }

    return [];
  }

  /// <summary>
  /// True when the command class carries the attribute <paramref name="attributeFullName"/>
  /// (<c>[Destructive]</c>, <c>[LocalOnly]</c>).
  /// </summary>
  private static bool HasAttribute(INamedTypeSymbol classSymbol, string attributeFullName)
  {
    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() == attributeFullName)
        return true;
    }

    return false;
  }

  /// <summary>
  /// The bool constructor argument of a Contracts hint attribute (<c>[Destructive]</c>,
  /// <c>[OpenWorld]</c>): true when it is omitted or anything other than <see langword="false"/>,
  /// null when the attribute is absent.
  /// </summary>
  private static bool? BoolAttributeValue(INamedTypeSymbol classSymbol, string attributeFullName)
  {
    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() != attributeFullName)
        continue;

      // [Destructive] with no argument is true. Only an explicit false is false.
      return attribute.ConstructorArguments.Length == 0 || attribute.ConstructorArguments[0].Value is not false;
    }

    return null;
  }

  /// <summary>
  /// Catalog title. An operation reads <c>OperationAttribute.Title</c> and ignores
  /// <c>[DisplayName]</c>. A route class with no operation reads <c>[DisplayName]</c>.
  /// Blank or whitespace is no title.
  /// </summary>
  private static string? TitleValue(INamedTypeSymbol classSymbol, bool hasOperation)
  {
    if (!hasOperation)
      return StringAttributeValue(classSymbol, DisplayNameAttributeFullName);

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (!IsMetadataAttribute(attribute, OperationAttributeMetadataName))
        continue;

      string? title = NamedString(attribute, "Title");
      return string.IsNullOrWhiteSpace(title) ? null : title;
    }

    return null;
  }

  /// <summary>
  /// The string constructor argument of an attribute on the command class (<c>[DisplayName]</c>),
  /// or null when the attribute is absent or its text is blank.
  /// </summary>
  private static string? StringAttributeValue(INamedTypeSymbol classSymbol, string attributeFullName)
  {
    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() != attributeFullName)
        continue;

      return attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string text
        && !string.IsNullOrWhiteSpace(text)
        ? text
        : null;
    }

    return null;
  }

  /// <summary>
  /// Extracts usage examples from all <c>[NuruRouteExample]</c> attributes on the command class.
  /// Unlike aliases, examples use <c>AllowMultiple = true</c>, so every matching attribute is
  /// accumulated (not just the first), preserving source declaration order. Attributes with a
  /// null or empty command are silently skipped.
  /// </summary>
  private static ImmutableArray<ExampleDefinition> ExtractNuruRouteExampleAttributes(INamedTypeSymbol classSymbol)
  {
    List<ExampleDefinition> examples = [];

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;
      if (attributeName != NuruRouteExampleAttributeName && attributeName != $"{NuruRouteExampleAttributeName}Attribute")
        continue;

      if (attribute.ConstructorArguments.Length == 0 ||
          attribute.ConstructorArguments[0].Value is not string command ||
          string.IsNullOrEmpty(command))
      {
        continue;
      }

      string? description = null;
      foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
      {
        if (namedArg.Key == "Description")
        {
          description = namedArg.Value.Value as string;
        }
      }

      // Normalize "" to null so both emitters (help / capabilities) see the same absent-vs-present
      // signal - capabilities' `is not null` guard would otherwise emit an empty "description": "".
      if (string.IsNullOrEmpty(description))
        description = null;

      examples.Add(new ExampleDefinition(command, description));
    }

    return [.. examples];
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
}
