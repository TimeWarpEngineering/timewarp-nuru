#!/usr/bin/dotnet --
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

  public static async Task Should_escape_percent_in_like_pattern()
  {
    string result = SearchIndex.EscapeLikePattern("100%");

    result.ShouldBe("100\\%");

    await Task.CompletedTask;
  }

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
