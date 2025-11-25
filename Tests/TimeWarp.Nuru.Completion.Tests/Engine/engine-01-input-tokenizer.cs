#!/usr/bin/dotnet --

// Test: InputTokenizer - Tokenizes command-line input for completion analysis
// Task: 063 - Implement InputTokenizer

using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<InputTokenizerTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class InputTokenizerTests
{
  // Helper to verify completed words
  private static void VerifyCompletedWords(ParsedInput result, params string[] expected)
  {
    result.CompletedWords.Length.ShouldBe(expected.Length);
    for (int i = 0; i < expected.Length; i++)
    {
      result.CompletedWords[i].ShouldBe(expected[i]);
    }
  }

  // ==================== Empty and Whitespace Input ====================

  public static async Task Should_return_empty_for_empty_string()
  {
    ParsedInput result = InputTokenizer.Parse("");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeFalse();
    result.IsEmpty.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_string_empty()
  {
    ParsedInput result = InputTokenizer.Parse(string.Empty);

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_space()
  {
    ParsedInput result = InputTokenizer.Parse(" ");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_spaces()
  {
    ParsedInput result = InputTokenizer.Parse("   ");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_tab_character()
  {
    ParsedInput result = InputTokenizer.Parse("\t");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Single Word ====================

  public static async Task Should_parse_single_character_as_partial()
  {
    ParsedInput result = InputTokenizer.Parse("g");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("g");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_single_word_as_partial()
  {
    ParsedInput result = InputTokenizer.Parse("git");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("git");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_word_with_trailing_space_as_complete()
  {
    ParsedInput result = InputTokenizer.Parse("git ");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_word_with_multiple_trailing_spaces()
  {
    ParsedInput result = InputTokenizer.Parse("git   ");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Multiple Words ====================

  public static async Task Should_parse_two_words_with_partial_second()
  {
    ParsedInput result = InputTokenizer.Parse("git s");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBe("s");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_two_complete_words_without_trailing()
  {
    ParsedInput result = InputTokenizer.Parse("git status");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBe("status");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_two_complete_words_with_trailing()
  {
    ParsedInput result = InputTokenizer.Parse("git status ");

    VerifyCompletedWords(result, "git", "status");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_three_words_with_partial_third()
  {
    ParsedInput result = InputTokenizer.Parse("backup data --com");

    VerifyCompletedWords(result, "backup", "data");
    result.PartialWord.ShouldBe("--com");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_three_complete_words()
  {
    ParsedInput result = InputTokenizer.Parse("backup data --compress ");

    VerifyCompletedWords(result, "backup", "data", "--compress");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_spaces_between_words()
  {
    ParsedInput result = InputTokenizer.Parse("git   status");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBe("status");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== Options ====================

  public static async Task Should_parse_short_option_prefix()
  {
    ParsedInput result = InputTokenizer.Parse("-");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("-");
    result.HasTrailingSpace.ShouldBeFalse();
    result.IsTypingOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_short_option()
  {
    ParsedInput result = InputTokenizer.Parse("-v");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("-v");
    result.HasTrailingSpace.ShouldBeFalse();
    result.IsTypingOption.ShouldBeTrue();
    result.IsTypingLongOption.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_long_option_prefix()
  {
    ParsedInput result = InputTokenizer.Parse("--");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("--");
    result.HasTrailingSpace.ShouldBeFalse();
    result.IsTypingOption.ShouldBeTrue();
    result.IsTypingLongOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_partial_long_option()
  {
    ParsedInput result = InputTokenizer.Parse("--ver");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("--ver");
    result.HasTrailingSpace.ShouldBeFalse();
    result.IsTypingLongOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_complete_long_option()
  {
    ParsedInput result = InputTokenizer.Parse("--verbose");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("--verbose");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_command_with_partial_option()
  {
    ParsedInput result = InputTokenizer.Parse("build --ver");

    VerifyCompletedWords(result, "build");
    result.PartialWord.ShouldBe("--ver");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_command_with_complete_option()
  {
    ParsedInput result = InputTokenizer.Parse("build --verbose ");

    VerifyCompletedWords(result, "build", "--verbose");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Quoted Strings ====================

  public static async Task Should_parse_double_quoted_word()
  {
    ParsedInput result = InputTokenizer.Parse("\"hello\"");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("\"hello\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_double_quoted_with_trailing_space()
  {
    ParsedInput result = InputTokenizer.Parse("\"hello\" ");

    VerifyCompletedWords(result, "\"hello\"");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_double_quoted_with_spaces_inside()
  {
    ParsedInput result = InputTokenizer.Parse("\"hello world\"");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("\"hello world\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_single_quoted_word()
  {
    ParsedInput result = InputTokenizer.Parse("'hello'");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("'hello'");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_single_quoted_with_spaces_inside()
  {
    ParsedInput result = InputTokenizer.Parse("'hello world'");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("'hello world'");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_command_with_quoted_argument()
  {
    ParsedInput result = InputTokenizer.Parse("echo \"hello world\"");

    VerifyCompletedWords(result, "echo");
    result.PartialWord.ShouldBe("\"hello world\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_command_with_quoted_argument_complete()
  {
    ParsedInput result = InputTokenizer.Parse("echo \"hello world\" ");

    VerifyCompletedWords(result, "echo", "\"hello world\"");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_unclosed_quote_as_partial()
  {
    ParsedInput result = InputTokenizer.Parse("echo \"hello");

    VerifyCompletedWords(result, "echo");
    result.PartialWord.ShouldBe("\"hello");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== Escape Sequences ====================

  public static async Task Should_parse_escaped_double_quote()
  {
    ParsedInput result = InputTokenizer.Parse("\"hello\\\"world\"");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("\"hello\"world\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_escaped_single_quote()
  {
    ParsedInput result = InputTokenizer.Parse("'hello\\'world'");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("'hello'world'");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_escaped_backslash()
  {
    ParsedInput result = InputTokenizer.Parse("\"path\\\\file\"");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("\"path\\file\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== ParsedInput Properties ====================

  public static async Task Should_calculate_total_word_count_for_empty()
  {
    ParsedInput result = InputTokenizer.Parse("");

    result.TotalWordCount.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_calculate_total_word_count_for_partial()
  {
    ParsedInput result = InputTokenizer.Parse("git");

    result.TotalWordCount.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_calculate_total_word_count_for_complete()
  {
    ParsedInput result = InputTokenizer.Parse("git ");

    result.TotalWordCount.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_calculate_total_word_count_for_multiple_with_partial()
  {
    ParsedInput result = InputTokenizer.Parse("git status");

    result.TotalWordCount.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_detect_typing_option_for_short()
  {
    ParsedInput result = InputTokenizer.Parse("-v");

    result.IsTypingOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_detect_typing_option_for_long()
  {
    ParsedInput result = InputTokenizer.Parse("--verbose");

    result.IsTypingOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_detect_typing_option_for_non_option()
  {
    ParsedInput result = InputTokenizer.Parse("status");

    result.IsTypingOption.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_detect_typing_long_option()
  {
    ParsedInput result = InputTokenizer.Parse("--verbose");

    result.IsTypingLongOption.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_detect_long_option_for_short()
  {
    ParsedInput result = InputTokenizer.Parse("-v");

    result.IsTypingLongOption.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== FromArgs Compatibility ====================

  public static async Task Should_create_from_empty_args_no_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs([], false);

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_create_from_empty_args_with_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs([], true);

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_create_from_single_arg_no_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs(["git"], false);

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("git");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_create_from_single_arg_with_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs(["git"], true);

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_create_from_multiple_args_no_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs(["git", "status"], false);

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBe("status");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_create_from_multiple_args_with_trailing()
  {
    ParsedInput result = InputTokenizer.FromArgs(["git", "status"], true);

    VerifyCompletedWords(result, "git", "status");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Edge Cases ====================

  public static async Task Should_trim_leading_whitespace()
  {
    ParsedInput result = InputTokenizer.Parse("  git");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("git");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_leading_and_trailing_whitespace()
  {
    ParsedInput result = InputTokenizer.Parse("  git  ");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_whitespace()
  {
    ParsedInput result = InputTokenizer.Parse("git\t \tstatus");

    VerifyCompletedWords(result, "git");
    result.PartialWord.ShouldBe("status");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_quoted_string()
  {
    ParsedInput result = InputTokenizer.Parse("echo \"\"");

    VerifyCompletedWords(result, "echo");
    result.PartialWord.ShouldBe("\"\"");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_quoted_string_with_space()
  {
    ParsedInput result = InputTokenizer.Parse("echo \"\" ");

    VerifyCompletedWords(result, "echo", "\"\"");
    result.PartialWord.ShouldBeNull();
    result.HasTrailingSpace.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_very_long_word()
  {
    string longWord = new('a', 1000);
    ParsedInput result = InputTokenizer.Parse(longWord);

    result.PartialWord.ShouldBe(longWord);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters()
  {
    ParsedInput result = InputTokenizer.Parse("file.txt");

    result.CompletedWords.ShouldBeEmpty();
    result.PartialWord.ShouldBe("file.txt");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_path_like_input()
  {
    ParsedInput result = InputTokenizer.Parse("cat /path/to/file.txt");

    VerifyCompletedWords(result, "cat");
    result.PartialWord.ShouldBe("/path/to/file.txt");
    result.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== Static Empty Instance ====================

  public static async Task Should_have_correct_empty_instance()
  {
    ParsedInput empty = ParsedInput.Empty;

    empty.CompletedWords.ShouldBeEmpty();
    empty.PartialWord.ShouldBeNull();
    empty.HasTrailingSpace.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_have_singleton_empty_instance()
  {
    ReferenceEquals(ParsedInput.Empty, ParsedInput.Empty).ShouldBeTrue();

    await Task.CompletedTask;
  }
}
