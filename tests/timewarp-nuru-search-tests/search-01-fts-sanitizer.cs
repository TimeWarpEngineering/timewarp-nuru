#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using TimeWarp.Nuru.Search.Services;

[TestTag("Search")]
public class FtsSanitizerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<FtsSanitizerTests>();

  public static async Task Should_quote_single_open_paren()
  {
    string result = SearchIndex.SanitizeFtsQuery("(");

    result.ShouldBe("\"(\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_quote_multiple_asterisks()
  {
    string result = SearchIndex.SanitizeFtsQuery("***");

    result.ShouldBe("\"***\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_double_internal_double_quotes()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello\"world");

    result.ShouldBe("\"hello\"\"world\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_quote_normal_multi_word_query()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello world");

    result.ShouldBe("\"hello\"* \"world\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_whitespace_only()
  {
    string result = SearchIndex.SanitizeFtsQuery("   ");

    result.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_empty_string()
  {
    string result = SearchIndex.SanitizeFtsQuery("");

    result.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_keep_caret_as_literal()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello^world");

    result.ShouldBe("\"hello^world\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_keep_brackets_as_literal()
  {
    string result = SearchIndex.SanitizeFtsQuery("[test]");

    result.ShouldBe("\"[test]\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_special_chars()
  {
    string result = SearchIndex.SanitizeFtsQuery("(hello)");

    result.ShouldBe("\"(hello)\"*");

    await Task.CompletedTask;
  }

  // ==========================================================================
  // 470-008 (M12): control characters. An embedded U+0000 reaches SQLite as a C-string
  // terminator, so FTS5 sees an unterminated quoted string and throws. Every control
  // character is treated as a token separator.
  // ==========================================================================

  public static async Task Should_treat_nul_as_token_separator()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello\0world");

    result.ShouldBe("\"hello\"* \"world\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_treat_c0_controls_as_token_separators()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello\u0001\u001fworld\tagain\r\nlast");

    result.ShouldBe("\"hello\"* \"world\"* \"again\"* \"last\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_treat_c1_controls_and_delete_as_token_separators()
  {
    string result = SearchIndex.SanitizeFtsQuery("hello\u007fworld\u0085last");

    result.ShouldBe("\"hello\"* \"world\"* \"last\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_control_only_query()
  {
    string result = SearchIndex.SanitizeFtsQuery("\0\0\u0001");

    result.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_leave_query_without_controls_unchanged()
  {
    string input = "hello world";
    string result = SearchIndex.SanitizeFtsQuery(input);

    result.ShouldBe("\"hello\"* \"world\"*");

    await Task.CompletedTask;
  }

  public static async Task Should_escape_percent_in_like_pattern()
  {
    string result = SearchIndex.EscapeLikePattern("100%");

    result.ShouldBe("100\\%");

    await Task.CompletedTask;
  }

  // ==========================================================================
  // End-to-end FTS5 validity: the original bug was an uncaught SqliteException
  // from real SQLite (MATCH '((*' etc.), so string-level assertions are not
  // enough — every sanitizer output must PARSE as a valid FTS5 MATCH expression.
  // Uses an in-memory database (SearchIndex's path is hard-coded to ~/.nuru).
  // ==========================================================================

  private static async Task AssertFtsQueryExecutes(string userInput)
  {
    string sanitized = SearchIndex.SanitizeFtsQuery(userInput);
    if (string.IsNullOrEmpty(sanitized))
    {
      return; // SearchAsync returns early for empty sanitized queries
    }

    await using Microsoft.Data.Sqlite.SqliteConnection connection = new("Data Source=:memory:");
    await connection.OpenAsync();

    await using (Microsoft.Data.Sqlite.SqliteCommand create = connection.CreateCommand())
    {
      create.CommandText = "CREATE VIRTUAL TABLE fts USING fts5(content)";
      await create.ExecuteNonQueryAsync();
    }

    await using (Microsoft.Data.Sqlite.SqliteCommand insert = connection.CreateCommand())
    {
      insert.CommandText = "INSERT INTO fts(content) VALUES ('hello world sample content')";
      await insert.ExecuteNonQueryAsync();
    }

    await using Microsoft.Data.Sqlite.SqliteCommand query = connection.CreateCommand();
    query.CommandText = "SELECT count(*) FROM fts WHERE fts MATCH $q";
    query.Parameters.AddWithValue("$q", sanitized);

    // Must not throw SqliteException — result count is irrelevant.
    await query.ExecuteScalarAsync();
  }

  public static async Task Should_execute_fts_query_for_open_paren() =>
    await AssertFtsQueryExecutes("(");

  public static async Task Should_execute_fts_query_for_asterisks() =>
    await AssertFtsQueryExecutes("***");

  public static async Task Should_execute_fts_query_for_embedded_quotes() =>
    await AssertFtsQueryExecutes("hello\"world");

  public static async Task Should_execute_fts_query_for_fts_operators() =>
    await AssertFtsQueryExecutes("NOT AND OR NEAR");

  public static async Task Should_execute_fts_query_for_normal_words() =>
    await AssertFtsQueryExecutes("hello world");

  public static async Task Should_execute_fts_query_for_punctuation_soup() =>
    await AssertFtsQueryExecutes("^ [ ] ( ) : - \" '");

  public static async Task Should_execute_fts_query_for_embedded_nul() =>
    await AssertFtsQueryExecutes("hello\0world");

  public static async Task Should_execute_fts_query_for_leading_and_trailing_nul() =>
    await AssertFtsQueryExecutes("\0hello\0");

  public static async Task Should_execute_fts_query_for_control_soup() =>
    await AssertFtsQueryExecutes("\u0001\u0002hello\u001f\u007f\u0085world\0");

  public static async Task Should_escape_underscore_in_like_pattern()
  {
    string result = SearchIndex.EscapeLikePattern("my_cli");

    result.ShouldBe("my\\_cli");

    await Task.CompletedTask;
  }

  public static async Task Should_escape_backslash_in_like_pattern()
  {
    string result = SearchIndex.EscapeLikePattern("path\\to");

    result.ShouldBe("path\\\\to");

    await Task.CompletedTask;
  }

  public static async Task Should_not_modify_plain_like_pattern()
  {
    string result = SearchIndex.EscapeLikePattern("mygroup");

    result.ShouldBe("mygroup");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
