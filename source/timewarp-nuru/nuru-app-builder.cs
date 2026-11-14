#region Purpose
// Core of NuruAppBuilder, the builder used to configure Nuru applications.
#endregion

#region Design
// The constructor is internal; NuruApp.CreateBuilder() is the only way to obtain a builder.
// NuruGenerator identifies DSL calls by containing type name and namespace (NuruAppBuilder in
// TimeWarp.Nuru), so the builder is a concrete partial class, not an interface, and renaming or
// moving it requires a matching generator change.
// The class is split by concern (routes, configuration) across partial files.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Builder for configuring Nuru applications.
/// </summary>
public partial class NuruAppBuilder
{
  /// <summary>
  /// Initializes a new instance of the <see cref="NuruAppBuilder"/> class.
  /// Use <see cref="NuruApp.CreateBuilder()"/> factory method instead.
  /// </summary>
  internal NuruAppBuilder() { }
}
