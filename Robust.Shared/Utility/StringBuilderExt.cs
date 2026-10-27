using Robust.Shared.IoC;
using Robust.Shared.Log;
using System;
using System.Linq;
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

    static StringBuilderExt()
    {
        foreach (var method in typeof(StringBuilder).GetMethods())
        {
            if (method.Name != nameof(StringBuilder.Append))
                continue;

            var @params = method.GetParameters();
            if (@params.Length < 1 || @params[0].ParameterType != typeof(Rune))
                continue;

            var log = IoCManager.Resolve<ISawmill>();
            log.Info(
                "StringBuilder.Append(Rune) overload was added, "+
                $"{nameof(StringBuilderExt)}.{nameof(AppendRune)} can be rewwrited to use them."
            );

            break;
        }
    }
}
