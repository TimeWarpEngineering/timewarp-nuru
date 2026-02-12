using TimeWarp.Nuru;

namespace GroupFilteringSample.Shared;

public class GitCommands
{
  [NuruRoute("commit")]
  public static string Commit()
  {
    return "Git: Committing changes";
  }

  [NuruRoute("status")]
  public static string Status()
  {
    return "Git: Showing repository status";
  }
}
