namespace TimeWarp.Nuru;

/// <summary>
/// Specifies the configuration section key to bind the options class to.
/// Supports both simple keys ("Database") and hierarchical keys ("MyApp:Settings:Database").
/// </summary>
/// <remarks>
/// <para>
/// By default, the generator uses a convention to determine the section key:
/// - Class name ending in "Options" has the suffix stripped: DatabaseOptions → "Database"
/// - Otherwise, the full class name is used: MyConfig → "MyConfig"
/// </para>
/// <para>
/// Use this attribute to override the convention when the section name doesn't match.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default convention: binds from "Database" section (strips "Options" suffix)
/// public class DatabaseOptions { }
///
/// // Custom section name
/// [ConfigurationKey("DB")]
/// public class DatabaseOptions { }
///
/// // Hierarchical key using colon separator
/// [ConfigurationKey("MyApp:Settings:Database")]
/// public class DatabaseOptions { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConfigurationKeyAttribute : Attribute
{
  /// <summary>
  /// Initializes a new instance of the ConfigurationKeyAttribute.
  /// </summary>
  /// <param name="key">
  /// The configuration section key. Can be a simple key ("Database")
  /// or a hierarchical key using colon separators ("MyApp:Settings:Database").
  /// </param>
  public ConfigurationKeyAttribute(string key)
  {
    Key = key;
  }

  /// <summary>
  /// Gets the configuration section key.
  /// </summary>
  public string Key { get; }
}
