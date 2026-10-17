#region Purpose
// Binds the scalar, enum, and string-array shapes Roslynk's tools use.
#endregion

namespace RoslynkNuru.Binding;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Echoes scalar --json-args values.
/// </summary>
[NuruRoute("scalars", Description = "Bind bool, number, string, enum, and string array")]
public sealed class ScalarsQuery : IQuery<string>
{
  [Option("includeErrors", Description = "List errors.")]
  public bool IncludeErrors { get; set; }

  [Option("includeAnalyzers", Description = "Run analyzers; false is faster, compiler-only.")]
  public bool? IncludeAnalyzers { get; set; }

  [Option("maxResults", Description = "Optional cap.")]
  public int? MaxResults { get; set; }

  [Option("startItem", Description = "First item (1-based).")]
  public int StartItem { get; set; } = 1;

  [Option("symbolName", Description = "Symbol name.")]
  public string? SymbolName { get; set; }

  [Option("ids", Description = "Diagnostic ids.")]
  public string[]? Ids { get; set; }

  [Option("tool", Description = "Enum name.")]
  public ProbeOp? Tool { get; set; }

  public sealed class Handler : IQueryHandler<ScalarsQuery, string>
  {
    public Task<string> Handle(ScalarsQuery query, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string ids = query.Ids is null ? "null" : string.Join("|", query.Ids);
      string max = query.MaxResults is null ? "null" : query.MaxResults.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
      string line = string.Join(
        ",",
        [
          $"includeErrors={query.IncludeErrors}",
          $"includeAnalyzers={(query.IncludeAnalyzers is null ? "null" : query.IncludeAnalyzers.Value.ToString())}",
          $"maxResults={max}",
          $"startItem={query.StartItem}",
          $"symbolName={query.SymbolName ?? "null"}",
          $"ids={ids}",
          $"tool={(query.Tool is null ? "null" : query.Tool.Value.ToString())}"
        ]);
      return Task.FromResult(line);
    }
  }
}
