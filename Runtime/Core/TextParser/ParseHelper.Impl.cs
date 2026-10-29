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

        [MethodImpl(Opt.Inline)]
        private static bool TryToImpl<T, P>(this ReadOnlySpan<char> span, P policy, out T result) where P : struct, IParsePolicy
        {
            if (typeof(T) == typeof(int)) return TryCast(span.TryToInt(out int vInt), vInt, out result);
            if (typeof(T) == typeof(float)) return TryCast(span.TryToFloat(out float vFloat), vFloat, out result);
            if (typeof(T) == typeof(string)) return TryCast(span.TryToStr(out string vStr), vStr, out result);
            if (typeof(T) == typeof(bool)) return TryCast(span.TryToBool(out bool vBool), vBool, out result);
            if (typeof(T) == typeof(long)) return TryCast(span.TryToLong(out long vLong), vLong, out result);
            if (typeof(T) == typeof(double)) return TryCast(span.TryToDouble(out double vDouble), vDouble, out result);
            if (typeof(T) == typeof(uint)) return TryCast(span.TryToUInt(out uint vUInt), vUInt, out result);
            if (typeof(T) == typeof(byte)) return TryCast(span.TryToByte(out byte vByte), vByte, out result);
            if (typeof(T) == typeof(short)) return TryCast(span.TryToShort(out short vShort), vShort, out result);
            if (typeof(T) == typeof(ushort)) return TryCast(span.TryToUShort(out ushort vUShort), vUShort, out result);
            if (typeof(T) == typeof(sbyte)) return TryCast(span.TryToSByte(out sbyte vSByte), vSByte, out result);
            if (typeof(T) == typeof(ulong)) return TryCast(span.TryToULong(out ulong vULong), vULong, out result);
            if (typeof(T) == typeof(DateTime)) return TryCast(span.TryToDateTime(out var vDateTime), vDateTime, out result);
            if (typeof(T).IsEnum) return span.TryToEnumImpl(policy, out result);

            result = default;
            return false;
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted('|', escapeMode);
                var token = index == -1 ? span : span[..index];
                if (!token.TryParseUnchecked(out T parsed, ignoreCase))
                    return false;

                EnumHelper.AccumulateFlagUnchecked(ref result, parsed);
                if (index == -1)
                    return true;

                span = span[(index + 1)..];
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
            int written = 0;
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                result[written++] = parser.Parse(index == -1 ? span : span[..index], policy);
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            int written = 0;
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                if (!parser.TryParse(index == -1 ? span : span[..index], policy, out var value))
                {
                    result = null;
                    return false;
                }
                candidate[written++] = value;
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                result.Add(parser.Parse(index == -1 ? span : span[..index], policy));
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                if (!parser.TryParse(index == -1 ? span : span[..index], policy, out var value))
                {
                    result = null;
                    return false;
                }
                candidate.Add(value);
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                var token = index == -1 ? span : span[..index];
                if (!result.Add(parser.Parse(token, policy)))
                    ThrowDuplicateItem(token);

                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                var token = index == -1 ? span : span[..index];
                if (!parser.TryParse(token, policy, out var value) || !candidate.Add(value))
                {
                    result = null;
                    return false;
                }
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                var token = index == -1 ? span : span[..index];
                var pair = token.ToPairImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser);
                result.Add(pair.Item1, pair.Item2);
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
            while (!span.IsEmpty)
            {
                int index = span.IndexOfUnquoted(separator, escapeMode);
                var token = index == -1 ? span : span[..index];
                if (!token.TryToPairImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser, out var pair) || !candidate.TryAdd(pair.Item1, pair.Item2))
                {
                    result = null;
                    return false;
                }
                if (index == -1)
                    break;

                span = span[(index + 1)..];
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
                ThrowInvalidPair(span);

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

        [MethodImpl(Opt.Inline)]
        private static bool TryCast<TSource, T>(bool success, TSource value, out T result)
        {
            result = success ? Unsafe.As<TSource, T>(ref value) : default;
            return success;
        }

        [MethodImpl(Opt.NoInline)]
        private static void ThrowDuplicateItem(ReadOnlySpan<char> token)
            => throw new ArgumentException($"duplicate item. text: {token.ToString()}");

        [MethodImpl(Opt.NoInline)]
        private static void ThrowInvalidPair(ReadOnlySpan<char> span)
            => throw new FormatException($"invalid pair format. text: {span.ToString()}");

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
