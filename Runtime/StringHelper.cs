using System;
using System.Text;
using UnityEngine;

namespace FerryKit
{
    public static class StringHelper
    {
        private const int BUFFER_SIZE_LIMIT = 1024;

        internal static readonly char[] _hexTable = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static ReadOnlySpan<char> HexTable => _hexTable;

        public static string Bold(this string str) => $"<b>{str}</b>";
        public static string Italic(this string str) => $"<i>{str}</i>";
        public static string Size(this string str, int size) => $"<size={size}>{str}</size>";
        public static string Color(this string str, string color) => $"<color={color}>{str}</color>";
        public static string Color(this string str, int r, int g, int b) => $"<color=#{r:X2}{g:X2}{b:X2}>{str}</color>";

        public static bool HasWhiteSpace(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            int length = str.Length;
            for (int i = 0; i < length; ++i)
            {
                if (char.IsWhiteSpace(str[i]))
                    return true;
            }
            return false;
        }

        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            if (maxLength <= suffix.Length)
                return suffix[..maxLength];

            int cutLength = maxLength - suffix.Length;
            return string.Create(maxLength, (str, suffix, cutLength), (span, state) =>
            {
                state.str.AsSpan(0, state.cutLength).CopyTo(span);
                state.suffix.AsSpan().CopyTo(span[state.cutLength..]);
            });
        }

        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
                return str;

            int len = str.Length;
            var buffer = len <= BUFFER_SIZE_LIMIT ? stackalloc char[len] : new char[len];
            str.AsSpan().CopyTo(buffer);
            for (int i = 0; i < len && char.IsUpper(buffer[i]); ++i)
            {
                if (i + 1 < len && !char.IsUpper(buffer[i + 1]))
                    break;

                buffer[i] = char.ToLowerInvariant(buffer[i]);
            }
            return new(buffer);
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsUpper(str[0]))
                return str;

            int len = str.Length;
            var buffer = len <= BUFFER_SIZE_LIMIT ? stackalloc char[len] : new char[len];
            str.AsSpan().CopyTo(buffer);
            buffer[0] = char.ToUpperInvariant(buffer[0]);
            return new(buffer);
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
