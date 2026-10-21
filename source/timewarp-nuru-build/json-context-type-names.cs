#region Purpose
// Gives a JsonSerializable registration its own TypeInfoPropertyName when the STJ source
// generator would name two types the same (SYSLIB1031).
#endregion

#region Design
// STJ names a type's metadata property from its simple name: GetSymbol.Response and
// GetMembers.Response are both "Response", and OneOf<GetSymbol.Response, X> is
// "OneOfResponseX". The collision also counts types STJ reaches through members, so the
// registered types are resolved to symbols and their member graph is walked. Every type in
// a colliding group is registered with a name built from its full name; a registered type
// gets the name on its own attribute, a reached type gets an extra registration. Types that
// do not collide keep the default name. A type name that does not resolve is registered
// as before, without a graph.
#endregion

namespace TimeWarp.Nuru.Build;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// One [JsonSerializable] registration.
/// </summary>
/// <param name="TypeName">The type passed to typeof.</param>
/// <param name="TypeInfoPropertyName">An explicit metadata property name, or null for the STJ default.</param>
internal sealed record JsonContextRegistration(string TypeName, string? TypeInfoPropertyName);

/// <summary>
/// Assigns TypeInfoPropertyName to registrations whose STJ names collide.
/// </summary>
internal static class JsonContextTypeNames
{
  internal static IReadOnlyList<JsonContextRegistration> Assign(Compilation compilation, IEnumerable<string> typeNames)
  {
    List<string> roots = [.. typeNames.OrderBy(name => name, StringComparer.Ordinal)];
    if (roots.Count == 0)
      return [];

    SemanticModel? model = compilation.SyntaxTrees.FirstOrDefault() is SyntaxTree tree
      ? compilation.GetSemanticModel(tree)
      : null;

    Dictionary<string, ITypeSymbol> rootSymbols = new(StringComparer.Ordinal);
    HashSet<ITypeSymbol> reached = new(SymbolEqualityComparer.Default);
    foreach (string root in roots)
    {
      if (model is null || Resolve(model, root) is not ITypeSymbol symbol)
        continue;

      rootSymbols[root] = symbol;
      Visit(symbol, reached);
    }

    HashSet<ITypeSymbol> renamed = new(SymbolEqualityComparer.Default);
    foreach (IGrouping<string, ITypeSymbol> group in reached.GroupBy(DefaultName, StringComparer.Ordinal))
    {
      if (group.Skip(1).Any())
        renamed.UnionWith(group);
    }

    HashSet<string> usedNames = new(StringComparer.Ordinal);
    List<JsonContextRegistration> registrations = [];
    HashSet<ITypeSymbol> registered = new(SymbolEqualityComparer.Default);
    foreach (string root in roots)
    {
      string? propertyName = null;
      if (rootSymbols.TryGetValue(root, out ITypeSymbol? symbol) && registered.Add(symbol) && renamed.Contains(symbol))
        propertyName = UniqueName(symbol, usedNames);

      registrations.Add(new JsonContextRegistration(root, propertyName));
    }

    foreach (ITypeSymbol symbol in renamed
      .Where(symbol => !registered.Contains(symbol) && IsAccessible(symbol, compilation.Assembly))
      .OrderBy(Display, StringComparer.Ordinal))
    {
      registrations.Add(new JsonContextRegistration(Display(symbol), UniqueName(symbol, usedNames)));
    }

    return registrations;
  }

  private static ITypeSymbol? Resolve(SemanticModel model, string typeName)
  {
    TypeSyntax syntax = SyntaxFactory.ParseTypeName(typeName);
    ITypeSymbol? type = model.GetSpeculativeTypeInfo(0, syntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
    return type is null or IErrorTypeSymbol ? null : type;
  }

  private static void Visit(ITypeSymbol type, HashSet<ITypeSymbol> reached)
  {
    if (type is ITypeParameterSymbol or IErrorTypeSymbol or IPointerTypeSymbol or IDynamicTypeSymbol)
      return;

    if (!reached.Add(type))
      return;

    if (type is IArrayTypeSymbol array)
    {
      Visit(array.ElementType, reached);
      return;
    }

    if (type is not INamedTypeSymbol named)
      return;

    foreach (ITypeSymbol argument in named.TypeArguments)
      Visit(argument, reached);

    if (named.TypeKind == TypeKind.Enum || IsFrameworkType(named))
      return;

    for (INamedTypeSymbol? current = named;
         current is not null && current.SpecialType != SpecialType.System_Object;
         current = current.BaseType)
    {
      foreach (ISymbol member in current.GetMembers())
      {
        if (member.IsStatic || HasAttribute(member, "JsonIgnoreAttribute"))
          continue;

        if (member is IPropertySymbol { IsIndexer: false, DeclaredAccessibility: Accessibility.Public } property
            && property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
        {
          Visit(property.Type, reached);
        }
        else if (member is IFieldSymbol { DeclaredAccessibility: Accessibility.Public } field
                 && HasAttribute(field, "JsonIncludeAttribute"))
        {
          Visit(field.Type, reached);
        }
      }
    }
  }

  private static bool IsFrameworkType(INamedTypeSymbol type)
  {
    string? ns = type.ContainingNamespace?.ToDisplayString();
    return ns is not null && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal));
  }

  private static bool HasAttribute(ISymbol symbol, string attributeName) =>
    symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == attributeName);

  // Mirrors the STJ source generator's default metadata property name.
  private static string DefaultName(ITypeSymbol type)
  {
    if (type is IArrayTypeSymbol array)
    {
      string suffix = array.Rank == 1 ? "Array" : $"Array{array.Rank}D";
      return DefaultName(array.ElementType) + suffix;
    }

    if (type is not INamedTypeSymbol named || !named.IsGenericType)
      return type.Name;

    StringBuilder name = new(named.Name);
    foreach (ITypeSymbol argument in TypeArgumentsInScope(named))
      name.Append(DefaultName(argument));

    return name.ToString();
  }

  private static IEnumerable<ITypeSymbol> TypeArgumentsInScope(INamedTypeSymbol type)
  {
    IEnumerable<ITypeSymbol> outer = type.ContainingType is INamedTypeSymbol container
      ? TypeArgumentsInScope(container)
      : [];
    return outer.Concat(type.TypeArguments);
  }

  private static string UniqueName(ITypeSymbol type, HashSet<string> usedNames)
  {
    StringBuilder name = new();
    foreach (char character in Display(type).Replace("global::", ""))
    {
      if (char.IsLetterOrDigit(character))
        name.Append(character);
      else if (name.Length > 0 && name[^1] != '_')
        name.Append('_');
    }

    string candidate = name.ToString().TrimEnd('_');
    string unique = candidate;
    for (int suffix = 2; !usedNames.Add(unique); suffix++)
      unique = candidate + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);

    return unique;
  }

  private static string Display(ITypeSymbol type) =>
    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

  // typeof in the generated context needs a public type, or an internal one from this compilation.
  private static bool IsAccessible(ITypeSymbol type, IAssemblySymbol assembly)
  {
    switch (type)
    {
      case IArrayTypeSymbol array:
        return IsAccessible(array.ElementType, assembly);
      case INamedTypeSymbol named:
        for (INamedTypeSymbol? current = named; current is not null; current = current.ContainingType)
        {
          bool visible = current.DeclaredAccessibility == Accessibility.Public
            || (current.DeclaredAccessibility == Accessibility.Internal
                && SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, assembly));
          if (!visible)
            return false;
        }

        return named.TypeArguments.All(argument => IsAccessible(argument, assembly));
      default:
        return false;
    }
  }
}
