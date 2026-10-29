using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace FerryKit.Core
{
    public static class StringHelper
    {
        private const int _bufferSizeLimit = 512;

        public static ReadOnlySpan<char> HexTable => "0123456789ABCDEF";

        [MethodImpl(Opt.Inline)] public static string Bold(this string str) => str.FormatTag('b');
        [MethodImpl(Opt.Inline)] public static string Italic(this string str) => str.FormatTag('i');
        [MethodImpl(Opt.Inline)] public static string Strike(this string str) => str.FormatTag('s');
        [MethodImpl(Opt.Inline)] public static string Underline(this string str) => str.FormatTag('u');
        [MethodImpl(Opt.Inline)] public static string Sub(this string str) => str.FormatTag("sub");
        [MethodImpl(Opt.Inline)] public static string Sup(this string str) => str.FormatTag("sup");
        [MethodImpl(Opt.Inline)] public static string Size(this string str, int size) => str.FormatTag("size", size);
        [MethodImpl(Opt.Inline)] public static string Link(this string str, string url) => str.FormatTag("link", url);
        [MethodImpl(Opt.Inline)] public static string Color(this string str, string color) => str.FormatTag("color", color);
        [MethodImpl(Opt.Inline)] public static string Color(this string str, int r, int g, int b) => str.FormatTag(r, g, b);

        [MethodImpl(Opt.Inline)] public static bool IsNumericStart(this char value) => value is >= '0' and <= '9' or '+' or '-';
        [MethodImpl(Opt.Inline)] public static bool IsNumericStart(this string value) => !string.IsNullOrEmpty(value) && value.IsNumericStartSkipEmptyCheck();
        [MethodImpl(Opt.Inline)] public static bool IsNumericStart(this ReadOnlySpan<char> value) => !value.IsEmpty && value.IsNumericStartSkipEmptyCheck();

        [MethodImpl(Opt.Inline)] public static bool HasLeadingOrTrailingWhiteSpace(this string value) => !string.IsNullOrEmpty(value) && value.HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck();
        [MethodImpl(Opt.Inline)] public static bool HasLeadingOrTrailingWhiteSpace(this ReadOnlySpan<char> value) => !value.IsEmpty && value.HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck();

        [MethodImpl(Opt.Inline)] public static bool IsInQuotes(this string value) => value?.Length > 1 && value[0] == '"' && value[^1] == '"';
        [MethodImpl(Opt.Inline)] public static bool IsInQuotes(this ReadOnlySpan<char> value) => value.Length > 1 && value[0] == '"' && value[^1] == '"';

        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimQuotes(this ReadOnlySpan<char> value) => value.IsInQuotes() ? value[1..^1] : value;
        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimEndOne(this ReadOnlySpan<char> value, char trimChar) => !value.IsEmpty && value[^1] == trimChar ? value[..^1] : value;
        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimStartOne(this ReadOnlySpan<char> value, char trimChar) => !value.IsEmpty && value[0] == trimChar ? value[1..] : value;

        [MethodImpl(Opt.Inline)] internal static bool IsNumericStartSkipEmptyCheck(this string value) => value[0].IsNumericStart();
        [MethodImpl(Opt.Inline)] internal static bool IsNumericStartSkipEmptyCheck(this ReadOnlySpan<char> value) => value[0].IsNumericStart();

        [MethodImpl(Opt.Inline)] internal static bool HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck(this string value) => char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]);
        [MethodImpl(Opt.Inline)] internal static bool HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck(this ReadOnlySpan<char> value) => char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]);

        [MethodImpl(Opt.Inline)]
        public static bool HasWhiteSpace(this string str) => str.AsSpan().HasWhiteSpace();

        [MethodImpl(Opt.Inline)]
        public static bool HasWhiteSpace(this ReadOnlySpan<char> span)
        {
            foreach (var c in span)
            {
                if (char.IsWhiteSpace(c))
                    return true;
            }
            return false;
        }

        public static string Truncate(this string str, int len, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= len)
                return str;

            if (len <= suffix.Length)
                return suffix[..len];

            int cutLen = len - suffix.Length;
            return string.Create(len, (str, suffix, cutLen), static (buffer, state) =>
            {
                buffer.ApplyTruncate(state.str, state.suffix, state.cutLen);
            });
        }

        public static string Truncate(this ReadOnlySpan<char> span, int len, string suffix = "...")
        {
            if (span.IsEmpty || span.Length <= len)
                return span.ToString();

            if (len <= suffix.Length)
                return suffix[..len];

            int cutLen = len - suffix.Length;
            if (len <= _bufferSizeLimit)
                return stackalloc char[len].ApplyTruncate(span, suffix, cutLen).ToString();

            var buffer = ArrayPool<char>.Shared.Rent(len);
            try
            {
                return buffer.AsSpan(0, len).ApplyTruncate(span, suffix, cutLen).ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return string.Create(str.Length, str, static (buffer, str) => buffer.ApplyCamelCase(str));
        }

        public static string ToCamelCase(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty || char.IsLower(span[0]))
                return span.ToString();

            int len = span.Length;
            if (len <= _bufferSizeLimit)
                return stackalloc char[len].ApplyCamelCase(span).ToString();

            var buffer = ArrayPool<char>.Shared.Rent(len);
            try
            {
                return buffer.AsSpan(0, len).ApplyCamelCase(span).ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsUpper(str[0]))
                return str;

            return string.Create(str.Length, str, static (buffer, str) => buffer.ApplyPascalCase(str));
        }

        public static string ToPascalCase(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty || char.IsUpper(span[0]))
                return span.ToString();

            int len = span.Length;
            if (len <= _bufferSizeLimit)
                return stackalloc char[len].ApplyPascalCase(span).ToString();

            var buffer = ArrayPool<char>.Shared.Rent(len);
            try
            {
                return buffer.AsSpan(0, len).ApplyPascalCase(span).ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        private static string FormatTag(this string str, char tag)
        {
            return string.Create(Tag.CharTagLen + str.Length, (str, tag), static (span, state) =>
            {
                span[0] = '<';
                span[1] = state.tag;
                span[2] = '>';
                state.str.AsSpan().CopyTo(span[Tag.CharTagPrefixLen..]);
                span[^4] = '<';
                span[^3] = '/';
                span[^2] = state.tag;
                span[^1] = '>';
            });
        }

        private static string FormatTag(this string str, string tag)
        {
            return string.Create(Tag.StrTagLen + str.Length + tag.Length * 2, (str, tag), static (span, state) =>
            {
                var t = state.tag.AsSpan();
                int tLen = t.Length;
                int idx = tLen + 1;
                span[0] = '<';
                t.CopyTo(span[1..]);
                span[idx++] = '>';
                state.str.AsSpan().CopyTo(span[idx..]);
                span[^(tLen + 3)] = '<';
                span[^(tLen + 2)] = '/';
                t.CopyTo(span[^(tLen + 1)..^1]);
                span[^1] = '>';
            });
        }

        /// <summary>
        /// Callers must supply only <see cref="string"/> or <see cref="int"/> because the implementation
        /// reinterprets the generic value without a fallback conversion.
        /// </summary>
        private static string FormatTag<T>(this string str, string tag, T value)
        {
            int valLen = typeof(T) == typeof(string)
                ? Unsafe.As<T, string>(ref value).Length
                : Unsafe.As<T, int>(ref value).Digits();

            return string.Create(Tag.AttrTagLen + str.Length + tag.Length * 2 + valLen, (str, tag, value), static (span, state) =>
            {
                var t = state.tag.AsSpan();
                int tLen = t.Length;
                int idx = tLen + 1;
                span[0] = '<';
                t.CopyTo(span[1..]);
                span[idx++] = '=';
                if (typeof(T) == typeof(string))
                {
                    var v = Unsafe.As<T, string>(ref state.value).AsSpan();
                    v.CopyTo(span[idx..]);
                    idx += v.Length;
                }
                else
                {
                    Unsafe.As<T, int>(ref state.value).TryFormat(span[idx..], out int written, provider: CultureInfo.InvariantCulture);
                    idx += written;
                }
                span[idx++] = '>';
                state.str.AsSpan().CopyTo(span[idx..]);
                span[^(tLen + 3)] = '<';
                span[^(tLen + 2)] = '/';
                t.CopyTo(span[^(tLen + 1)..^1]);
                span[^1] = '>';
            });
        }

        private static string FormatTag(this string str, int r, int g, int b)
        {
            return string.Create(Tag.ColorHexTagLen + str.Length, (str, r, g, b), static (span, state) =>
            {
                var hexTable = HexTable;
                Tag.ColorHexPrefix.AsSpan().CopyTo(span);
                span[Tag.ColorHexPrefixLen] = hexTable[(state.r >> 4) & 0xF];
                span[Tag.ColorHexPrefixLen + 1] = hexTable[state.r & 0xF];
                span[Tag.ColorHexPrefixLen + 2] = hexTable[(state.g >> 4) & 0xF];
                span[Tag.ColorHexPrefixLen + 3] = hexTable[state.g & 0xF];
                span[Tag.ColorHexPrefixLen + 4] = hexTable[(state.b >> 4) & 0xF];
                span[Tag.ColorHexPrefixLen + 5] = hexTable[state.b & 0xF];
                span[Tag.ColorHexPrefixLen + 6] = '>';
                state.str.AsSpan().CopyTo(span[(Tag.ColorHexPrefixLen + Tag.HexRgb + Tag.Close)..]);
                Tag.ColorSuffix.AsSpan().CopyTo(span[^Tag.ColorSuffixLen..]);
            });
        }

        [MethodImpl(Opt.Inline)]
        private static Span<char> ApplyTruncate(this Span<char> buffer, ReadOnlySpan<char> span, ReadOnlySpan<char> suffix, int cutLen)
        {
            span[..cutLen].CopyTo(buffer);
            suffix.CopyTo(buffer[cutLen..]);
            return buffer;
        }

        [MethodImpl(Opt.Inline)]
        private static Span<char> ApplyCamelCase(this Span<char> buffer, ReadOnlySpan<char> span)
        {
            int len = span.Length;
            int i = 0;
            for (; i < len && char.IsUpper(span[i]); ++i)
            {
                if (i > 0 && i + 1 < len && char.IsLower(span[i + 1]))
                    break;

                buffer[i] = char.ToLowerInvariant(span[i]);
            }
            span[i..].CopyTo(buffer[i..]);
            return buffer;
        }

        [MethodImpl(Opt.Inline)]
        private static Span<char> ApplyPascalCase(this Span<char> buffer, ReadOnlySpan<char> span)
        {
            buffer[0] = char.ToUpperInvariant(span[0]);
            span[1..].CopyTo(buffer[1..]);
            return buffer;
        }

        [MethodImpl(Opt.Inline)]
        private static int Digits(this int value)
        {
            int count = value < 0 ? 1 : 0;
            uint u = (uint)(value < 0 ? -(long)value : value);
            // This is used for font sizes, which are commonly below 100, so the early switch arms favor that range.
            return u switch
            {
                < 10u => count + 1,
                < 100u => count + 2,
                < 1000u => count + 3,
                < 10000u => count + 4,
                < 100000u => count + 5,
                < 1000000u => count + 6,
                < 10000000u => count + 7,
                < 100000000u => count + 8,
                < 1000000000u => count + 9,
                _ => count + 10
            };
        }

        // ---------------------------------------------------------------------
        // RichText tag structural constants (for compile-time calculations)
        // Grouping them keeps the length formulas and call sites explicit.
        // ---------------------------------------------------------------------
        private static class Tag
        {
            // Base component length
            public const int Open = 1;          // '<'
            public const int Close = 1;         // '>'
            public const int Slash = 1;         // '/'
            public const int Eq = 1;            // '='
            public const int HexRgb = 6;        // "RRGGBB"

            // 1. single character tag: <t>...</t>
            public const int CharTagPrefixLen = Open + 1 + Close;           // "<t>".Length
            public const int CharTagSuffixLen = Open + Slash + 1 + Close;   // "</t>".Length
            public const int CharTagLen = CharTagPrefixLen + CharTagSuffixLen;

            // 2. string tag: <tag>...</tag> (excluding tag length)
            public const int StrTagPrefixLen = Open + Close;           // "<>".Length
            public const int StrTagSuffixLen = Open + Slash + Close;   // "</>".Length
            public const int StrTagLen = StrTagPrefixLen + StrTagSuffixLen;

            // 3. attribute tag: <tag=value>...</tag> (excluding tag and value length)
            public const int AttrTagPrefixLen = Open + Eq + Close;      // "<=>".Length
            public const int AttrTagSuffixLen = Open + Slash + Close;   // "</>".Length
            public const int AttrTagLen = AttrTagPrefixLen + AttrTagSuffixLen;

            // 4. color tag: <color=#RRGGBB>...</color>
            public const string ColorHexPrefix = "<color=#";
            public const string ColorSuffix = "</color>";
            public const int ColorHexPrefixLen = 8; // "<color=#".Length
            public const int ColorSuffixLen = 8;    // "</color>".Length
            public const int ColorHexTagLen = ColorHexPrefixLen + HexRgb + Close + ColorSuffixLen;
        }
    }

    public static class StringBuilderHelper
    {
        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendBold(this StringBuilder sb, string text)
            => sb.Append("<b>").Append(text).Append("</b>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendItalic(this StringBuilder sb, string text)
            => sb.Append("<i>").Append(text).Append("</i>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendStrike(this StringBuilder sb, string text)
            => sb.Append("<s>").Append(text).Append("</s>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendUnderline(this StringBuilder sb, string text)
            => sb.Append("<u>").Append(text).Append("</u>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendSub(this StringBuilder sb, string text)
            => sb.Append("<sub>").Append(text).Append("</sub>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendSup(this StringBuilder sb, string text)
            => sb.Append("<sup>").Append(text).Append("</sup>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendSize(this StringBuilder sb, string text, int size)
        {
            Span<char> value = stackalloc char[11]; // Includes the sign of int.MinValue.
            size.TryFormat(value, out int written, provider: CultureInfo.InvariantCulture);
            return sb.Append("<size=").Append(value[..written]).Append('>').Append(text).Append("</size>");
        }

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendLink(this StringBuilder sb, string text, string url)
            => sb.Append("<link=").Append(url).Append('>').Append(text).Append("</link>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, string color)
            => sb.Append("<color=").Append(color).Append('>').Append(text).Append("</color>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, int r, int g, int b)
        {
            var hex = StringHelper.HexTable;
            return sb.Append("<color=#")
                .Append(hex[(r >> 4) & 0xF])
                .Append(hex[r & 0xF])
                .Append(hex[(g >> 4) & 0xF])
                .Append(hex[g & 0xF])
                .Append(hex[(b >> 4) & 0xF])
                .Append(hex[b & 0xF])
                .Append('>').Append(text).Append("</color>");
        }
    }
}
