using TimeWarp.Nuru;

namespace GroupFilteringSample.Shared;

[NuruRouteGroup("ganda")]
public abstract class GandaGroupBase;

[NuruRouteGroup("kanban")]
public abstract class KanbanGroupBase : GandaGroupBase;

[NuruRouteGroup("git")]
public abstract class GitGroupBase : GandaGroupBase;
