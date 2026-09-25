using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("identify", Description = "Work with a GUID")]
public sealed class IdentifyCommand : IQuery<string>
{
  [Parameter] public Guid Id { get; set; }

  public sealed class Handler : IQueryHandler<IdentifyCommand, string>
  {
    public Task<string> Handle(IdentifyCommand c, CancellationToken ct) =>
      Task.FromResult<string>($"Processed ID: {c.Id}");
  }
}
