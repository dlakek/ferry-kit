using System;
using System.Buffers;
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

        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimQuotes(this ReadOnlySpan<char> span) => span.Length > 1 && span[0] == '"' && span[^1] == '"' ? span[1..^1] : span;
        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimEndOne(this ReadOnlySpan<char> span, char trimChar) => !span.IsEmpty && span[^1] == trimChar ? span[..^1] : span;
        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<char> TrimStartOne(this ReadOnlySpan<char> span, char trimChar) => !span.IsEmpty && span[0] == trimChar ? span[1..] : span;

        [MethodImpl(Opt.Inline)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, bool handleEscape = false)
            => handleEscape
            ? span.IndexOfUnquotedWithEscape(value)
            : span.IndexOfUnquotedNoEscape(value);

        [MethodImpl(Opt.Inline)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, int startIndex, bool handleEscape = false)
        {
            var slice = span[startIndex..];
            int idx = slice.IndexOfUnquoted(value, handleEscape);
            return idx == -1 ? -1 : startIndex + idx;
        }

        [MethodImpl(Opt.Inline)]
        public static bool HasWhiteSpace(this string str)
        {
            return str?.AsSpan().HasWhiteSpace() ?? false;
        }

        [MethodImpl(Opt.Inline)]
        public static bool HasWhiteSpace(this ReadOnlySpan<char> span)
        {
            int len = span.Length;
            for (int i = 0; i < len; ++i)
            {
                if (char.IsWhiteSpace(span[i]))
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

        // ---------------------------------------------------------------------
        // private
        // ---------------------------------------------------------------------

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
                    Unsafe.As<T, int>(ref state.value).TryFormat(span[idx..], out int written);
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

        private static int IndexOfUnquotedNoEscape(this ReadOnlySpan<char> span, char value)
        {
            var inQuotes = false;
            int offset = 0;
            while (true)
            {
                var slice = span[offset..];
                int idx = inQuotes ? slice.IndexOf('"') : slice.IndexOfAny('"', value);
                if (idx == -1)
                    return -1;

                if (slice[idx] != '"')
                    return offset + idx;

                inQuotes = !inQuotes;
                offset += idx + 1;
            }
        }

        private static int IndexOfUnquotedWithEscape(this ReadOnlySpan<char> span, char value)
        {
            var inQuotes = false;
            int offset = 0;
            while (true)
            {
                var slice = span[offset..];
                int idx = inQuotes ? slice.IndexOfAny('"', '\\') : slice.IndexOfAny('"', value);
                if (idx == -1)
                    return -1;

                if (slice[idx] == '\\')
                {
                    offset += idx + 2;
                    if (offset > span.Length) return -1;
                    continue;
                }
                if (slice[idx] != '"')
                    return offset + idx;

                inQuotes = !inQuotes;
                offset += idx + 1;
            }
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
            span.CopyTo(buffer);
            int len = buffer.Length;
            for (int i = 0; i < len && char.IsUpper(buffer[i]); ++i)
            {
                if (i > 0 && i + 1 < len && char.IsLower(buffer[i + 1]))
                    break;

                buffer[i] = char.ToLowerInvariant(buffer[i]);
            }
            return buffer;
        }

        [MethodImpl(Opt.Inline)]
        private static Span<char> ApplyPascalCase(this Span<char> buffer, ReadOnlySpan<char> span)
        {
            span.CopyTo(buffer);
            buffer[0] = char.ToUpperInvariant(buffer[0]);
            return buffer;
        }

        [MethodImpl(Opt.Inline)]
        private static int Digits(this int value)
        {
            if (value == 0) return 1;
            int count = 0;
            if (value < 0)
            {
                if (value == int.MinValue) return 11; // -2147483648
                value = -value;
                count = 1;
            }
            if (value < 10) return count + 1;
            if (value < 100) return count + 2; // 폰트 사이즈 입력의 경우 보통 이보다 작으므로 일부러 이진탐색 분기 대신 이렇게 작성
            if (value < 1000) return count + 3;
            if (value < 10000) return count + 4;
            if (value < 100000) return count + 5;
            if (value < 1000000) return count + 6;
            if (value < 10000000) return count + 7;
            if (value < 100000000) return count + 8;
            if (value < 1000000000) return count + 9;
            return count + 10;
        }

        // ---------------------------------------------------------------------
        // RichText 태그 구조 상수 (컴파일 타임 계산용)
        // 정적 클래스로 그룹화하여 가독성 향상 및 네임스페이스 오염 방지
        // ---------------------------------------------------------------------
        private static class Tag
        {
            // 기본 구성 요소 길이
            public const int Open = 1;          // '<'
            public const int Close = 1;         // '>'
            public const int Slash = 1;         // '/'
            public const int Eq = 1;            // '='
            public const int HexRgb = 6;        // "RRGGBB"

            // 1. 단일 문자 태그: <t>...</t>
            public const int CharTagPrefixLen = Open + 1 + Close;           // "<t>".Length
            public const int CharTagSuffixLen = Open + Slash + 1 + Close;   // "</t>".Length
            public const int CharTagLen = CharTagPrefixLen + CharTagSuffixLen;

            // 1. 문자열 태그: <tag>...</tag> (tag 길이 제외)
            public const int StrTagPrefixLen = Open + Close;           // "<>".Length
            public const int StrTagSuffixLen = Open + Slash + Close;   // "</>".Length
            public const int StrTagLen = StrTagPrefixLen + StrTagSuffixLen;

            // 3. 속성 태그: <tag=value>...</tag> (tag, value 길이 제외)
            public const int AttrTagPrefixLen = Open + Eq + Close;      // "<=>".Length
            public const int AttrTagSuffixLen = Open + Slash + Close;   // "</>".Length
            public const int AttrTagLen = AttrTagPrefixLen + AttrTagSuffixLen;

            // 4. Color 태그: <color=#RRGGBB>...</color>
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
            => sb.Append("<size=").Append(size).Append('>').Append(text).Append("</size>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendLink(this StringBuilder sb, string text, string url)
            => sb.Append("<link=").Append(url).Append('>').Append(text).Append("</link>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, string color)
            => sb.Append("<color=").Append(color).Append('>').Append(text).Append("</color>");

        [MethodImpl(Opt.Inline)]
        public static StringBuilder AppendColor(this StringBuilder sb, string text, int r, int g, int b)
            => sb.Append("<color=#")
            .Append(StringHelper.HexTable[(r >> 4) & 0xF])
            .Append(StringHelper.HexTable[r & 0xF])
            .Append(StringHelper.HexTable[(g >> 4) & 0xF])
            .Append(StringHelper.HexTable[g & 0xF])
            .Append(StringHelper.HexTable[(b >> 4) & 0xF])
            .Append(StringHelper.HexTable[b & 0xF])
            .Append('>').Append(text).Append("</color>");
    }
}
