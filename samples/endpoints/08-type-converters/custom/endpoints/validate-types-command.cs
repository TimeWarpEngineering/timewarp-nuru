using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("validate", Description = "Validate custom types")]
public sealed class ValidateTypesCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true)] public string[] Values { get; set; } = [];

  public sealed class Handler : ICommandHandler<ValidateTypesCommand, Unit>
  {
    public ValueTask<Unit> Handle(ValidateTypesCommand c, CancellationToken ct)
    {
      WriteLine("Validating values...\n");

      foreach (string value in c.Values)
      {
        bool isEmail = EmailAddress.IsValid(value);
        bool isColor = HexColor.IsValid(value);

        string type = (isEmail, isColor) switch
        {
          (true, _) => "email",
          (_, true) => "color",
          _ => "unknown"
        };

        WriteLine($"  {value,-30} -> {type}");
      }

      return default;
    }
  }
}
