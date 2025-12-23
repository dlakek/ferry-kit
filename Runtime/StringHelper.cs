using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace FerryKit
{
    public static class StringHelper
    {
        private const int BUFFER_SIZE_LIMIT = 512;

        internal static readonly char[] _hexTable = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static ReadOnlySpan<char> HexTable => _hexTable;

        public static string Bold(this string str) => $"<b>{str}</b>";
        public static string Italic(this string str) => $"<i>{str}</i>";
        public static string Size(this string str, int size) => $"<size={size}>{str}</size>";
        public static string Color(this string str, string color) => $"<color={color}>{str}</color>";
        public static string Color(this string str, int r, int g, int b) => $"<color=#{r:X2}{g:X2}{b:X2}>{str}</color>";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> TrimQuotes(this ReadOnlySpan<char> span) => span.Length > 1 && span[0] == '"' && span[^1] == '"' ? span[1..^1] : span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> TrimEndOne(this ReadOnlySpan<char> span, char trimChar) => !span.IsEmpty && span[^1] == trimChar ? span[..^1] : span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> TrimStartOne(this ReadOnlySpan<char> span, char trimChar) => !span.IsEmpty && span[0] == trimChar ? span[1..] : span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, int startIndex, bool handleEscape = false)
        {
            var slice = span[startIndex..];
            int idx = slice.IndexOfUnquoted(value, handleEscape);
            return idx == -1 ? -1 : startIndex + idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, bool handleEscape = false)
            => handleEscape
            ? span.IndexOfUnquotedWithEscape(value)
            : span.IndexOfUnquotedNoEscape(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasWhiteSpace(this string str)
        {
            return str?.AsSpan().HasWhiteSpace() ?? false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            return string.Create(len, (str, suffix, cutLen), (buffer, state) =>
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
            if (len <= BUFFER_SIZE_LIMIT)
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

            return string.Create(str.Length, str, (buffer, str) => buffer.ApplyCamelCase(str));
        }

        public static string ToCamelCase(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty || char.IsLower(span[0]))
                return span.ToString();

            int len = span.Length;
            if (len <= BUFFER_SIZE_LIMIT)
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

            return string.Create(str.Length, str, (buffer, str) => buffer.ApplyPascalCase(str));
        }

        public static string ToPascalCase(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty || char.IsUpper(span[0]))
                return span.ToString();

            int len = span.Length;
            if (len <= BUFFER_SIZE_LIMIT)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<char> ApplyTruncate(this Span<char> buffer, ReadOnlySpan<char> span, ReadOnlySpan<char> suffix, int cutLen)
        {
            span[..cutLen].CopyTo(buffer);
            suffix.CopyTo(buffer[cutLen..]);
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<char> ApplyPascalCase(this Span<char> buffer, ReadOnlySpan<char> span)
        {
            span.CopyTo(buffer);
            buffer[0] = char.ToUpperInvariant(buffer[0]);
            return buffer;
        }
    }

    public static class StringBuilderHelper
    {
        public static StringBuilder AppendBold(this StringBuilder sb, string text)
            => sb.Append("<b>").Append(text).Append("</b>");

        public static StringBuilder AppendItalic(this StringBuilder sb, string text)
            => sb.Append("<i>").Append(text).Append("</i>");

        public static StringBuilder AppendSize(this StringBuilder sb, string text, int size)
            => sb.Append("<size=").Append(size).Append('>').Append(text).Append("</size>");

        public static StringBuilder AppendColor(this StringBuilder sb, string text, string color)
            => sb.Append("<color=").Append(color).Append('>').Append(text).Append("</color>");

        public static StringBuilder AppendColor(this StringBuilder sb, string text, int r, int g, int b)
            => sb.Append("<color=#")
            .Append(StringHelper._hexTable[(r >> 4) & 0xF])
            .Append(StringHelper._hexTable[r & 0xF])
            .Append(StringHelper._hexTable[(g >> 4) & 0xF])
            .Append(StringHelper._hexTable[g & 0xF])
            .Append(StringHelper._hexTable[(b >> 4) & 0xF])
            .Append(StringHelper._hexTable[b & 0xF])
            .Append('>').Append(text).Append("</color>");

        public static StringBuilder AppendColor(this StringBuilder sb, string text, Color32 color)
            => sb.AppendColor(text, color.r, color.g, color.b);

        public static StringBuilder AppendColor(this StringBuilder sb, string text, Color color)
        {
            Color32 c = color;
            return sb.AppendColor(text, c.r, c.g, c.b);
        }
    }
}
