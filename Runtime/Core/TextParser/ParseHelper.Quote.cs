using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public enum QuoteEscapeMode : byte
    {
        None,
        Csv,
        Backslash,
    }

    public static partial class ParseHelper
    {
        private const int _stackallocUnescapeLimit = 512;

        [MethodImpl(Opt.Inline)]
        public static bool TryGetUnescapeCsvQuotes<T>(this ReadOnlySpan<char> cell, out T result)
        {
            if (typeof(T) == typeof(string))
            {
                var value = cell.UnescapeCsvQuotes();
                result = Unsafe.As<string, T>(ref value);
                return true;
            }
            result = default;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, QuoteEscapeMode escapeMode = QuoteEscapeMode.None) => escapeMode switch
        {
            QuoteEscapeMode.Csv => span.IndexOfUnquotedCsv(value),
            QuoteEscapeMode.Backslash => span.IndexOfUnquotedWithEscape(value),
            _ => span.IndexOfUnquotedNoEscape(value),
        };

        [MethodImpl(Opt.Inline)]
        public static int IndexOfUnquoted(this ReadOnlySpan<char> span, char value, int startIndex, QuoteEscapeMode escapeMode = QuoteEscapeMode.None)
        {
            if ((uint)startIndex >= (uint)span.Length)
                return -1;

            int boundary = 0;
            while (true)
            {
                int index = span.IndexOfUnquotedFromBoundary(value, boundary, escapeMode);
                if (index == -1 || index >= startIndex)
                    return index;

                boundary = index + 1;
            }
        }

        [MethodImpl(Opt.Inline)]
        internal static int IndexOfUnquotedFromBoundary(this ReadOnlySpan<char> span, char value, int startIndex, QuoteEscapeMode escapeMode)
        {
            if ((uint)startIndex >= (uint)span.Length)
                return -1;

            var slice = span[startIndex..];
            int index = slice.IndexOfUnquoted(value, escapeMode);
            return index == -1 ? -1 : startIndex + index;
        }

        private static int IndexOfUnquotedCsv(this ReadOnlySpan<char> span, char value)
        {
            var inQuotes = false;
            int offset = 0;
            int length = span.Length;
            while (offset < length)
            {
                var slice = span[offset..];
                int index = inQuotes ? slice.IndexOf('"') : slice.IndexOfAny('"', value);
                if (index == -1)
                    return -1;

                if (!inQuotes)
                {
                    if (slice[index] == value)
                        return offset + index;

                    inQuotes = true;
                    offset += index + 1;
                    continue;
                }
                int next = offset + index + 1;
                if (next < length && span[next] == '"')
                {
                    offset = next + 1;
                    continue;
                }
                inQuotes = false;
                offset = next;
            }
            return -1;
        }

        private static int IndexOfUnquotedWithEscape(this ReadOnlySpan<char> span, char value)
        {
            var inQuotes = false;
            int offset = 0;
            int length = span.Length;
            while (true)
            {
                var slice = span[offset..];
                int index = inQuotes ? slice.IndexOfAny('"', '\\') : slice.IndexOfAny('"', value);
                if (index == -1)
                    return -1;

                var character = slice[index];
                if (character == '\\')
                {
                    offset += index + 2;
                    if (offset >= length)
                        return -1;

                    continue;
                }
                if (character != '"')
                    return offset + index;

                inQuotes = !inQuotes;
                offset += index + 1;
            }
        }

        private static int IndexOfUnquotedNoEscape(this ReadOnlySpan<char> span, char value)
        {
            var inQuotes = false;
            int offset = 0;
            while (true)
            {
                var slice = span[offset..];
                int index = inQuotes ? slice.IndexOf('"') : slice.IndexOfAny('"', value);
                if (index == -1)
                    return -1;

                if (slice[index] != '"')
                    return offset + index;

                inQuotes = !inQuotes;
                offset += index + 1;
            }
        }

        private static string UnescapeCsvQuotes(this ReadOnlySpan<char> span)
        {
            int escapedQuoteCount = 0;
            int offset = 0;
            while (offset + 1 < span.Length)
            {
                int index = span[offset..].IndexOf('"');
                if (index == -1)
                    break;

                offset += index + 1;
                if (offset < span.Length && span[offset] == '"')
                {
                    ++escapedQuoteCount;
                    ++offset;
                }
            }
            if (escapedQuoteCount == 0)
                return span.ToString();

            int resultLength = span.Length - escapedQuoteCount;
            if (resultLength <= _stackallocUnescapeLimit)
            {
                Span<char> buffer = stackalloc char[resultLength];
                span.UnescapeCsvQuotesTo(buffer);
                return buffer.ToString();
            }
            var rented = ArrayPool<char>.Shared.Rent(resultLength);
            try
            {
                var buffer = rented.AsSpan(0, resultLength);
                span.UnescapeCsvQuotesTo(buffer);
                return buffer.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }

        [MethodImpl(Opt.Inline)]
        private static void UnescapeCsvQuotesTo(this ReadOnlySpan<char> source, Span<char> destination)
        {
            int written = 0;
            for (int i = 0; i < source.Length; ++i)
            {
                char value = source[i];
                destination[written++] = value;
                if (value == '"' && i + 1 < source.Length && source[i + 1] == '"')
                {
                    ++i;
                }
            }
        }
    }
}
