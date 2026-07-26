using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    // Internal parsing implementations are separated from the public facade for readability.
    public static partial class ParseHelper
    {
        [MethodImpl(Opt.Inline)]
        private static T ToImpl<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy
        {
            if (span.TryToImpl(policy, out T result))
                return result;

            return ThrowException<T>(span);
        }

        [MethodImpl(Opt.Inline)]
        private static T ToImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => parser.Parse(span, policy);

        private static bool TryToImpl<T, P>(this ReadOnlySpan<char> span, P policy, out T result) where P : struct, IParsePolicy
        {
            var type = typeof(T);
            return type switch
            {
                _ when type == typeof(sbyte) => TryCast(span.TryToSByte(out sbyte value), value, out result),
                _ when type == typeof(byte) => TryCast(span.TryToByte(out byte value), value, out result),
                _ when type == typeof(short) => TryCast(span.TryToShort(out short value), value, out result),
                _ when type == typeof(ushort) => TryCast(span.TryToUShort(out ushort value), value, out result),
                _ when type == typeof(int) => TryCast(span.TryToInt(out int value), value, out result),
                _ when type == typeof(uint) => TryCast(span.TryToUInt(out uint value), value, out result),
                _ when type == typeof(long) => TryCast(span.TryToLong(out long value), value, out result),
                _ when type == typeof(ulong) => TryCast(span.TryToULong(out ulong value), value, out result),
                _ when type == typeof(float) => TryCast(span.TryToFloat(out float value), value, out result),
                _ when type == typeof(double) => TryCast(span.TryToDouble(out double value), value, out result),
                _ when type == typeof(bool) => TryCast(span.TryToBool(out bool value), value, out result),
                _ when type == typeof(string) => TryCast(span.TryToStr(out string value), value, out result),
                _ when type == typeof(DateTime) => TryCast(span.TryToDateTime(out var value), value, out result),
                _ when type.IsEnum => span.TryToEnumImpl(policy, out result),
                _ => TryCast(false, default(T), out result),
            };
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser, out T result)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => parser.TryParse(span, policy, out result);

        [MethodImpl(Opt.Inline)]
        private static T ToEnumImpl<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy
        {
            if (span.TryToEnumImpl(policy, out T result))
                return result;

            return ThrowException<T>(span);
        }

        private static bool TryToEnumImpl<T, P>(this ReadOnlySpan<char> span, P policy, out T result) where P : struct, IParsePolicy
        {
            result = default;
            if (span.IsWhiteSpace())
                return false;

            var escapeMode = policy.EscapeMode;
            bool ignoreCase = policy.IgnoreCase;
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary('|', start, escapeMode);
                var token = index == -1 ? span[start..] : span[start..index];
                if (!token.TryParseUnchecked(out T parsed, ignoreCase))
                    return false;

                EnumHelper.AccumulateFlagUnchecked(ref result, parsed);
                if (index == -1)
                    return true;

                start = index + 1;
            }
            return true;
        }

        private static T[] ToArrayImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
                return Array.Empty<T>();

            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            var result = new T[span.CountElements(separator, escapeMode)];
            int start = 0;
            int written = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                result[written++] = parser.Parse(index == -1 ? span[start..] : span[start..index], policy);
                if (index == -1)
                    break;

                start = index + 1;
            }
            return result;
        }

        private static bool TryToArrayImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser, out T[] result)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
            {
                result = Array.Empty<T>();
                return true;
            }
            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            var candidate = new T[span.CountElements(separator, escapeMode)];
            if (!span.TryParseElementsImpl(candidate.AsSpan(), policy, parser, out _))
            {
                result = null;
                return false;
            }
            result = candidate;
            return true;
        }

        private static List<T> ToListImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
                return new();

            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            var result = new List<T>(span.CountElements(separator, escapeMode));
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                result.Add(parser.Parse(index == -1 ? span[start..] : span[start..index], policy));
                if (index == -1)
                    break;

                start = index + 1;
            }
            return result;
        }

        private static bool TryToListImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser, out List<T> result)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
            {
                result = new();
                return true;
            }
            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            var candidate = new List<T>(span.CountElements(separator, escapeMode));
            if (!span.TryAppendImpl(candidate, policy, parser, out _))
            {
                result = null;
                return false;
            }
            result = candidate;
            return true;
        }

        private static HashSet<T> ToHashSetImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
                return new();

            char separator = policy.SetSep;
            var escapeMode = policy.EscapeMode;
            var result = new HashSet<T>(span.CountElements(separator, escapeMode));
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                var token = index == -1 ? span[start..] : span[start..index];
                if (!result.Add(parser.Parse(token, policy)))
                    throw new ArgumentException($"duplicate item. text: {token.ToString()}");

                if (index == -1)
                    break;

                start = index + 1;
            }
            return result;
        }

        private static bool TryToHashSetImpl<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser, out HashSet<T> result)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (span.IsWhiteSpace())
            {
                result = new();
                return true;
            }
            char separator = policy.SetSep;
            var escapeMode = policy.EscapeMode;
            var candidate = new HashSet<T>(span.CountElements(separator, escapeMode));
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                var token = index == -1 ? span[start..] : span[start..index];
                if (!parser.TryParse(token, policy, out var value) || !candidate.Add(value))
                {
                    result = null;
                    return false;
                }
                if (index == -1)
                    break;

                start = index + 1;
            }
            result = candidate;
            return true;
        }

        private static Dictionary<K, V> ToDictionaryImpl<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
        {
            if (span.IsWhiteSpace())
                return new();

            char separator = policy.DictSep;
            var escapeMode = policy.EscapeMode;
            var result = new Dictionary<K, V>(span.CountElements(separator, escapeMode));
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                var token = index == -1 ? span[start..] : span[start..index];
                var pair = token.ToPairImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser);
                result.Add(pair.Item1, pair.Item2);
                if (index == -1)
                    break;

                start = index + 1;
            }
            return result;
        }

        private static bool TryToDictionaryImpl<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser, out Dictionary<K, V> result)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
        {
            if (span.IsWhiteSpace())
            {
                result = new();
                return true;
            }
            char separator = policy.DictSep;
            var escapeMode = policy.EscapeMode;
            var candidate = new Dictionary<K, V>(span.CountElements(separator, escapeMode));
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                var token = index == -1 ? span[start..] : span[start..index];
                if (!token.TryToPairImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser, out var pair) || !candidate.TryAdd(pair.Item1, pair.Item2))
                {
                    result = null;
                    return false;
                }
                if (index == -1)
                    break;

                start = index + 1;
            }
            result = candidate;
            return true;
        }

        private static (K, V) ToPairImpl<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
        {
            int index = span.IndexOfUnquoted(policy.PairSep, policy.EscapeMode);
            if (index == -1)
                throw new FormatException($"invalid pair format. text: {span.ToString()}");

            return (keyParser.Parse(span[..index], policy), valueParser.Parse(span[(index + 1)..], policy));
        }

        private static bool TryToPairImpl<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser, out (K, V) result)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
        {
            int index = span.IndexOfUnquoted(policy.PairSep, policy.EscapeMode);
            if (index != -1 && keyParser.TryParse(span[..index], policy, out var key) && valueParser.TryParse(span[(index + 1)..], policy, out var value))
            {
                result = (key, value);
                return true;
            }
            result = default;
            return false;
        }

        private static bool TryParseElementsImpl<T, P, TParser>(this ReadOnlySpan<char> span, Span<T> destination, P policy, TParser parser, out int written)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            written = 0;
            if (span.IsWhiteSpace())
                return true;

            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            int start = 0;
            while (start < span.Length)
            {
                if (written >= destination.Length)
                    return false;

                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                if (!parser.TryParse(index == -1 ? span[start..] : span[start..index], policy, out var value))
                    return false;

                destination[written++] = value;
                if (index == -1)
                    return true;

                start = index + 1;
            }
            return true;
        }

        private static bool TryAppendImpl<T, P, TParser>(this ReadOnlySpan<char> span, List<T> destination, P policy, TParser parser, out int appendedCount)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            int originalCount = destination.Count;
            if (span.IsWhiteSpace())
            {
                appendedCount = 0;
                return true;
            }
            char separator = policy.ArrSep;
            var escapeMode = policy.EscapeMode;
            int start = 0;
            while (start < span.Length)
            {
                int index = span.IndexOfUnquotedFromBoundary(separator, start, escapeMode);
                if (!parser.TryParse(index == -1 ? span[start..] : span[start..index], policy, out var value))
                {
                    destination.RemoveRange(originalCount, destination.Count - originalCount);
                    appendedCount = 0;
                    return false;
                }
                destination.Add(value);
                if (index == -1)
                    break;

                start = index + 1;
            }
            appendedCount = destination.Count - originalCount;
            return true;
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryCast<TSource, T>(bool success, TSource value, out T result)
        {
            result = success ? Unsafe.As<TSource, T>(ref value) : default;
            return success;
        }

        [MethodImpl(Opt.NoInline)]
        private static T ThrowException<T>(ReadOnlySpan<char> span)
        {
            var type = typeof(T);
            if (type == typeof(int)
                || type == typeof(long)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(string)
                || type == typeof(bool)
                || type == typeof(DateTime)
                || type.IsEnum)
                throw new FormatException($"'{span.ToString()}' could not be converted to '{type}'.");

            throw new NotSupportedException($"no default parse parser for '{type}'.");
        }
    }
}
