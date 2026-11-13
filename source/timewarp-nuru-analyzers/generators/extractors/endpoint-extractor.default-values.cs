#region Purpose
// EndpointExtractor partial: property default value extraction (symbol, metadata, constants, attribute display).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// Extracts the default value literal from a property symbol using DeclaringSyntaxReferences.
  /// This method correctly handles properties defined in partial class files.
  /// Prefers semantic resolution (constant value / symbol reference, emitted fully-qualified)
  /// over raw syntax text, so defaults referencing types outside the generated file's using
  /// scope (e.g. an enum member or a static field) still resolve correctly.
  /// </summary>
  /// <param name="property">The property symbol.</param>
  /// <param name="compilation">Compilation used to obtain a SemanticModel for the initializer, if available.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The C# literal and the capabilities display, or nulls when the property has no initializer.</returns>
  private static PropertyDefault ExtractPropertyDefaultValueFromSymbol(IPropertySymbol property, Compilation? compilation, CancellationToken cancellationToken)
  {
    // Get syntax references for this property (handles partial classes correctly).
    // A referenced-assembly property has none, or (for an IDE project reference) syntax
    // from another compilation that this one cannot bind.
    ImmutableArray<SyntaxReference> syntaxReferences = property.DeclaringSyntaxReferences;
    if (syntaxReferences.Length == 0
        || compilation?.ContainsSyntaxTree(syntaxReferences[0].SyntaxTree) == false)
    {
      return MetadataPropertyDefault(property, compilation);
    }

    // A positional record property is declared by its primary-constructor parameter.
    // The parameter default, when there is one, is the default.
    Microsoft.CodeAnalysis.SyntaxNode? syntaxNode = syntaxReferences[0].GetSyntax(cancellationToken);
    if (syntaxNode is Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax parameterSyntax)
    {
      if (compilation is null)
        return default;

      SemanticModel parameterModel = compilation.GetSemanticModel(syntaxReferences[0].SyntaxTree);
      return parameterModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is IParameterSymbol declared
        ? ParameterDefault(declared, property.Type)
        : default;
    }

    if (syntaxNode is not PropertyDeclarationSyntax propertySyntax)
      return default;

    // Check for property initializer (e.g., public int X { get; set; } = 1;)
    if (propertySyntax.Initializer?.Value is not { } initializerValue)
      return default;

    if (compilation is not null)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReferences[0].SyntaxTree);
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(initializerValue, cancellationToken);
      ISymbol? symbol = symbolInfo.Symbol;
      ITypeSymbol valueType = UnwrapNullableType(property.Type);

      // `= Name` copies a primary-constructor parameter. Its default is the parameter default.
      // Any other initializer that reads a parameter cannot be copied and has no default.
      if (symbol is IParameterSymbol initializerParameter)
        return ParameterDefault(initializerParameter, property.Type);

      if (ReferencesParameter(initializerValue, semanticModel, cancellationToken))
        return default;

      // Enum members have a numeric constant value. Keep the member so the generated
      // assignment stays typed. Capabilities show the member name only.
      if (symbol is IFieldSymbol enumField && enumField.ContainingType.TypeKind == TypeKind.Enum)
      {
        return new PropertyDefault(enumField.ToDisplayString(FullyQualifiedMemberFormat), enumField.Name);
      }

      Optional<object?> constantValue = semanticModel.GetConstantValue(initializerValue, cancellationToken);
      if (constantValue.HasValue
          && constantValue.Value is not null
          && valueType.TypeKind == TypeKind.Enum
          && valueType is INamedTypeSymbol enumType)
      {
        string display = EnumDisplay(enumType, constantValue.Value);
        if (symbol is IFieldSymbol or IPropertySymbol)
          return new PropertyDefault(symbol.ToDisplayString(FullyQualifiedMemberFormat), display);

        return new PropertyDefault(FormatEnumCast(enumType, constantValue.Value), display);
      }

      if (constantValue.HasValue)
        return FormatConstantLiteral(constantValue.Value, property.Type);

      // A static readonly field or a property is copied as code. Its value is not known
      // at build time, so capabilities omit it.
      if (symbol is IFieldSymbol or IPropertySymbol)
        return new PropertyDefault(symbol.ToDisplayString(FullyQualifiedMemberFormat), null);

      // new(), new Foo(), and method calls are not a capabilities default.
      // Collection expressions stay as syntax so `[]` still compiles.
      if (initializerValue is CollectionExpressionSyntax
          or ArrayCreationExpressionSyntax
          or ImplicitArrayCreationExpressionSyntax)
      {
        string syntax = initializerValue.ToString();
        return new PropertyDefault(syntax, syntax);
      }

      if (initializerValue is ObjectCreationExpressionSyntax
          or ImplicitObjectCreationExpressionSyntax
          or InvocationExpressionSyntax
          || symbol is IMethodSymbol)
      {
        return new PropertyDefault(UnemittableDefaultLiteral, null);
      }
    }

    string text = initializerValue.ToString();
    return new PropertyDefault(text, text);
  }

  /// <summary>
  /// Default for a property with no initializer syntax, as in a referenced assembly.
  /// A record constructor parameter counts only on the constructor the generated code calls.
  /// An explicit parameter default is real code, so it is copied. A parameter with no default
  /// on that constructor is a required argument. Otherwise the initializer cannot be seen: the
  /// object initializer leaves the member out (default!) so the compiled initializer runs, and
  /// a [DefaultValue] only feeds capabilities.
  /// </summary>
  private static PropertyDefault MetadataPropertyDefault(IPropertySymbol property, Compilation? compilation)
  {
    if (property.ContainingType is { IsRecord: true } record)
    {
      IMethodSymbol? parameterless = compilation is null ? null : CallableParameterlessConstructor(compilation, record);
      foreach (IMethodSymbol constructor in record.InstanceConstructors)
      {
        if (parameterless is not null && !SymbolEqualityComparer.Default.Equals(constructor, parameterless))
          continue;

        if (compilation is not null && !IsAccessibleConstructor(compilation, constructor))
          continue;

        if (IsCopyConstructor(record, constructor))
          continue;

        foreach (IParameterSymbol parameter in constructor.Parameters)
        {
          if (parameter.Name == property.Name
              && SymbolEqualityComparer.Default.Equals(parameter.Type, property.Type)
              && (parameter.HasExplicitDefaultValue || parameterless is null))
          {
            return ParameterDefault(parameter, property.Type);
          }
        }
      }
    }

    return new PropertyDefault(UnemittableDefaultLiteral, DefaultValueAttributeDisplay(property));
  }

  /// <summary>
  /// Capabilities text of a <c>[DefaultValue(constant)]</c>, or null.
  /// </summary>
  private static string? DefaultValueAttributeDisplay(IPropertySymbol property)
  {
    foreach (AttributeData attribute in property.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() != DefaultValueAttributeFullName
          || attribute.ConstructorArguments.Length != 1)
      {
        continue;
      }

      TypedConstant argument = attribute.ConstructorArguments[0];
      if (argument.Kind != TypedConstantKind.Primitive && argument.Kind != TypedConstantKind.Enum)
        return null;

      object? value = argument.Value;
      if (value is null)
        return null;

      if (UnwrapNullableType(property.Type) is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        return EnumDisplay(enumType, value);

      return value switch
      {
        bool flag => flag ? "true" : "false",
        string text => text,
        IFormattable formattable => formattable.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()
      };
    }

    return null;
  }

  /// <summary>
  /// Emits a C# literal for a compile-time constant, and a suffix-free display value.
  /// </summary>
  private static PropertyDefault FormatConstantLiteral(object? value, ITypeSymbol propertyType)
  {
    switch (value)
    {
      case string text:
      {
        string quoted = SymbolDisplay.FormatLiteral(text, quote: true);
        return new PropertyDefault(quoted, quoted);
      }
      case char character:
      {
        string quoted = SymbolDisplay.FormatLiteral(character, quote: true);
        return new PropertyDefault(quoted, quoted);
      }
      case bool flag:
      {
        string text = flag ? "true" : "false";
        return new PropertyDefault(text, text);
      }
      case null:
      {
        if (propertyType.IsReferenceType && propertyType.NullableAnnotation != NullableAnnotation.Annotated)
          return new PropertyDefault(NullForgivingDefaultLiteral, null);

        // `= default` on a non-nullable struct, typically a parameter default.
        if (propertyType.IsValueType && propertyType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
          return new PropertyDefault("default", null);

        return new PropertyDefault("null", "null");
      }
      case float number:
      {
        if (float.IsNaN(number))
          return new PropertyDefault("float.NaN", "NaN");

        if (float.IsPositiveInfinity(number))
          return new PropertyDefault("float.PositiveInfinity", "Infinity");

        if (float.IsNegativeInfinity(number))
          return new PropertyDefault("float.NegativeInfinity", "-Infinity");

        string plain = number.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(FormatRealLiteral(plain, "f"), TrimRealDisplay(plain));
      }
      case double number:
      {
        if (double.IsNaN(number))
          return new PropertyDefault("double.NaN", "NaN");

        if (double.IsPositiveInfinity(number))
          return new PropertyDefault("double.PositiveInfinity", "Infinity");

        if (double.IsNegativeInfinity(number))
          return new PropertyDefault("double.NegativeInfinity", "-Infinity");

        string plain = number.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain, TrimRealDisplay(plain));
      }
      case decimal number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(FormatRealLiteral(plain, "m"), TrimRealDisplay(plain));
      }
      case uint number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "u", plain);
      }
      case long number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "L", plain);
      }
      case ulong number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "UL", plain);
      }
      case IFormattable formattable:
      {
        string plain = formattable.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain, plain);
      }
      default:
      {
        string plain = value.ToString() ?? UnemittableDefaultLiteral;
        return new PropertyDefault(plain, plain == UnemittableDefaultLiteral ? null : plain);
      }
    }
  }

  private static string TrimRealDisplay(string text)
  {
    if (text.EndsWith(".0", StringComparison.Ordinal))
      return text[..^2];

    return text;
  }

  private static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1)
    {
      return named.TypeArguments[0];
    }

    return type;
  }

  private static bool PropertyIsInitOnly(IPropertySymbol property) =>
    property.SetMethod is { IsInitOnly: true };
}
