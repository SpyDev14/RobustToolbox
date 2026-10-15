using System;
using System.Text;

namespace Robust.Shared.Utility;

public static class StringBuilderExt
{
    public static void AppendRune(this StringBuilder sb, Rune rune)
    {
        // NOTE: Append(Rune) can be added in .NET 11
        Span<char> chars = stackalloc char[2];
        int written = rune.EncodeToUtf16(chars);
        sb.Append(chars[..written]);
    }
}
