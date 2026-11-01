using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("identify", Description = "Work with a GUID")]
public sealed class IdentifyCommand : IQuery<string>
{
  [Parameter] public Guid Id { get; set; }

  public sealed class Handler : IRequestHandler<IdentifyCommand, string>
  {
    public ValueTask<string> Handle(IdentifyCommand c, CancellationToken ct) =>
      ValueTask.FromResult<string>($"Processed ID: {c.Id}");
  }
}
