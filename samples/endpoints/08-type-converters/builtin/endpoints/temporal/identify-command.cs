using TimeWarp.Nuru;

[NuruRoute("identify", Description = "Work with a GUID")]
public sealed class IdentifyCommand : IQuery<string>
{
  [Parameter] public Guid Id { get; set; }

  public sealed class Handler : IQueryHandler<IdentifyCommand, string>
  {
    public ValueTask<string> Handle(IdentifyCommand c, CancellationToken ct) =>
      new ValueTask<string>($"Processed ID: {c.Id}");
  }
}
