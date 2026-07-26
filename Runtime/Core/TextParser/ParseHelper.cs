using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// A static helper class that facilitates Span-based parsing.
    /// Created for use with LineReader, but implemented for general use.
    /// </summary>
    public static partial class ParseHelper
    {
        [MethodImpl(Opt.Inline)] public static T To<T>(this ReadOnlySpan<char> span) => span.ToImpl<T, DefaultPolicy>(default);
        [MethodImpl(Opt.Inline)] public static T To<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToImpl<T, P>(policy);
        [MethodImpl(Opt.Inline)] public static T ToEnum<T>(this ReadOnlySpan<char> span) where T : struct, Enum => span.ToEnumImpl<T, DefaultPolicy>(default);
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T>(this ReadOnlySpan<char> span) => span.ToArrayImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default);
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToArrayImpl<T, P, Parser<T, P>>(policy, default);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T>(this ReadOnlySpan<char> span) => span.ToListImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToListImpl<T, P, Parser<T, P>>(policy, default);
        [MethodImpl(Opt.Inline)] public static HashSet<T> ToHashSet<T>(this ReadOnlySpan<char> span) => span.ToHashSetImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default);
        [MethodImpl(Opt.Inline)] public static HashSet<T> ToHashSet<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToHashSetImpl<T, P, Parser<T, P>>(policy, default);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> span) => span.ToDictionaryImpl<K, V, DefaultPolicy, Parser<K, DefaultPolicy>, Parser<V, DefaultPolicy>>(default, default, default);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToDictionaryImpl<K, V, P, Parser<K, P>, Parser<V, P>>(policy, default, default);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V>(this ReadOnlySpan<char> span) => span.ToPairImpl<K, V, DefaultPolicy, Parser<K, DefaultPolicy>, Parser<V, DefaultPolicy>>(default, default, default);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => span.ToPairImpl<K, V, P, Parser<K, P>, Parser<V, P>>(policy, default, default);

        [MethodImpl(Opt.Inline)] public static bool TryTo<T>(this ReadOnlySpan<char> span, out T result) => span.TryToImpl<T, DefaultPolicy>(default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryTo<T, P>(this ReadOnlySpan<char> span, out T result, P policy) where P : struct, IParsePolicy => span.TryToImpl(policy, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T>(this ReadOnlySpan<char> span, out T result) where T : struct, Enum => span.TryToEnumImpl<T, DefaultPolicy>(default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T>(this ReadOnlySpan<char> span, out T[] result) => span.TryToArrayImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T, P>(this ReadOnlySpan<char> span, out T[] result, P policy) where P : struct, IParsePolicy => span.TryToArrayImpl<T, P, Parser<T, P>>(policy, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T>(this ReadOnlySpan<char> span, out List<T> result) => span.TryToListImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T, P>(this ReadOnlySpan<char> span, out List<T> result, P policy) where P : struct, IParsePolicy => span.TryToListImpl<T, P, Parser<T, P>>(policy, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToHashSet<T>(this ReadOnlySpan<char> span, out HashSet<T> result) => span.TryToHashSetImpl<T, DefaultPolicy, Parser<T, DefaultPolicy>>(default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToHashSet<T, P>(this ReadOnlySpan<char> span, out HashSet<T> result, P policy) where P : struct, IParsePolicy => span.TryToHashSetImpl<T, P, Parser<T, P>>(policy, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V>(this ReadOnlySpan<char> span, out Dictionary<K, V> result) => span.TryToDictionaryImpl<K, V, DefaultPolicy, Parser<K, DefaultPolicy>, Parser<V, DefaultPolicy>>(default, default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V, P>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, P policy) where P : struct, IParsePolicy => span.TryToDictionaryImpl<K, V, P, Parser<K, P>, Parser<V, P>>(policy, default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V>(this ReadOnlySpan<char> span, out (K, V) result) => span.TryToPairImpl<K, V, DefaultPolicy, Parser<K, DefaultPolicy>, Parser<V, DefaultPolicy>>(default, default, default, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V, P>(this ReadOnlySpan<char> span, out (K, V) result, P policy) where P : struct, IParsePolicy => span.TryToPairImpl<K, V, P, Parser<K, P>, Parser<V, P>>(policy, default, default, out result);

        [MethodImpl(Opt.Inline)]
        public static int CountElements(this ReadOnlySpan<char> span, char separator, QuoteEscapeMode escapeMode)
        {
            int count = 1;
            int index = 0;
            while ((index = span.IndexOfUnquotedFromBoundary(separator, index, escapeMode)) != -1)
            {
                if (++index == span.Length)
                    break;

                ++count;
            }
            return count;
        }

    }
}
