using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Robust.Shared.Utility;

#if false
public static class StringExt
{
    static readonly Regex 

    /// <returns>start and len in Runes</returns>
    private static IEnumerable<(int start, int len)> SelectWords(Rune[] runes)
    {
        static bool isWordRune(Rune rune) =>
            Rune.IsLetterOrDigit(rune) ||
            rune == new Rune('\'') ||
            rune == new Rune('’') ||
            rune == new Rune('_');

        int startIdx = -1; // -1 mean is not in word
        int len = 0;
        foreach (var (i, rune) in runes.Index())
        {
            bool isWord = isWordRune(rune);

            // Catch word
            if (isWord && startIdx == -1)
                startIdx = i;

            if (isWord)
                len++;

            // Catched word ended now
            if (!isWord && startIdx != -1)
            {
                yield return (startIdx, len);
                startIdx = -1;
                len = 0;
            }
        }

        if (startIdx != -1)
            yield return (startIdx, len);
    }

    private static string RunesToString(Rune[] runes)
    {
        var sb = new StringBuilder();
        var x = new Rune();
        sb.Append(ref x);
    }

    /// <summary>
    /// Uppercase the first character of each word.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello W0rld,All-Rig-ht;
    /// </code>
    /// </remarks>
    public static string ToTitleCase(this string text, CaseConvertationOptions? opts = null)
    {
        opts ??= CaseConvertationOptions.Default;

        if (string.IsNullOrWhiteSpace(text))
            return text;

        Rune[] textAsRunes = text.EnumerateRunes().ToArray();
        var words = SelectWords(textAsRunes);

        foreach (var (start, len) in words)
        {
            textAsRunes[start] = Rune.ToUpper(textAsRunes[start], opts.CultureInfo);

            for (int i = start + 1; i < start + len; i++)
                textAsRunes[i] = opts.Strict
                    ? Rune.ToLower(textAsRunes[i], opts.CultureInfo)
                    : textAsRunes[i];
        }
        return sb.ToString();
    }

    /// <summary>
    /// Uppercase the first character of string only, leave the rest as-is.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello w0rld,all-right
    /// </code>
    /// </remarks>
    public static string ToSentenceCase(this string text, CaseConvertationOptions? opts = null)
    {
        opts ??= CaseConvertationOptions.Default;

        return "";
    }
}

/// <summary>
/// Options that control how a string is converted to a specific case
/// (Title Case, Sentence case, etc.).
/// </summary>
/// <remarks>
/// The same options are shared by all case-conversion methods
/// (<see cref="StringExt.ToTitleCase"/>, <see cref="StringExt.ToSentenceCase"/>, ...),
/// so that their behavior stays consistent.
/// </remarks>
public record class CaseConvertationOptions()
{
    public static readonly CaseConvertationOptions Default = new CaseConvertationOptions();

    /// <summary>
    /// Whether the rest of each word should be lowercased.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see langword="true"/> (default) — <b>strict</b> mode:
    /// all other characters are lowercased.
    /// </para>
    /// <para>
    /// <see langword="false"/> — <b>preserve</b> mode:
    /// remaining characters are left as-is.
    /// </para>
    /// <para>
    /// If the text may contain acronyms (e.g. <c>API</c>, <c>URL</c>, <c>HP</c>)
    /// that must keep their original casing, set this to <see langword="false"/>.
    /// Strict mode lowercases the rest of each word, so <c>"API"</c> becomes
    /// <c>"Api"</c> and <c>"URL"</c> becomes <c>"Url"</c>.
    /// </para>
    /// <para>
    /// Example for Title Case:
    /// <code>
    /// "hello w0RLD,ALL rIgHt"
    ///     strict:   "Hello World,All Right"
    ///     preserve: "Hello WoRLD,ALL RIgHt"
    /// </code>
    /// </para>
    /// <para>
    /// Example for Sentence Case:
    /// <code>
    /// "hello w0RLD,ALL rIgHt"
    ///     strict:   "Hello w0rld,all right"
    ///     preserve: "Hello w0RLD,ALL rIgHt"
    /// </code>
    /// </para>
    /// </remarks>
    public bool Strict { get; init; } = true;

    /// <summary>
    /// The culture used for culture-sensitive operations
    /// (e.g. determining word boundaries and applying case rules).
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CultureInfo.CurrentCulture"/>.
    /// Pass <see cref="CultureInfo.InvariantCulture"/> for culture-independent,
    /// deterministic results (useful in tests, logs, protocol messages, etc.).
    /// </remarks>
    public CultureInfo CultureInfo { get; init; } = CultureInfo.CurrentCulture;
}
#endif
