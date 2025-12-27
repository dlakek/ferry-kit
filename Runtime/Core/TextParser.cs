using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static FerryKit.Core.StringHelper;

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
        QuoteEscapeMode EscapeMode { get; }
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

            var lineSplitter = LineSplitter<P>.From(text, policy);
            if (isSkipFirstLine)
            {
                lineSplitter.MoveNext();
            }
            var result = new List<T>(reserveLine > 0 ? reserveLine : text.Length / _estimateLengthPerLine);
            foreach (var line in lineSplitter)
            {
                if (line.IsWhiteSpace())
                    continue;

                var data = ExpressionCache<T>.New();
                if (data.Parse(LineReader<P>.From(line, policy)))
                {
                    result.Add(data);
                }
                else throw new InvalidOperationException($"line parse failed. line: {line.ToString()}");
            }
            return result;
        }
    }

    /// <summary>
    /// 전달받은 문자열을 한 줄씩 읽어나가는 구조체
    /// </summary>
    public ref struct LineSplitter<P> where P : struct, IParsePolicy
    {
        private readonly P _policy;

        private ReadOnlySpan<char> _remain;
        private ReadOnlySpan<char> _curLine;

        public readonly ReadOnlySpan<char> Current
        {
            [MethodImpl(Opt.Inline)]
            get => _curLine;
        }

        [MethodImpl(Opt.Inline)] public readonly LineSplitter<P> GetEnumerator() => this;
        [MethodImpl(Opt.Inline)] public static LineSplitter<P> From(ReadOnlySpan<char> text, P policy) => new(text, policy);

        [MethodImpl(Opt.Inline)]
        public LineSplitter(ReadOnlySpan<char> text, P policy)
        {
            _policy = policy;
            _remain = text;
            _curLine = default;
        }

        [MethodImpl(Opt.Inline)]
        public bool MoveNext()
        {
            if (_remain.Length == 0)
            {
                _curLine = ReadOnlySpan<char>.Empty;
                return false;
            }
            int idx = _remain.IndexOfUnquoted('\n', _policy.EscapeMode);
            if (idx == -1)
            {
                _curLine = _remain.TrimEndOne('\r');
                _remain = ReadOnlySpan<char>.Empty;
            }
            else
            {
                _curLine = _remain[..idx].TrimEndOne('\r');
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

        private ReadOnlySpan<char> _curRead;
        private int _curIdx;

        public readonly ReadOnlySpan<char> Line => _line;
        public readonly ReadOnlySpan<char> Current => _curRead;

        [MethodImpl(Opt.Inline)] public T Read<T>() => ReadNext().To<T, P>(_policy);
        [MethodImpl(Opt.Inline)] public bool TryRead<T>(out T result) => ReadNext().TryTo(out result, _policy);
        [MethodImpl(Opt.Inline)] public static LineReader<P> From(ReadOnlySpan<char> line, P policy) => new(line, policy);

        [MethodImpl(Opt.Inline)]
        public LineReader(ReadOnlySpan<char> line, P policy)
        {
            _line = line;
            _policy = policy;
            _curRead = default;
            _curIdx = 0;
        }

        public ReadOnlySpan<char> ReadNext()
        {
            if (_curIdx >= _line.Length)
                return _curRead = ReadOnlySpan<char>.Empty;

            var remaining = _line[_curIdx..];
            int nextComma = remaining.IndexOfUnquoted(_policy.ColSep, _policy.EscapeMode);
            if (nextComma == -1)
            {
                _curIdx = _line.Length;
                _curRead = remaining;
            }
            else
            {
                _curIdx += nextComma + 1;
                _curRead = remaining[..nextComma];
            }
            return _curRead;
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
        public const QuoteEscapeMode EscapeMode = QuoteEscapeMode.Csv;

        /// <summary>
        /// 런타임에 함수 인자로 전달될 때에 크기 0의 value type 인자로 취급. 오버헤드 최소화.
        /// </summary>
        public readonly struct Default : IParsePolicy
        {
            public char ColSep => ParseHelper.ColSep;
            public char ArrSep => ParseHelper.ArrSep;
            public char DictSep => ParseHelper.DictSep;
            public char PairSep => ParseHelper.PairSep;
            public QuoteEscapeMode EscapeMode => ParseHelper.EscapeMode;
        }

        /// <summary>
        /// 런타임에 함수 인자로 전달될 때에 필드 만큼의 크기를 가진 value type 인자로 취급
        /// 런타임에 설정을 커스텀하여 파싱 시작하려는 경우는 이 구조체를 사용하면 되고,
        /// 컴파일 타임에 결정 가능한 커스텀인 경우는 위의 Default 처럼 IParsePolicy 상속 구조체를 추가하여 사용하면 된다.
        /// </summary>
        public readonly struct Custom : IParsePolicy
        {
            public char ColSep { get; }
            public char ArrSep { get; }
            public char DictSep { get; }
            public char PairSep { get; }
            public QuoteEscapeMode EscapeMode { get; }

            public Custom(
                char colSep = ParseHelper.ColSep,
                char arrSep = ParseHelper.ArrSep,
                char dictSep = ParseHelper.DictSep,
                char pairSep = ParseHelper.PairSep,
                QuoteEscapeMode escapeMode = ParseHelper.EscapeMode)
            {
                ColSep = colSep;
                ArrSep = arrSep;
                DictSep = dictSep;
                PairSep = pairSep;
                EscapeMode = escapeMode;
            }
        }

        public delegate T Parser<T, P>(ReadOnlySpan<char> span, P policy) where P : struct, IParsePolicy;
        public delegate bool TryParser<T, P>(ReadOnlySpan<char> span, out T result, P policy) where P : struct, IParsePolicy;

        [MethodImpl(Opt.Inline)] public static T To<T>(this ReadOnlySpan<char> s) => Cache<T, Default>.Parse(s, default);
        [MethodImpl(Opt.Inline)] public static int ToInt(this ReadOnlySpan<char> s) => s.IsEmpty ? 0 : int.Parse(s);
        [MethodImpl(Opt.Inline)] public static long ToLong(this ReadOnlySpan<char> s) => s.IsEmpty ? 0 : long.Parse(s);
        [MethodImpl(Opt.Inline)] public static float ToFloat(this ReadOnlySpan<char> s) => s.IsEmpty ? 0 : float.Parse(s);
        [MethodImpl(Opt.Inline)] public static double ToDouble(this ReadOnlySpan<char> s) => s.IsEmpty ? 0 : double.Parse(s);
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime(this ReadOnlySpan<char> s) => s.IsEmpty ? default : DateTime.Parse(s);
        [MethodImpl(Opt.Inline)] public static string ToStr(this ReadOnlySpan<char> s) => s.TrimQuotes().ToString();
        [MethodImpl(Opt.Inline)] public static bool ToBool(this ReadOnlySpan<char> s) => s.TryParseForBit(out var r) ? r : bool.Parse(s);
        [MethodImpl(Opt.Inline)] public static T ToEnum<T>(this ReadOnlySpan<char> s) where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T>(this ReadOnlySpan<char> s) => s.ToArrayImpl(Cache<T, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T>(this ReadOnlySpan<char> s) => s.ToListImpl(Cache<T, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V>(this ReadOnlySpan<char> s) => s.ToPairImpl(Cache<K, Default>.Parse, Cache<V, Default>.Parse);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> s) => s.ToDictionaryImpl(Cache<K, Default>.Parse, Cache<V, Default>.Parse);

        [MethodImpl(Opt.Inline)] public static bool TryTo<T>(this ReadOnlySpan<char> s, out T r) => Cache<T, Default>.TryParse(s, out r, default);
        [MethodImpl(Opt.Inline)] public static bool TryToInt(this ReadOnlySpan<char> s, out int r) => int.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToLong(this ReadOnlySpan<char> s, out long r) => long.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat(this ReadOnlySpan<char> s, out float r) => float.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble(this ReadOnlySpan<char> s, out double r) => double.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime(this ReadOnlySpan<char> s, out DateTime r) => DateTime.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToStr(this ReadOnlySpan<char> s, out string r) { r = s.TrimQuotes().ToStr(); return true; }
        [MethodImpl(Opt.Inline)] public static bool TryToBool(this ReadOnlySpan<char> s, out bool r) => s.TryParseForBit(out r) || bool.TryParse(s, out r);
        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T>(this ReadOnlySpan<char> s, out T r) where T : struct, Enum => s.TryParse(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T>(this ReadOnlySpan<char> s, out T[] r) => s.TryToArrayImpl(out r, Cache<T, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T>(this ReadOnlySpan<char> s, out List<T> r) => s.TryToListImpl(out r, Cache<T, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V>(this ReadOnlySpan<char> s, out (K, V) r) => s.TryToPairImpl(out r, Cache<K, Default>.TryParse, Cache<V, Default>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V>(this ReadOnlySpan<char> s, out Dictionary<K, V> r) => s.TryToDictionaryImpl(out r, Cache<K, Default>.TryParse, Cache<V, Default>.TryParse);

        [MethodImpl(Opt.Inline)] public static T To<T, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => Cache<T, P>.Parse(s, p);
        [MethodImpl(Opt.Inline)] public static int ToInt<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToInt();
        [MethodImpl(Opt.Inline)] public static long ToLong<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToLong();
        [MethodImpl(Opt.Inline)] public static float ToFloat<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToFloat();
        [MethodImpl(Opt.Inline)] public static double ToDouble<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToDouble();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToDateTime();
        [MethodImpl(Opt.Inline)] public static string ToStr<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToStr();
        [MethodImpl(Opt.Inline)] public static bool ToBool<P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy => s.ToBool();
        [MethodImpl(Opt.Inline)] public static T ToEnum<T, P>(this ReadOnlySpan<char> s, P _) where P : struct, IParsePolicy where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToArrayImpl(Cache<T, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToListImpl(Cache<T, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToPairImpl(Cache<K, P>.Parse, Cache<V, P>.Parse, p);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V, P>(this ReadOnlySpan<char> s, P p) where P : struct, IParsePolicy => s.ToDictionaryImpl(Cache<K, P>.Parse, Cache<V, P>.Parse, p);

        [MethodImpl(Opt.Inline)] public static bool TryTo<T, P>(this ReadOnlySpan<char> s, out T r, P p) where P : struct, IParsePolicy => Cache<T, P>.TryParse(s, out r, p);
        [MethodImpl(Opt.Inline)] public static bool TryToInt<P>(this ReadOnlySpan<char> s, out int r, P _) where P : struct, IParsePolicy => s.TryToInt(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToLong<P>(this ReadOnlySpan<char> s, out long r, P _) where P : struct, IParsePolicy => s.TryToLong(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat<P>(this ReadOnlySpan<char> s, out float r, P _) where P : struct, IParsePolicy => s.TryToFloat(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble<P>(this ReadOnlySpan<char> s, out double r, P _) where P : struct, IParsePolicy => s.TryToDouble(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime<P>(this ReadOnlySpan<char> s, out DateTime r, P _) where P : struct, IParsePolicy => s.TryToDateTime(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToStr<P>(this ReadOnlySpan<char> s, out string r, P _) where P : struct, IParsePolicy => s.TryToStr(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToBool<P>(this ReadOnlySpan<char> s, out bool r, P _) where P : struct, IParsePolicy => s.TryToBool(out r);
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

            var esc = policy.EscapeMode;
            var sep = policy.ArrSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new T[num];
            int arrIdx = 0;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
                if (idx == -1)
                {
                    ret[arrIdx] = parser(span[start..], policy);
                    break;
                }
                ret[arrIdx++] = parser(span[start..idx], policy);
                start = idx + 1;
            }
            return ret;
        }

        private static bool TryToArrayImpl<T, P>(this ReadOnlySpan<char> span, out T[] result, TryParser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                result = Array.Empty<T>();
                return true;
            }
            result = null;
            var esc = policy.EscapeMode;
            var sep = policy.ArrSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new T[num];
            int arrIdx = 0;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
                if (idx == -1)
                {
                    if (!parser(span[start..], out ret[arrIdx], policy))
                        return false;

                    break;
                }
                if (!parser(span[start..idx], out ret[arrIdx++], policy))
                    return false;

                start = idx + 1;
            }
            result = ret;
            return true;
        }

        private static List<T> ToListImpl<T, P>(this ReadOnlySpan<char> span, Parser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return new();

            var esc = policy.EscapeMode;
            var sep = policy.ArrSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new List<T>(num);
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
                if (idx == -1)
                {
                    ret.Add(parser(span[start..], policy));
                    break;
                }
                ret.Add(parser(span[start..idx], policy));
                start = idx + 1;
            }
            return ret;
        }

        private static bool TryToListImpl<T, P>(this ReadOnlySpan<char> span, out List<T> result, TryParser<T, P> parser, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                result = new();
                return true;
            }
            result = null;
            var esc = policy.EscapeMode;
            var sep = policy.ArrSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new List<T>(num);
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
                if (idx == -1)
                {
                    if (!parser(span[start..], out var parsed, policy))
                        return false;

                    ret.Add(parsed);
                    break;
                }
                else
                {
                    if (!parser(span[start..idx], out var parsed, policy))
                        return false;

                    ret.Add(parsed);
                }
                start = idx + 1;
            }
            result = ret;
            return true;
        }

        private static Dictionary<K, V> ToDictionaryImpl<K, V, P>(this ReadOnlySpan<char> span, Parser<K, P> kp, Parser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return new();

            var esc = policy.EscapeMode;
            var sep = policy.DictSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new Dictionary<K, V>(num);
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                var (k, v) = sub.ToPairImpl(kp, vp, policy);
                ret.Add(k, v);
                if (idx == -1)
                    break;

                start = idx + 1;
            }
            return ret;
        }

        private static bool TryToDictionaryImpl<K, V, P>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, TryParser<K, P> kp, TryParser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            if (span.IsEmpty || span.IsWhiteSpace())
            {
                result = new();
                return true;
            }
            result = null;
            var esc = policy.EscapeMode;
            var sep = policy.DictSep;
            int len = span.Length;
            int num = span.CountByUnquotedSep(sep, esc);
            var ret = new Dictionary<K, V>(num);
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(sep, start, esc);
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

        [MethodImpl(Opt.Inline)]
        private static (K, V) ToPairImpl<K, V, P>(this ReadOnlySpan<char> span, Parser<K, P> kp, Parser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            int idx = span.IndexOfUnquoted(policy.PairSep, policy.EscapeMode);
            if (idx == -1)
                throw new FormatException($"invalid pair format. text: {span.ToString()}");

            return (kp(span[..idx], policy), vp(span[(idx + 1)..], policy));
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToPairImpl<K, V, P>(this ReadOnlySpan<char> span, out (K, V) result, TryParser<K, P> kp, TryParser<V, P> vp, P policy = default)
            where P : struct, IParsePolicy
        {
            int idx = span.IndexOfUnquoted(policy.PairSep, policy.EscapeMode);
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

        [MethodImpl(Opt.Inline)]
        private static bool TryParseForBit(this ReadOnlySpan<char> span, out bool result)
        {
            if (span.Length == 1)
            {
                if (span[0] == '1') { result = true; return true; }
                if (span[0] == '0') { result = false; return true; }
            }
            result = false;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static int CountByUnquotedSep(this ReadOnlySpan<char> span, char sep, QuoteEscapeMode esc)
        {
            int count = 1;
            int idx = 0;
            while ((idx = span.IndexOfUnquoted(sep, idx, esc)) != -1)
            {
                ++count;
                ++idx;
            }
            return count;
        }

        /// <summary>
        /// 각 특수화된 타입 별로 최초 1회씩만 리플렉션을 통한 캐싱
        /// </summary>
        private static class Cache<T, P> where P : struct, IParsePolicy
        {
            public static readonly Parser<T, P> Parse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (Parser<T, P>)(object)(Parser<int, P>)ToInt,
                _ when typeof(T) == typeof(long) => (Parser<T, P>)(object)(Parser<long, P>)ToLong,
                _ when typeof(T) == typeof(float) => (Parser<T, P>)(object)(Parser<float, P>)ToFloat,
                _ when typeof(T) == typeof(double) => (Parser<T, P>)(object)(Parser<double, P>)ToDouble,
                _ when typeof(T) == typeof(DateTime) => (Parser<T, P>)(object)(Parser<DateTime, P>)ToDateTime,
                _ when typeof(T) == typeof(string) => (Parser<T, P>)(object)(Parser<string, P>)ToStr,
                _ when typeof(T) == typeof(bool) => (Parser<T, P>)(object)(Parser<bool, P>)ToBool,
                _ when typeof(T).IsEnum => CreateParser(nameof(ToEnum), typeof(T), typeof(P)),
                _ when typeof(T).IsArray => CreateParser(nameof(ToArray), typeof(T).GetElementType(), typeof(P)),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateParser(nameof(ToList), typeof(T).GetGenericArguments()[0], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateParser(nameof(ToDictionary), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(ValueTuple<,>)) => CreateParser(nameof(ToPair), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ => throw new ArgumentException($"no parser for Parse<{typeof(T).Name}, {typeof(P).Name}>")
            };

            public static readonly TryParser<T, P> TryParse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (TryParser<T, P>)(object)(TryParser<int, P>)TryToInt,
                _ when typeof(T) == typeof(long) => (TryParser<T, P>)(object)(TryParser<long, P>)TryToLong,
                _ when typeof(T) == typeof(float) => (TryParser<T, P>)(object)(TryParser<float, P>)TryToFloat,
                _ when typeof(T) == typeof(double) => (TryParser<T, P>)(object)(TryParser<double, P>)TryToDouble,
                _ when typeof(T) == typeof(DateTime) => (TryParser<T, P>)(object)(TryParser<DateTime, P>)TryToDateTime,
                _ when typeof(T) == typeof(string) => (TryParser<T, P>)(object)(TryParser<string, P>)TryToStr,
                _ when typeof(T) == typeof(bool) => (TryParser<T, P>)(object)(TryParser<bool, P>)TryToBool,
                _ when typeof(T).IsEnum => CreateTryParser(nameof(TryToEnum), typeof(T), typeof(P)),
                _ when typeof(T).IsArray => CreateTryParser(nameof(TryToArray), typeof(T).GetElementType(), typeof(P)),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateTryParser(nameof(TryToList), typeof(T).GetGenericArguments()[0], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateTryParser(nameof(TryToDictionary), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ when IsGeneric(typeof(T), typeof(ValueTuple<,>)) => CreateTryParser(nameof(TryToPair), typeof(T).GetGenericArguments()[0], typeof(T).GetGenericArguments()[1], typeof(P)),
                _ => throw new ArgumentException($"no parser for TryParse<{typeof(T).Name}, {typeof(P).Name}>")
            };

            private static Parser<T, P> CreateParser(string methodName, params Type[] typeArgs)
            {
                var method = FindParserMethod(methodName, 2, false);
                return method == null
                    ? throw new InvalidOperationException($"failed to bind wrapper method: {methodName}<{string.Join(", ", typeArgs.Select(t => t.Name))}>")
                    : (Parser<T, P>)Delegate.CreateDelegate(typeof(Parser<T, P>), method.MakeGenericMethod(typeArgs));
            }

            private static TryParser<T, P> CreateTryParser(string methodName, params Type[] typeArgs)
            {
                var method = FindParserMethod(methodName, 3, true);
                return method == null
                    ? throw new InvalidOperationException($"failed to bind wrapper method: {methodName}<{string.Join(", ", typeArgs.Select(t => t.Name))}>")
                    : (TryParser<T, P>)Delegate.CreateDelegate(typeof(TryParser<T, P>), method.MakeGenericMethod(typeArgs));
            }

            private static MethodInfo FindParserMethod(string methodName, int paramCount, bool hasOutParam)
            {
                foreach (var m in Methods)
                {
                    if (m.Name != methodName)
                        continue;

                    if (!m.IsGenericMethodDefinition)
                        continue;

                    var ps = m.GetParameters();
                    if (ps.Length != paramCount)
                        continue;

                    if (ps[0].ParameterType != typeof(ReadOnlySpan<char>))
                        continue;

                    if (hasOutParam && !ps[1].IsOut)
                        continue;

                    return m;
                }
                return null;
            }

            private static bool IsGeneric(Type type, Type genericDef) => type.IsGenericType && type.GetGenericTypeDefinition() == genericDef;
        }

        private static readonly MethodInfo[] Methods = typeof(ParseHelper).GetMethods(BindingFlags.Public | BindingFlags.Static);
    }
}
