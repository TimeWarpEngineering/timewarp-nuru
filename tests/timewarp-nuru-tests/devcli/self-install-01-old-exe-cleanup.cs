#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// WindowsExeReplace.TryDeleteOldExecutable coverage (470 M41): after a
/// successful Windows self-install the renamed previous binary (dev.exe.old)
/// must be best-effort deleted so successful installs do not litter.
/// </summary>
[TestTag("DevCli")]
public class WindowsExeReplaceTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WindowsExeReplaceTests>();

  public static async Task Deletes_existing_old_executable()
  {
    string path = Path.Combine(Path.GetTempPath(), $"dev-old-{Guid.NewGuid():N}.exe.old");
    await File.WriteAllTextAsync(path, "previous", CancellationToken.None).ConfigureAwait(false);

    try
    {
      WindowsExeReplace.TryDeleteOldExecutable(path).ShouldBeTrue();
      File.Exists(path).ShouldBeFalse();
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }

    await Task.CompletedTask;
  }

  public static async Task Absent_old_executable_is_success()
  {
    string path = Path.Combine(Path.GetTempPath(), $"dev-missing-{Guid.NewGuid():N}.exe.old");
    File.Exists(path).ShouldBeFalse();

    WindowsExeReplace.TryDeleteOldExecutable(path).ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
