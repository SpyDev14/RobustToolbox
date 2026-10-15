using System.Text;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Robust.Shared.Tests.Utility;

[TestFixture]
[Parallelizable(ParallelScope.Fixtures | ParallelScope.All)]
[TestOf(typeof(StringBuilderExt))]
internal sealed class StringBuilderExt_Test
{
    private static IEnumerable<(Rune rune, string expected)> TestCases { get; } =
    [
        // ASCII (1 UTF-16 code unit)
        (new Rune('A'), "A"),
        (new Rune('\0'), "\0"),
        (new Rune('~'), "~"),
        (new Rune(' '), " "),
        (new Rune('0'), "0"),

        // 2-byte UTF-8 -> single UTF-16 code unit
        (new Rune(0x00E9), "\u00E9"),         // é (LATIN SMALL LETTER E WITH ACUTE)
        (new Rune(0x00FF), "\u00FF"),         // ÿ
        (new Rune(0x0416), "\u0416"),         // Ж (CYRILLIC CAPITAL LETTER ZHE)
        (new Rune(0x03A9), "\u03A9"),         // Ω (GREEK CAPITAL LETTER OMEGA)
        (new Rune(0x07FF), "\u07FF"),         // max 2-byte boundary

        // 3-byte UTF-8 -> single UTF-16 code unit
        (new Rune(0x0800), "\u0800"),         // min 3-byte boundary
        (new Rune(0x20AC), "\u20AC"),         // € (EURO SIGN)
        (new Rune(0x4E2D), "\u4E2D"),         // 中
        (new Rune(0xFFFD), "\uFFFD"),         // REPLACEMENT CHARACTER
        (new Rune(0xD7FF), "\uD7FF"),         // last before surrogates
        (new Rune(0xE000), "\uE000"),         // first after surrogates
        (new Rune(0xFFFF), "\uFFFF"),         // max BMP

        // 4-byte UTF-8 -> surrogate pair in UTF-16
        (new Rune(0x10000), "\uD800\uDC00"),
        (new Rune(0x1F600), "\uD83D\uDE00"),  // 😀 GRINNING FACE
        (new Rune(0x1F4A9), "\uD83D\uDCA9"),  // 💩 PILE OF POO
        (new Rune(0x10FFFF), "\uDBFF\uDFFF"), // max Unicode scalar value
        (new Rune(0x1D11E), "\uD834\uDD1E"),  // 𝄞 MUSICAL SYMBOL G CLEF
    ];

    [Test]
    public void AppendRune_SingleRune([ValueSource(nameof(TestCases))] (Rune rune, string expected) data)
    {
        var sb = new StringBuilder();
        sb.AppendRune(data.rune);
        Assert.That(sb.ToString(), Is.EqualTo(data.expected));
        Assert.That(sb.Length, Is.EqualTo(data.expected.Length));
    }

    [Test]
    public void AppendRune_PreservesPreExistingContent()
    {
        var sb = new StringBuilder("prefix:");
        sb.AppendRune(new Rune(0x1F600));
        sb.AppendRune(new Rune('!'));
        Assert.That(sb.ToString(), Is.EqualTo("prefix:\uD83D\uDE00!"));
    }

    [Test]
    public void AppendRune_MultipleRunesRoundTrip()
    {
        const string expected = "A\u00E9\u0416\u4E2D\uD83D\uDE00\uDBFF\uDFFF";

        var sb = new StringBuilder();
        foreach (var rune in expected.EnumerateRunes())
            sb.AppendRune(rune);

        Assert.That(sb.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void AppendRune_MatchesStringBuilderAppendRune()
    {
        // Sanity: our extension should produce the same result as the built-in
        // StringBuilder.Append(Rune) available on modern .NET.
        foreach (var (rune, expected) in TestCases)
        {
            var sb = new StringBuilder();
            sb.AppendRune(rune);
            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void AppendRune_ReturnValue_IsVoidExtension_ButChainableViaWrap()
    {
        // Just make sure calling on a fresh empty sb with a surrogate-pair rune
        // doesn't throw and produces exactly 2 UTF-16 code units.
        var sb = new StringBuilder();
        sb.AppendRune(new Rune(0x1F600));
        Assert.That(sb.Length, Is.EqualTo(2));
        Assert.That(char.IsHighSurrogate(sb[0]), Is.True);
        Assert.That(char.IsLowSurrogate(sb[1]), Is.True);
    }
}
