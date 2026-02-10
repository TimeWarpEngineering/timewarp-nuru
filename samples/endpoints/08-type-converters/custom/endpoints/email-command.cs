using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("email", Description = "Send email to address")]
public sealed class EmailCommand : ICommand<Unit>
{
  [Parameter] public string Address { get; set; } = "";
  [Parameter] public string Message { get; set; } = "";

  public sealed class Handler : ICommandHandler<EmailCommand, Unit>
  {
    public ValueTask<Unit> Handle(EmailCommand c, CancellationToken ct)
    {
      EmailAddress email = new EmailAddress(c.Address);
      WriteLine($"Sending email to: {email}");
      WriteLine($"  Domain: {email.Domain}");
      WriteLine($"  Message: {c.Message}");
      WriteLine("✓ Email sent (simulated)");
      return default;
    }
  }
}
