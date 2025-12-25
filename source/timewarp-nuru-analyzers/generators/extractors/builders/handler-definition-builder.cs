// Fluent builder for constructing HandlerDefinition.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Fluent builder for constructing HandlerDefinition.
/// Used to build handler info extracted from delegates or IRequest types.
/// </summary>
internal sealed class HandlerDefinitionBuilder
{
  private HandlerKind Kind = HandlerKind.Delegate;
  private string? FullTypeName;
  private string? MethodName;
  private readonly List<ParameterBinding> Parameters = [];
  private HandlerReturnType ReturnType = HandlerReturnType.Void;
  private bool IsAsync;
  private bool RequiresCancellationToken;
  private bool RequiresServiceProvider;

  /// <summary>
  /// Sets this as a delegate handler.
  /// </summary>
  public HandlerDefinitionBuilder AsDelegate()
  {
    Kind = HandlerKind.Delegate;
    FullTypeName = null;
    MethodName = null;
    return this;
  }

  /// <summary>
  /// Sets this as a mediator handler with the request type.
  /// </summary>
  public HandlerDefinitionBuilder AsMediator(string requestTypeName)
  {
    Kind = HandlerKind.Mediator;
    FullTypeName = requestTypeName;
    IsAsync = true;
    RequiresCancellationToken = true;
    RequiresServiceProvider = true;
    return this;
  }

  /// <summary>
  /// Sets this as a method handler.
  /// </summary>
  public HandlerDefinitionBuilder AsMethod(string typeName, string methodName)
  {
    Kind = HandlerKind.Method;
    FullTypeName = typeName;
    MethodName = methodName;
    return this;
  }

  /// <summary>
  /// Adds a parameter bound from a route segment.
  /// </summary>
  public HandlerDefinitionBuilder WithParameter
  (
    string name,
    string typeName,
    string? segmentName = null,
    bool isOptional = false,
    string? defaultValue = null
  )
  {
    // Resolve the type name to full form
    (string fullTypeName, _, _, _) = ResolveTypeName(typeName);

    // Determine if conversion is needed (non-string types need conversion from string input)
    bool requiresConversion = !fullTypeName.Equals("global::System.String", StringComparison.Ordinal);

    Parameters.Add
    (
      ParameterBinding.FromParameter
      (
        parameterName: name,
        typeName: fullTypeName,
        segmentName: segmentName ?? name,
        isOptional: isOptional,
        defaultValue: defaultValue,
        requiresConversion: requiresConversion
      )
    );

    return this;
  }

  /// <summary>
  /// Adds a parameter bound from an option.
  /// </summary>
  public HandlerDefinitionBuilder WithOptionParameter
  (
    string name,
    string typeName,
    string optionName,
    bool isOptional = false,
    bool isArray = false,
    string? defaultValue = null
  )
  {
    // Resolve the type name to full form
    (string fullTypeName, _, _, _) = ResolveTypeName(typeName);

    bool requiresConversion = !fullTypeName.Equals("global::System.String", StringComparison.Ordinal)
                           && !fullTypeName.Equals("global::System.Boolean", StringComparison.Ordinal);

    Parameters.Add
    (
      ParameterBinding.FromOption
      (
        parameterName: name,
        typeName: fullTypeName,
        optionName: optionName,
        isOptional: isOptional,
        isArray: isArray,
        defaultValue: defaultValue,
        requiresConversion: requiresConversion
      )
    );

    return this;
  }

  /// <summary>
  /// Adds a boolean flag parameter (from an option without value).
  /// </summary>
  public HandlerDefinitionBuilder WithFlagParameter(string name, string optionName)
  {
    Parameters.Add(ParameterBinding.FromFlag(name, optionName));
    return this;
  }

  /// <summary>
  /// Adds a CancellationToken parameter.
  /// </summary>
  public HandlerDefinitionBuilder WithCancellationToken(string name = "cancellationToken")
  {
    RequiresCancellationToken = true;
    Parameters.Add(ParameterBinding.ForCancellationToken(name));
    return this;
  }

  /// <summary>
  /// Adds a service parameter (injected from DI).
  /// </summary>
  public HandlerDefinitionBuilder WithService(string name, string typeName)
  {
    RequiresServiceProvider = true;
    Parameters.Add
    (
      new ParameterBinding
      (
        ParameterName: name,
        ParameterTypeName: typeName,
        Source: BindingSource.Service,
        SourceName: typeName,
        IsOptional: false,
        IsArray: false,
        DefaultValueExpression: null,
        RequiresConversion: false,
        ConverterTypeName: null
      )
    );
    return this;
  }

  /// <summary>
  /// Sets the return type to void.
  /// </summary>
  public HandlerDefinitionBuilder ReturnsVoid()
  {
    ReturnType = HandlerReturnType.Void;
    IsAsync = false;
    return this;
  }

  /// <summary>
  /// Sets the return type to Task (async void).
  /// </summary>
  public HandlerDefinitionBuilder ReturnsTask()
  {
    ReturnType = HandlerReturnType.Task;
    IsAsync = true;
    return this;
  }

  /// <summary>
  /// Sets the return type to Task&lt;T&gt;.
  /// </summary>
  public HandlerDefinitionBuilder ReturnsTaskOf(string fullTypeName, string shortTypeName)
  {
    ReturnType = HandlerReturnType.TaskOf(fullTypeName, shortTypeName);
    IsAsync = true;
    return this;
  }

  /// <summary>
  /// Sets a synchronous return type.
  /// </summary>
  public HandlerDefinitionBuilder Returns(string fullTypeName, string shortTypeName)
  {
    ReturnType = HandlerReturnType.Of(fullTypeName, shortTypeName);
    IsAsync = false;
    return this;
  }

  /// <summary>
  /// Sets the return type using common type shortcuts.
  /// </summary>
  public HandlerDefinitionBuilder Returns(string typeName)
  {
    // Handle Task<T> specially to extract inner type
    if (typeName.StartsWith("Task<", StringComparison.Ordinal) && typeName.EndsWith('>'))
    {
      string innerType = typeName[5..^1]; // Extract T from Task<T>
      (string innerFull, string innerShort, _, _) = ResolveTypeName(innerType);
      ReturnType = HandlerReturnType.TaskOf(innerFull, innerShort);
      IsAsync = true;
      return this;
    }

    (string fullName, string shortName, bool isAsync, bool isVoid) = ResolveTypeName(typeName);

    if (isVoid)
    {
      ReturnType = HandlerReturnType.Void;
      IsAsync = false;
    }
    else if (isAsync && shortName == "Task")
    {
      ReturnType = HandlerReturnType.Task;
      IsAsync = true;
    }
    else if (isAsync)
    {
      // This branch shouldn't be hit anymore for Task<T> but keep for safety
      ReturnType = HandlerReturnType.TaskOf(fullName, shortName);
      IsAsync = true;
    }
    else
    {
      ReturnType = HandlerReturnType.Of(fullName, shortName);
      IsAsync = false;
    }

    return this;
  }

  /// <summary>
  /// Builds the HandlerDefinition.
  /// </summary>
  public HandlerDefinition Build()
  {
    return new HandlerDefinition
    (
      HandlerKind: Kind,
      FullTypeName: FullTypeName,
      MethodName: MethodName,
      Parameters: [.. Parameters],
      ReturnType: ReturnType,
      IsAsync: IsAsync,
      RequiresCancellationToken: RequiresCancellationToken,
      RequiresServiceProvider: RequiresServiceProvider
    );
  }

  /// <summary>
  /// Resolves a type name to full/short names.
  /// </summary>
  private static (string fullName, string shortName, bool isAsync, bool isVoid) ResolveTypeName(string typeName)
  {
    // Handle void
    if (typeName is "void")
    {
      return ("void", "void", false, true);
    }

    // Handle Task
    if (typeName is "Task" or "System.Threading.Tasks.Task")
    {
      return ("global::System.Threading.Tasks.Task", "Task", true, false);
    }

    // Handle Task<T>
    if (typeName.StartsWith("Task<", StringComparison.Ordinal))
    {
      string inner = typeName[5..^1]; // Extract T from Task<T>
      (string innerFull, string innerShort, _, _) = ResolveTypeName(inner);
      return ($"global::System.Threading.Tasks.Task<{innerFull}>", $"Task<{innerShort}>", true, false);
    }

    // Handle common primitive types
    return typeName.ToLowerInvariant() switch
    {
      "int" => ("global::System.Int32", "int", false, false),
      "long" => ("global::System.Int64", "long", false, false),
      "short" => ("global::System.Int16", "short", false, false),
      "byte" => ("global::System.Byte", "byte", false, false),
      "float" => ("global::System.Single", "float", false, false),
      "double" => ("global::System.Double", "double", false, false),
      "decimal" => ("global::System.Decimal", "decimal", false, false),
      "bool" => ("global::System.Boolean", "bool", false, false),
      "char" => ("global::System.Char", "char", false, false),
      "string" => ("global::System.String", "string", false, false),
      _ => ($"global::{typeName}", typeName, false, false)
    };
  }
}
