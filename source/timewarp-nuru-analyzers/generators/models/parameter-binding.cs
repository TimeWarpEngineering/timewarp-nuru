namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents how a handler parameter is bound from route data or services.
/// This captures the mapping between route segments and handler parameters.
/// </summary>
/// <param name="ParameterName">The name of the handler parameter</param>
/// <param name="ParameterTypeName">Fully qualified type name of the parameter</param>
/// <param name="Source">Where the value comes from (route, option, service, etc.)</param>
/// <param name="SourceName">Name of the source (segment name, option name, service type)</param>
/// <param name="IsOptional">Whether the parameter is optional</param>
/// <param name="IsArray">Whether the parameter is an array (for repeated options)</param>
/// <param name="DefaultValueExpression">C# expression for the default value, if any</param>
/// <param name="RequiresConversion">Whether type conversion is needed</param>
/// <param name="ConverterTypeName">Custom converter type, if specified</param>
/// <param name="ValidatorTypeName">For IOptions&lt;T&gt;, the validator type implementing IValidateOptions&lt;T&gt;</param>
/// <param name="IsEnumType">Whether the parameter type is an enum (uses EnumTypeConverter)</param>
public sealed record ParameterBinding(
  string ParameterName,
  string ParameterTypeName,
  BindingSource Source,
  string SourceName,
  bool IsOptional,
  bool IsArray,
  string? DefaultValueExpression,
  bool RequiresConversion,
  string? ConverterTypeName,
  string? ValidatorTypeName = null,
  bool IsEnumType = false)
{
  /// <summary>
  /// Creates a binding for a route parameter.
  /// </summary>
  public static ParameterBinding FromParameter(
    string parameterName,
    string typeName,
    string segmentName,
    bool isOptional = false,
    string? defaultValue = null,
    bool requiresConversion = false,
    bool isEnumType = false)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Parameter,
      SourceName: segmentName,
      IsOptional: isOptional,
      IsArray: false,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null,
      IsEnumType: isEnumType);
  }

  /// <summary>
  /// Creates a binding for an option value.
  /// </summary>
  public static ParameterBinding FromOption(
    string parameterName,
    string typeName,
    string optionName,
    bool isOptional = false,
    bool isArray = false,
    string? defaultValue = null,
    bool requiresConversion = false,
    bool isEnumType = false)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.Option,
      SourceName: optionName,
      IsOptional: isOptional,
      IsArray: isArray,
      DefaultValueExpression: defaultValue,
      RequiresConversion: requiresConversion,
      ConverterTypeName: null,
      IsEnumType: isEnumType);
  }

  /// <summary>
  /// Creates a binding for a boolean flag option.
  /// </summary>
  public static ParameterBinding FromFlag(
    string parameterName,
    string optionName)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Boolean",
      Source: BindingSource.Flag,
      SourceName: optionName,
      IsOptional: true, // Flags are always optional
      IsArray: false,
      DefaultValueExpression: "false",
      RequiresConversion: false,
      ConverterTypeName: null);
  }

  /// <summary>
  /// Creates a binding for a service injected from the service provider.
  /// </summary>
  /// <param name="parameterName">The parameter name.</param>
  /// <param name="serviceTypeName">The fully qualified service type name.</param>
  /// <param name="isOptional">Whether the parameter is optional.</param>
  /// <param name="configurationKey">For IOptions&lt;T&gt;, the configuration section key (from [ConfigurationKey] or convention).</param>
  /// <param name="validatorTypeName">For IOptions&lt;T&gt;, the validator type implementing IValidateOptions&lt;T&gt;.</param>
  public static ParameterBinding FromService(
    string parameterName,
    string serviceTypeName,
    bool isOptional = false,
    string? configurationKey = null,
    string? validatorTypeName = null)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: serviceTypeName,
      Source: BindingSource.Service,
      SourceName: configurationKey ?? serviceTypeName,
      IsOptional: isOptional,
      IsArray: false,
      DefaultValueExpression: null,
      RequiresConversion: false,
      ConverterTypeName: null,
      ValidatorTypeName: validatorTypeName);
  }

  /// <summary>
  /// Creates a binding for the CancellationToken.
  /// </summary>
  public static ParameterBinding ForCancellationToken(string parameterName = "cancellationToken")
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: "global::System.Threading.CancellationToken",
      Source: BindingSource.CancellationToken,
      SourceName: "CancellationToken",
      IsOptional: false,
      IsArray: false,
      DefaultValueExpression: null,
      RequiresConversion: false,
      ConverterTypeName: null);
  }

  /// <summary>
  /// Creates a binding for a catch-all parameter (remaining arguments).
  /// </summary>
  public static ParameterBinding FromCatchAll(
    string parameterName,
    string typeName,
    string segmentName)
  {
    return new ParameterBinding(
      ParameterName: parameterName,
      ParameterTypeName: typeName,
      Source: BindingSource.CatchAll,
      SourceName: segmentName,
      IsOptional: true, // Catch-all can be empty
      IsArray: true,
      DefaultValueExpression: null,
      RequiresConversion: false,
      ConverterTypeName: null);
  }

  /// <summary>
  /// Gets whether this binding has a default value.
  /// </summary>
  public bool HasDefaultValue => DefaultValueExpression is not null;

  /// <summary>
  /// Gets whether this binding comes from the route (parameter, option, flag, or catch-all).
  /// </summary>
  public bool IsFromRoute => Source is BindingSource.Parameter
    or BindingSource.Option
    or BindingSource.Flag
    or BindingSource.CatchAll;

  /// <summary>
  /// Gets the short type name for display/identifier purposes.
  /// </summary>
  public string ShortTypeName
  {
    get
    {
      string typeName = ParameterTypeName;

      // Remove global:: prefix
      if (typeName.StartsWith("global::", StringComparison.Ordinal))
        typeName = typeName[8..];

      // Get just the type name without namespace
      int lastDot = typeName.LastIndexOf('.');
      if (lastDot >= 0)
        typeName = typeName[(lastDot + 1)..];

      return typeName;
    }
  }
}

/// <summary>
/// Specifies where a parameter's value comes from.
/// </summary>
public enum BindingSource
{
  /// <summary>
  /// From a route parameter segment like {name}.
  /// </summary>
  Parameter,

  /// <summary>
  /// From an option with a value like --output {path}.
  /// </summary>
  Option,

  /// <summary>
  /// From a boolean flag option like --verbose.
  /// </summary>
  Flag,

  /// <summary>
  /// From a catch-all parameter like {*args}.
  /// </summary>
  CatchAll,

  /// <summary>
  /// From dependency injection via the service provider.
  /// </summary>
  Service,

  /// <summary>
  /// The CancellationToken for async operations.
  /// </summary>
  CancellationToken
}
