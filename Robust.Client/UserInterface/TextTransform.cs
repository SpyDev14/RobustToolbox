using Robust.Shared.Utility;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Robust.Client.UserInterface;


public enum TextTransform : byte
{
    /// <summary>
    /// No transorm.
    /// </summary>
    None = 0,

    /// <summary>
    /// Uppercase each letter: Hello World => HELLO WORLD;
    /// </summary>
    Upper = 1,

    /// <summary>
    /// Lowercase each letter: Hello World => hello world;
    /// </summary>
    Lower = 2,

    /// <summary>
    /// Uppercase the first character of each word, leave the rest as-is.
    /// Call <see cref="StringExt.ToTitleCase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello W0RLD,ALL-RIgHt;
    /// </code>
    /// </para>
    /// </remarks>
    Title = 3,

    /// <summary>
    /// Uppercase the first char of each word, lowercase the rest.
    /// Call <see cref="StringExt.ToStrictTitleCase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello W0rld,All-Right;
    /// </code>
    /// </para>
    /// </remarks>
    TitleStrict = 4,

    /// <summary>
    /// Uppercase the first character of each word, leave the rest as-is.
    /// Call <see cref="StringExt.ToSentenceCase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello w0RLD,ALL-rIgHt;
    /// </code>
    /// </para>
    /// </remarks>
    Sentence = 5,


    /// <summary>
    /// First letter of string get upper, others get lower: hello woRLD,ALL rIgHt => Hello world,all right;
    /// Call <see cref="StringExt.ToStrictSentenceCase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <code>
    /// hello w0RLD,ALL-rIgHt => Hello w0rld,all-right
    /// </code>
    /// </para>
    /// </remarks>
    SentenceStrict = 6,
}

public static class TextTransformHelpers
{
    /// <summary>
    /// Return transformed string
    /// </summary>
    public static string ApplyToString(this TextTransform textTransform, string text)
        => textTransform switch
        {
            TextTransform.None => text,
            TextTransform.Upper => text.ToUpper(),
            TextTransform.Lower => text.ToLower(),
            TextTransform.Sentence => ApplySentence(text, isStrict: false),
            TextTransform.SentenceStrict => ApplySentence(text, isStrict: true),
            TextTransform.Title => ApplyTitle(text, isStrict: false),
            TextTransform.TitleStrict => ApplyTitle(text, isStrict: true),
            _ => throw new ArgumentOutOfRangeException(
                nameof(textTransform), textTransform, null
            )
        };

    private static string ApplySentence(string text, bool isStrict)
    {
        if (text.Length == 0)
            return "";

        var sb = new StringBuilder(text.Length);
        var runes = text.EnumerateRunes();
        Rune firstRune = runes.Current;
        sb.AppendRune(Rune.ToUpper(firstRune, CultureInfo.CurrentCulture));

        // Need to lower all other runes
        if (isStrict)
        {
            foreach (var rune in runes)
                sb.AppendRune(Rune.ToLower(rune, CultureInfo.CurrentCulture));
        }
        // Does not touch other runes
        else
        {
            int firstLen = firstRune.Utf16SequenceLength;
            sb.Append(text, firstLen, text.Length - firstLen);
        }

        return sb.ToString();
    }

    // Reminder:
    // 1. FrozenSet creation is too heavy operation
    // 2. FrozenSet is very optimized for reading
    private static readonly FrozenSet<Rune> _extraWordRunes = [
        new Rune('-'),
        new Rune('_'),
        new Rune('\''),
        new Rune('’'),
    ];

    private static bool IsWordRune(Rune rune) => (
        Rune.IsLetterOrDigit(rune) || _extraWordRunes.Contains(rune)
    );

    private static string ApplyTitle(string text, bool isStrict)
    {
        if (text.Length == 0)
            return "";

        var sb = new StringBuilder(text.Length);
        int wordRuneIdx = -1; // -1 means is not in a word now
        int textCharIdx = 0; // index of current char in the text
        foreach (var rune in text.EnumerateRunes())
        {
            bool isWord = IsWordRune(rune);

            // set wordRuneIdx to 0 if is word starts
            if (isWord && wordRuneIdx == -1)
                wordRuneIdx = 0;

            // set wordRuneIdx to -1 if word ends
            if (!isWord && wordRuneIdx != -1)
                wordRuneIdx = -1;

            // check isWord & wordRuneIdx synchronyzed
            Debug.Assert(isWord == (wordRuneIdx != -1));

            if (isWord && wordRuneIdx == 0)
                sb.AppendRune(Rune.ToUpper(rune, CultureInfo.CurrentCulture));
            else if (isWord && isStrict)
                sb.AppendRune(Rune.ToLower(rune, CultureInfo.CurrentCulture));
            else // AppendRune is optimized, but built-in Append faster, i think
                sb.Append(text, textCharIdx, rune.Utf16SequenceLength);

            if (isWord)
                wordRuneIdx++;
            textCharIdx += rune.Utf16SequenceLength;
        }

        return sb.ToString();
    }
}
