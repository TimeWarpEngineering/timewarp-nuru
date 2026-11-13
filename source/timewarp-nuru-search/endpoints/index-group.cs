#region Purpose
// Route-group base class that gives every index endpoint the shared `index` prefix.
#endregion

namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRouteGroup("index")]
public abstract class IndexGroup;
