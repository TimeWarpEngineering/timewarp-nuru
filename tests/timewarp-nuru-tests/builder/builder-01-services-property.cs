#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 470-001: NuruAppBuilder.Services is obsolete and tells callers to use ConfigureServices.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Builder
{
  using Microsoft.Extensions.DependencyInjection;

[TestTag("Builder")]
public class ServicesPropertyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ServicesPropertyTests>();

  public static Task Should_throw_directing_callers_to_configure_services()
  {
#pragma warning disable CS0618
    InvalidOperationException ex = Should.Throw<InvalidOperationException>(() =>
    {
      IServiceCollection unused = NuruApp.CreateBuilder().Services;
    });
#pragma warning restore CS0618

    ex.Message.ShouldContain("ConfigureServices");
    ex.Message.ShouldContain("UseMicrosoftDependencyInjection");
    return Task.CompletedTask;
  }
}

}
