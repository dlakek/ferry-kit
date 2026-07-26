using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static partial class ParseHelper
    {
        [MethodImpl(Opt.Inline)]
        public static T ToWith<T, TParser>(this ReadOnlySpan<char> span, TParser parser)
            where TParser : struct, IParser<T>
            => span.ToImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser));

        [MethodImpl(Opt.Inline)]
        public static T ToWith<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.ToImpl<T, P, TParser>(policy, parser);

        [MethodImpl(Opt.Inline)]
        public static T[] ToArrayWith<T, TParser>(this ReadOnlySpan<char> span, TParser parser)
            where TParser : struct, IParser<T>
            => span.ToArrayImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser));

        [MethodImpl(Opt.Inline)]
        public static T[] ToArrayWith<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.ToArrayImpl<T, P, TParser>(policy, parser);

        [MethodImpl(Opt.Inline)]
        public static List<T> ToListWith<T, TParser>(this ReadOnlySpan<char> span, TParser parser)
            where TParser : struct, IParser<T>
            => span.ToListImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser));

        [MethodImpl(Opt.Inline)]
        public static List<T> ToListWith<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.ToListImpl<T, P, TParser>(policy, parser);

        [MethodImpl(Opt.Inline)]
        public static HashSet<T> ToHashSetWith<T, TParser>(this ReadOnlySpan<char> span, TParser parser)
            where TParser : struct, IParser<T>
            => span.ToHashSetImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser));

        [MethodImpl(Opt.Inline)]
        public static HashSet<T> ToHashSetWith<T, P, TParser>(this ReadOnlySpan<char> span, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.ToHashSetImpl<T, P, TParser>(policy, parser);

        [MethodImpl(Opt.Inline)]
        public static Dictionary<K, V> ToDictionaryWith<K, V, TKParser, TVParser>(this ReadOnlySpan<char> span, TKParser keyParser, TVParser valueParser)
            where TKParser : struct, IParser<K>
            where TVParser : struct, IParser<V>
            => span.ToDictionaryImpl<K, V, DefaultPolicy, ParserAdapter<K, DefaultPolicy, TKParser>, ParserAdapter<V, DefaultPolicy, TVParser>>(default, new(keyParser), new(valueParser));

        [MethodImpl(Opt.Inline)]
        public static Dictionary<K, V> ToDictionaryWith<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
            => span.ToDictionaryImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser);

        [MethodImpl(Opt.Inline)]
        public static (K, V) ToPairWith<K, V, TKParser, TVParser>(this ReadOnlySpan<char> span, TKParser keyParser, TVParser valueParser)
            where TKParser : struct, IParser<K>
            where TVParser : struct, IParser<V>
            => span.ToPairImpl<K, V, DefaultPolicy, ParserAdapter<K, DefaultPolicy, TKParser>, ParserAdapter<V, DefaultPolicy, TVParser>>(default, new(keyParser), new(valueParser));

        [MethodImpl(Opt.Inline)]
        public static (K, V) ToPairWith<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
            => span.ToPairImpl<K, V, P, TKParser, TVParser>(policy, keyParser, valueParser);

        [MethodImpl(Opt.Inline)]
        public static bool TryToWith<T, TParser>(this ReadOnlySpan<char> span, out T result, TParser parser)
            where TParser : struct, IParser<T>
            => span.TryToImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToWith<T, P, TParser>(this ReadOnlySpan<char> span, out T result, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.TryToImpl(policy, parser, out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToArrayWith<T, TParser>(this ReadOnlySpan<char> span, out T[] result, TParser parser)
            where TParser : struct, IParser<T>
            => span.TryToArrayImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToArrayWith<T, P, TParser>(this ReadOnlySpan<char> span, out T[] result, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.TryToArrayImpl(policy, parser, out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToListWith<T, TParser>(this ReadOnlySpan<char> span, out List<T> result, TParser parser)
            where TParser : struct, IParser<T>
            => span.TryToListImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToListWith<T, P, TParser>(this ReadOnlySpan<char> span, out List<T> result, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.TryToListImpl(policy, parser, out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToHashSetWith<T, TParser>(this ReadOnlySpan<char> span, out HashSet<T> result, TParser parser)
            where TParser : struct, IParser<T>
            => span.TryToHashSetImpl<T, DefaultPolicy, ParserAdapter<T, DefaultPolicy, TParser>>(default, new(parser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToHashSetWith<T, P, TParser>(this ReadOnlySpan<char> span, out HashSet<T> result, P policy, TParser parser)
            where P : struct, IParsePolicy
            where TParser : struct, IParser<T, P>
            => span.TryToHashSetImpl(policy, parser, out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToDictionaryWith<K, V, TKParser, TVParser>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, TKParser keyParser, TVParser valueParser)
            where TKParser : struct, IParser<K>
            where TVParser : struct, IParser<V>
            => span.TryToDictionaryImpl<K, V, DefaultPolicy, ParserAdapter<K, DefaultPolicy, TKParser>, ParserAdapter<V, DefaultPolicy, TVParser>>(default, new(keyParser), new(valueParser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToDictionaryWith<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
            => span.TryToDictionaryImpl(policy, keyParser, valueParser, out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToPairWith<K, V, TKParser, TVParser>(this ReadOnlySpan<char> span, out (K, V) result, TKParser keyParser, TVParser valueParser)
            where TKParser : struct, IParser<K>
            where TVParser : struct, IParser<V>
            => span.TryToPairImpl<K, V, DefaultPolicy, ParserAdapter<K, DefaultPolicy, TKParser>, ParserAdapter<V, DefaultPolicy, TVParser>>(default, new(keyParser), new(valueParser), out result);

        [MethodImpl(Opt.Inline)]
        public static bool TryToPairWith<K, V, P, TKParser, TVParser>(this ReadOnlySpan<char> span, out (K, V) result, P policy, TKParser keyParser, TVParser valueParser)
            where P : struct, IParsePolicy
            where TKParser : struct, IParser<K, P>
            where TVParser : struct, IParser<V, P>
            => span.TryToPairImpl(policy, keyParser, valueParser, out result);
    }
}
