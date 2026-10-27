using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Robust.Shared.Utility;


// TODO: choose better name
public static class TextTransformUtils
{
    public struct Opts()
    {
        public bool Strict = false;
        public CultureInfo Culture = CultureInfo.CurrentCulture;
    }

    /// <param name="idx">Index of <see cref="Rune"/> in word.</param>
    private static bool IsWord(Rune rune, int idx)
    {
        if (Rune.IsLetterOrDigit(rune))
            return true;

        if (idx == 0)
            return false;

        return rune == new Rune('\'')
            || rune == new Rune('’');
    }

    /// <summary>
    /// Makes every first symbol of word uppercase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not strict:</b>
    /// <list type="bullet">
    ///   <item><c>'hello' w0R-lD,ALL can't be rIgHt</c> => <c>'Hello' W0R-LD,ALL CAn't Be RIgHt</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Strict:</b>
    /// <list type="bullet">
    ///   <item><c>'hello' w0R-lD,ALL can't be rIgHt</c> => <c>'Hello' W0r-Ld,All Can't Be Right</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static string ToTitleCase(this string text, Opts? options = null)
    {
        Opts opts = options ?? new();

        if (text.Length == 0)
            return "";

        var sb = new StringBuilder(text.Length);
        int wordRuneIdx = -1; // -1 means is not in a word now
        int textCharIdx = 0; // index of current char in the text
        foreach (var rune in text.EnumerateRunes())
        {
            bool isWord = IsWord(rune, wordRuneIdx);

            // set wordRuneIdx to 0 if is word starts
            if (isWord && wordRuneIdx == -1)
                wordRuneIdx = 0;

            // set wordRuneIdx to -1 if word ends
            if (!isWord && wordRuneIdx != -1)
                wordRuneIdx = -1;

            // check isWord & wordRuneIdx synchronyzed
            Debug.Assert(isWord == (wordRuneIdx != -1));

            if (isWord && wordRuneIdx == 0)
                sb.AppendRune(Rune.ToUpper(rune, opts.Culture));
            else if (isWord && opts.Strict)
                sb.AppendRune(Rune.ToLower(rune, opts.Culture));
            else // AppendRune is optimized, but built-in Append faster, i think
                sb.Append(text, textCharIdx, rune.Utf16SequenceLength);

            if (isWord)
                wordRuneIdx++;
            textCharIdx += rune.Utf16SequenceLength;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Makes first symbol of text uppercase.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If strict, makes every other symbos lowercase.
    /// </para>
    /// <para>
    /// <b>Not strict:</b>
    /// <list type="bullet">
    ///   <item><c>'hello' w0R-lD,ALL can't be rIgHt</c> => <c>'Hello' w0R-lD,alL can't be rIgHt</c></item>
    ///   <item><c>1234 - word</c> => <c>1234 - Word</c></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Strict:</b>
    /// <list type="bullet">
    ///   <item><c>'hello' w0R-lD,ALL can't be rIgHt</c> => <c>'Hello' w0r-ld,all can't be right</c></item>
    ///   <item><c>1234 - word</c> => <c>1234 - Word</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static string ToSentenceCase(this string text, Opts? options = null)
    {
        Opts opts = options ?? new();

        if (text.Length == 0)
            return "";

        var sb = new StringBuilder(text.Length);
        int charsToSkip = 0;
        Rune firstWordRune = default;
        foreach (var rune in text.EnumerateRunes())
        {
            charsToSkip += rune.Utf16SequenceLength;
            if (IsWord(rune, 0))
            {
                firstWordRune = rune;
                break;
            }
        }

        sb.AppendRune(Rune.ToUpper(firstWordRune, opts.Culture));
        if (opts.Strict)
            sb.Append(text[charsToSkip..].ToUpper(opts.Culture));
        else
            sb.Append(text, charsToSkip, text.Length - charsToSkip);

        return sb.ToString();
    }
}
