#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text;
using global::DevCli;

/// <summary>
/// Fail-closed coverage for NuGetVersionService.GetPackageVersionsAsync and its
/// id-validation helpers (kanban task 470-007, parent-470 findings M9/M31): only
/// HTTP 404 may map to "not published"; every other non-success status must
/// throw HttpRequestException carrying the status code, and an invalid package
/// id must be rejected before any HTTP call is made.
/// </summary>
[TestTag("DevCli")]
public class FailClosedLookupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<FailClosedLookupTests>();

  public static async Task Not_found_yields_empty_list()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
    using NuGetVersionService service = new(handler);

    IReadOnlyList<string> versions = await service
      .GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None)
      .ConfigureAwait(false);

    versions.Count.ShouldBe(0);
  }

  public static async Task Service_unavailable_throws_with_status()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    using NuGetVersionService service = new(handler);

    HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None).ConfigureAwait(false));

    exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
  }

  public static async Task Too_many_requests_throws()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
    using NuGetVersionService service = new(handler);

    HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None).ConfigureAwait(false));

    exception.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
  }

  public static async Task Unauthorized_throws()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
    using NuGetVersionService service = new(handler);

    HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None).ConfigureAwait(false));

    exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Success_returns_versions()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("{\"versions\":[\"1.0.0\",\"1.1.0\"]}", Encoding.UTF8, "application/json")
    });
    using NuGetVersionService service = new(handler);

    IReadOnlyList<string> versions = await service
      .GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None)
      .ConfigureAwait(false);

    versions.ShouldBe(["1.0.0", "1.1.0"]);
  }

  public static async Task Null_body_on_success_throws()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("null", Encoding.UTF8, "application/json")
    });
    using NuGetVersionService service = new(handler);

    await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None).ConfigureAwait(false));
  }

  public static async Task Invalid_id_throws_before_request()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
    using NuGetVersionService service = new(handler);

    await Should.ThrowAsync<ArgumentException>(async () =>
      await service.GetPackageVersionsAsync("../evil", CancellationToken.None).ConfigureAwait(false));

    handler.RequestCount.ShouldBe(0);
  }

  public static async Task Request_url_is_lowercased_flat_container_index()
  {
    StubHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
    using NuGetVersionService service = new(handler);

    await service.GetPackageVersionsAsync("TimeWarp.Nuru", CancellationToken.None).ConfigureAwait(false);

    handler.LastRequestUri.ShouldNotBeNull();
    handler.LastRequestUri!.ToString().ShouldBe("https://api.nuget.org/v3-flatcontainer/timewarp.nuru/index.json");
  }

  public static async Task IsValidPackageId_accepts_valid_ids()
  {
    string maxLengthId = new('a', NuGetVersionService.MaxPackageIdLength);
    string[] validIds =
    [
      "TimeWarp.Nuru",
      "a",
      "a-b_c.d1",
      maxLengthId
    ];

    foreach (string id in validIds)
    {
      NuGetVersionService.IsValidPackageId(id).ShouldBeTrue(id);
    }

    await Task.CompletedTask;
  }

  public static async Task IsValidPackageId_rejects_invalid_ids()
  {
    string tooLongId = new('a', NuGetVersionService.MaxPackageIdLength + 1);
    string?[] invalidIds =
    [
      null,
      "",
      ".a",
      "a.",
      "a..b",
      "a/b",
      "../evil",
      "a b",
      "ü",
      tooLongId
    ];

    foreach (string? id in invalidIds)
    {
      NuGetVersionService.IsValidPackageId(id).ShouldBeFalse(id ?? "<null>");
    }

    await Task.CompletedTask;
  }

  private sealed class StubHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> Factory;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
      Factory = factory;
    }

    public int RequestCount { get; private set; }

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      RequestCount++;
      LastRequestUri = request.RequestUri;
      return Task.FromResult(Factory(request));
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
