using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// 구조체 제약을 통해 오버헤드 없이 필요시에만 Config를 전달할 수 있도록 하기 위한 인터페이스
    /// </summary>
    public interface IParsePolicy
    {
        char ColSep { get; }
        char ArrSep { get; }
        char DictSep { get; }
        char PairSep { get; }
    }

    /// <summary>
    /// TextParser를 통해 파싱 가능한 객체 인터페이스
    /// </summary>
    public interface IParsable
    {
        bool Parse<P>(LineReader<P> reader) where P : struct, IParsePolicy;
    }

    /// <summary>
    /// 기본적으로 CSV 파싱을 위해 사용되지만, 다른 형태의 텍스트도 파싱할 수 있도록 구현된 범용 파서
    /// </summary>
    public static class TextParser
    {
        private const int _estimateLengthPerLine = 50;


        public static List<T> Load<T>(string text, int reserveLine = 0, bool isSkipFirstLine = false)
            where T : IParsable, new()
        {
            return Load<T, ParseHelper.Default>(text, default, reserveLine, isSkipFirstLine);
        }

        public static List<T> Load<T, P>(string text, P policy, int reserveLine = 0, bool isSkipFirstLine = false)
            where T : IParsable, new()
            where P : struct, IParsePolicy
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            var lineSplitter = LineSplitter.From(text);
            if (isSkipFirstLine)
            {
                lineSplitter.MoveNext();
            }
            var result = new List<T>(reserveLine > 0 ? reserveLine : text.Length / _estimateLengthPerLine);
            foreach (var row in lineSplitter)
            {
                if (row.IsWhiteSpace())
                    continue;

                var data = ExpressionCache<T>.New();
                if (data.Parse(LineReader<P>.From(row, policy)))
                {
                    result.Add(data);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 전달받은 문자열을 한 줄씩 읽어나가는 구조체
    /// </summary>
    public ref struct LineSplitter
    {
        private ReadOnlySpan<char> _remain;
        private ReadOnlySpan<char> _curRow;

        public readonly ReadOnlySpan<char> Current
        {
            [MethodImpl(Opt.Inline)]
            get => _curRow;
        }

        [MethodImpl(Opt.Inline)] public readonly LineSplitter GetEnumerator() => this;
        [MethodImpl(Opt.Inline)] public static LineSplitter From(ReadOnlySpan<char> text) => new(text);

        [MethodImpl(Opt.Inline)]
        public LineSplitter(ReadOnlySpan<char> text)
        {
            _remain = text;
            _curRow = default;
        }

        [MethodImpl(Opt.Inline)]
        public bool MoveNext()
        {
            if (_remain.Length == 0)
                return false;

            int idx = _remain.IndexOfUnquoted('\n');
            if (idx == -1)
            {
                _curRow = _remain.TrimEndOne('\r');
                _remain = ReadOnlySpan<char>.Empty;
            }
            else
            {
                _curRow = _remain[..idx].TrimEndOne('\r');
                _remain = _remain[(idx + 1)..];
            }
            return true;
        }
    }

    /// <summary>
    /// LineSplitter로 분리한 각 줄에 대한 파싱을 담당하는 구조체
    /// </summary>
    public ref struct LineReader<P> where P : struct, IParsePolicy
    {
        private readonly ReadOnlySpan<char> _line;
        private readonly P _policy;
        private int _curIdx;

        [MethodImpl(Opt.Inline)] public T Read<T>() => ReadNext().To<T, P>(_policy);
        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out T result) => ReadNext().TryTo(out result, _policy);
        [MethodImpl(Opt.Inline)] public static LineReader<P> From(ReadOnlySpan<char> line, P policy) => new(line, policy);

        [MethodImpl(Opt.Inline)]
        public LineReader(ReadOnlySpan<char> line, P policy)
        {
            _line = line;
            _policy = policy;
            _curIdx = 0;
        }

        public ReadOnlySpan<char> ReadNext()
        {
            if (_curIdx >= _line.Length)
                return ReadOnlySpan<char>.Empty;

            var remaining = _line[_curIdx..];
            int nextComma = remaining.IndexOfUnquoted(_policy.ColSep);
            if (nextComma == -1)
            {
                _curIdx = _line.Length;
                return remaining;
            }
            else
            {
                _curIdx += nextComma + 1;
                return remaining[..nextComma];
            }
        }
    }

    /// <summary>
    /// Span 기반 파싱을 돕는 정적 헬퍼 클래스
    /// LineReader에서 쓰기 위해 만들었지만, 범용적으로 사용도 가능하게 구현함
    /// </summary>
    public static class ParseHelper
    {
        public const char ColSep = ',';
        public const char ArrSep = ';';
        public const char DictSep = '|';
        public const char PairSep = '=';

        public readonly struct Default : IParsePolicy
        {
            public char ColSep => ParseHelper.ColSep;
            public char ArrSep => ParseHelper.ArrSep;
            public char DictSep => ParseHelper.DictSep;
            public char PairSep => ParseHelper.PairSep;
        }

        public readonly struct Custom : IParsePolicy
        {
            public char ColSep { get; }
            public char ArrSep { get; }
            public char DictSep { get; }
            public char PairSep { get; }

            public Custom(char columnSep, char arraySep = ParseHelper.ArrSep, char dictSep = ParseHelper.DictSep, char pairSep = ParseHelper.PairSep)
            {
                ColSep = columnSep;
                ArrSep = arraySep;
                DictSep = dictSep;
                PairSep = pairSep;
            }
        }

        public delegate T Parser<T, P>(ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy;
        public delegate bool TryParser<T, P>(ReadOnlySpan<char> span, out T result, P policy) where P : struct, IParsePolicy;

        [MethodImpl(Opt.Inline)] public static T To<T>(this ReadOnlySpan<char> span) => Cache<T, Default>.Parse(span, default);
        [MethodImpl(Opt.Inline)] public static T To<T, P>(this ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy => Cache<T, P>.Parse(span, policy);
        [MethodImpl(Opt.Inline)] public static bool TryTo<T>(this ReadOnlySpan<char> span, out T result) => Cache<T, Default>.TryParse(span, out result, default);
        [MethodImpl(Opt.Inline)] public static bool TryTo<T, P>(this ReadOnlySpan<char> span, out T result, P policy) where P : struct, IParsePolicy => Cache<T, P>.TryParse(span, out result, policy);

        [MethodImpl(Opt.Inline)] public static int ToInt(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : int.Parse(span);
        [MethodImpl(Opt.Inline)] public static long ToLong(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : long.Parse(span);
        [MethodImpl(Opt.Inline)] public static float ToFloat(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : float.Parse(span);
        [MethodImpl(Opt.Inline)] public static double ToDouble(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : double.Parse(span);
        [MethodImpl(Opt.Inline)] public static bool ToBool(this ReadOnlySpan<char> span) => span.Length == 1 && span[0] == '1' || bool.Parse(span);
        [MethodImpl(Opt.Inline)] public static string ToStr(this ReadOnlySpan<char> span) => span.ToString();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime(this ReadOnlySpan<char> span) => span.IsEmpty ? default : DateTime.Parse(span);

        [MethodImpl(Opt.Inline)] public static int ToInt<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToInt();
        [MethodImpl(Opt.Inline)] public static long ToLong<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToLong();
        [MethodImpl(Opt.Inline)] public static float ToFloat<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToFloat();
        [MethodImpl(Opt.Inline)] public static double ToDouble<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToDouble();
        [MethodImpl(Opt.Inline)] public static bool ToBool<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToBool();
        [MethodImpl(Opt.Inline)] public static string ToStr<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToStr();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime<P>(this ReadOnlySpan<char> span, P _) where P : struct, IParsePolicy => span.ToDateTime();

        [MethodImpl(Opt.Inline)] public static bool TryToInt(this ReadOnlySpan<char> span, out int result) => int.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToLong(this ReadOnlySpan<char> span, out long result) => long.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat(this ReadOnlySpan<char> span, out float result) => float.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble(this ReadOnlySpan<char> span, out double result) => double.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToBool(this ReadOnlySpan<char> span, out bool result) => span.Length == 1 && span[0] == '1' ? result = true : bool.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToStr(this ReadOnlySpan<char> span, out string result) => !string.IsNullOrEmpty(result = span.ToStr());
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime(this ReadOnlySpan<char> span, out DateTime result) => DateTime.TryParse(span, out result);

        [MethodImpl(Opt.Inline)] public static bool TryToInt<P>(this ReadOnlySpan<char> span, out int result, P _) where P : struct, IParsePolicy => span.TryToInt(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToLong<P>(this ReadOnlySpan<char> span, out long result, P _) where P : struct, IParsePolicy => span.TryToLong(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat<P>(this ReadOnlySpan<char> span, out float result, P _) where P : struct, IParsePolicy => span.TryToFloat(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble<P>(this ReadOnlySpan<char> span, out double result, P _) where P : struct, IParsePolicy => span.TryToDouble(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToBool<P>(this ReadOnlySpan<char> span, out bool result, P _) where P : struct, IParsePolicy => span.TryToBool(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToStr<P>(this ReadOnlySpan<char> span, out string result, P _) where P : struct, IParsePolicy => span.TryToStr(out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime<P>(this ReadOnlySpan<char> span, out DateTime result, P _) where P : struct, IParsePolicy => span.TryToDateTime(out result);

        [MethodImpl(Opt.Inline)] public static T ToEnum<T>(this ReadOnlySpan<char> s) where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T>(this ReadOnlySpan<char> s) => s.ToArrayImpl(Cache<T, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T>(this ReadOnlySpan<char> s) => s.ToListImpl(Cache<T, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V>(this ReadOnlySpan<char> s) => s.ToPairImpl(Cache<K, Default>.Parse, Cache<V, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> s) => s.ToDictionaryImpl(Cache<K, Default>.Parse, Cache<V, Default>.Parse);

        [MethodImpl(Opt.Inline)] public static T ToEnum<T, P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToArrayImpl(Cache<T, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToListImpl(Cache<T, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToPairImpl(Cache<K, P>.Parse, Cache<V, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToDictionaryImpl(Cache<K, P>.Parse, Cache<V, P>.Parse, p);

        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T>(this ReadOnlySpan<char> s, out T r) where T : struct, Enum => s.TryParse(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T>(this ReadOnlySpan<char> s, out T[] r) => s.TryToArrayImpl(out r, Cache<T, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T>(this ReadOnlySpan<char> s, out List<T> r) => s.TryToListImpl(out r, Cache<T, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V>(this ReadOnlySpan<char> s, out (K, V) r) => s.TryToPairImpl(out r, Cache<K, Default>.TryParse, Cache<V, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V>(this ReadOnlySpan<char> s, out Dictionary<K, V> r) => s.TryToDictionaryImpl(out r, Cache<K, Default>.TryParse, Cache<V, Default>.TryParse);

        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T, P>(this ReadOnlySpan<char> s, out T r, P _) where P : struct, IParsePolicy where T : struct, Enum => s.TryParse(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T, P>(this ReadOnlySpan<char> s, out T[] r, P p) where P : struct, IParsePolicy => s.TryToArrayImpl(out r, Cache<T, P>.TryParse, p);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T, P>(this ReadOnlySpan<char> s, out List<T> r, P p) where P : struct, IParsePolicy => s.TryToListImpl(out r, Cache<T, P>.TryParse, p);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V, P>(this ReadOnlySpan<char> s, out (K, V) r, P p) where P : struct, IParsePolicy => s.TryToPairImpl(out r, Cache<K, P>.TryParse, Cache<V, P>.TryParse, p);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V, P>(this ReadOnlySpan<char> s, out Dictionary<K, V> r, P p) where P : struct, IParsePolicy => s.TryToDictionaryImpl(out r, Cache<K, P>.TryParse, Cache<V, P>.TryParse, p);

        private static T[] ToArrayImpl<T, P>(this ReadOnlySpan<char> span, Parser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return Array.Empty<T>();

            var sep = policy.ArrSep;
            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                if (idx == -1)
                    break;

                start = idx + 1;
                ++count;
            }
            var result = new T[count];
            int arrIdx = 0;
            start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                if (idx == -1)
                {
                    result[arrIdx] = parser(span[start..], policy);
                    break;
                }
                result[arrIdx++] = parser(span[start..idx], policy);
                start = idx + 1;
            }
            return result;
        }

        private static bool TryToArrayImpl<T, P>(this ReadOnlySpan<char> span, out T[] result, TryParser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            var sep = policy.ArrSep;
            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                if (idx == -1)
                    break;

                start = idx + 1;
                ++count;
            }
            var ret = new T[count];
            int arrIdx = 0;
            start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                if (idx == -1)
                {
                    if (!parser(span[start..], out ret[arrIdx], policy))
                        return false;

                    break;
                }
                if (!parser(span[start..], out ret[arrIdx++], policy))
                    return false;

                start = idx + 1;
            }
            result = ret;
            return true;
        }

        [MethodImpl(Opt.Inline)]
        private static List<T> ToListImpl<T, P>(this ReadOnlySpan<char> span, Parser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            return new(span.ToArrayImpl(parser, policy));
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToListImpl<T, P>(this ReadOnlySpan<char> span, out List<T> result, TryParser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.TryToArrayImpl(out var arrayResult, parser, policy))
            {
                result = new(arrayResult);
                return true;
            }
            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static (K, V) ToPairImpl<K, V, P>(this ReadOnlySpan<char> span, Parser<K, P> kp, Parser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            int idx = span.IndexOfUnquoted(policy.PairSep);
            if (idx == -1)
                return default;

            return (kp(span[..idx], policy), vp(span[(idx + 1)..], policy));
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToPairImpl<K, V, P>(this ReadOnlySpan<char> span, out (K, V) result, TryParser<K, P> kp, TryParser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            int idx = span.IndexOfUnquoted(policy.PairSep);
            if (idx != -1)
            {
                if (kp(span[..idx], out K kr, policy) && vp(span[(idx + 1)..], out V vr, policy))
                {
                    result = (kr, vr);
                    return true;
                }
            }
            result = default;
            return false;
        }

        private static Dictionary<K, V> ToDictionaryImpl<K, V, P>(this ReadOnlySpan<char> span, Parser<K, P> kp, Parser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            var result = new Dictionary<K, V>();
            if (span.IsEmpty || span.IsWhiteSpace())
                return result;

            var sep = policy.DictSep;
            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                var (k, v) = sub.ToPairImpl(kp, vp, policy);
                result.Add(k, v);
                if (idx == -1)
                    break;

                start = idx + 1;
            }
            return result;
        }

        private static bool TryToDictionaryImpl<K, V, P>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, TryParser<K, P> kp, TryParser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            var ret = new Dictionary<K, V>();
            var sep = policy.DictSep;
            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                if (!sub.TryToPairImpl(out var pair, kp, vp, policy))
                    return false;

                if (!ret.TryAdd(pair.Item1, pair.Item2))
                    return false;

                if (idx == -1)
                    break;

                start = idx + 1;
            }
            result = ret;
            return true;
        }

        private static class Cache<T, P> where P : struct, IParsePolicy
        {
            public static readonly Parser<T, P> Parse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (Parser<T, P>)(object)(Parser<int, P>)ToInt,
                _ when typeof(T) == typeof(long) => (Parser<T, P>)(object)(Parser<long, P>)ToLong,
                _ when typeof(T) == typeof(float) => (Parser<T, P>)(object)(Parser<float, P>)ToFloat,
                _ when typeof(T) == typeof(double) => (Parser<T, P>)(object)(Parser<double, P>)ToDouble,
                _ when typeof(T) == typeof(bool) => (Parser<T, P>)(object)(Parser<bool, P>)ToBool,
                _ when typeof(T) == typeof(string) => (Parser<T, P>)(object)(Parser<string, P>)ToStr,
                _ when typeof(T) == typeof(DateTime) => (Parser<T, P>)(object)(Parser<DateTime, P>)ToDateTime,
                _ when typeof(T).IsEnum => CreateParser(nameof(ToEnum), typeof(T), typeof(P)),
                _ when typeof(T).IsArray => CreateParser(nameof(ToArray), typeof(T).GetElementType(), typeof(P)),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateParser(nameof(ToList), typeof(T).GetGenericArguments()[0], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateParser(nameof(ToDictionary), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ => throw new ArgumentException($"no parser for {Parse}<{typeof(T).Name}>")
            };

            public static readonly TryParser<T, P> TryParse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (TryParser<T, P>)(object)(TryParser<int, P>)TryToInt,
                _ when typeof(T) == typeof(long) => (TryParser<T, P>)(object)(TryParser<long, P>)TryToLong,
                _ when typeof(T) == typeof(float) => (TryParser<T, P>)(object)(TryParser<float, P>)TryToFloat,
                _ when typeof(T) == typeof(double) => (TryParser<T, P>)(object)(TryParser<double, P>)TryToDouble,
                _ when typeof(T) == typeof(bool) => (TryParser<T, P>)(object)(TryParser<bool, P>)TryToBool,
                _ when typeof(T) == typeof(string) => (TryParser<T, P>)(object)(TryParser<string, P>)TryToStr,
                _ when typeof(T) == typeof(DateTime) => (TryParser<T, P>)(object)(TryParser<DateTime, P>)TryToDateTime,
                _ when typeof(T).IsEnum => CreateTryParser(nameof(ToEnum), typeof(T), typeof(P)),
                _ when typeof(T).IsArray => CreateTryParser(nameof(ToArray), typeof(T).GetElementType(), typeof(P)),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateTryParser(nameof(ToList), typeof(T).GetGenericArguments()[0], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateTryParser(nameof(ToDictionary), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ => throw new ArgumentException($"no parser for {TryParse}<{typeof(T).Name}>")
            };

            private static Parser<T, P> CreateParser(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>), typeof(P) });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (Parser<T, P>)Delegate.CreateDelegate(typeof(Parser<T, P>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }

            private static TryParser<T, P> CreateTryParser(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>), typeof(T).MakeByRefType(), typeof(P) });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (TryParser<T, P>)Delegate.CreateDelegate(typeof(TryParser<T, P>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }

            private static bool IsGeneric(Type type, Type genericDef) => type.IsGenericType && type.GetGenericTypeDefinition() == genericDef;
        }
    }
}
