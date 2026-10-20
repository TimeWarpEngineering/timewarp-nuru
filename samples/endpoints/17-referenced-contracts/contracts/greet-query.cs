namespace ReferencedContracts;

using System.ComponentModel;
using TimeWarp.Contracts;
using TimeWarp.Mediator;

/// <summary>
/// Greets someone. The CLI that references this assembly sees metadata only, so it cannot read the
/// <c>Greeting</c> initializer. Omitting <c>--Greeting</c> leaves that initializer in place, and
/// <c>[DefaultValue]</c> is what <c>--capabilities</c> shows.
/// </summary>
[Operation("greet", Description = "Greet someone")]
public sealed class GreetQuery : IQuery<string>
{
  [Description("Who to greet")]
  public required string Name { get; set; }

  [Description("The greeting word")]
  [DefaultValue("Hello")]
  public string Greeting { get; set; } = "Hello";

  [Description("Shout the greeting")]
  public bool Shout { get; set; }

  public sealed class Handler : IQueryHandler<GreetQuery, string>
  {
    public Task<string> Handle(GreetQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string text = $"{query.Greeting}, {query.Name}!";
      return Task.FromResult(query.Shout ? text.ToUpperInvariant() : text);
    }
  }
}
