using TimeWarp.Nuru;

namespace GroupFilteringSample.Shared;

public class KanbanCommands
{
  [NuruRoute("add")]
  public static string Add()
  {
    return "Kanban: Adding a new task";
  }

  [NuruRoute("list")]
  public static string List()
  {
    return "Kanban: Listing all tasks";
  }
}
