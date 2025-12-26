using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    /// <summary>
    /// TextParser를 통해 파싱 가능한 객체 인터페이스
    /// </summary>
    public interface IParsable
    {
        bool Parse(LineReader reader);
    }

    /// <summary>
    /// 기본적으로 CSV 파싱을 위해 사용되지만, 다른 형태의 텍스트도 파싱할 수 있도록 구현된 범용 파서
    /// </summary>
    public static class TextParser
    {
        private const int _estimateLengthPerLine = 50;

        public static List<T> Load<T>(string text, int reserveLine = 0, bool isSkipFirstLine = false, ParseConfig? config = null) where T : IParsable, new()
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

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
                if (data.Parse(LineReader.From(row, config)))
                {
                    result.Add(data);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// 기본적으로 CSV 파싱에 맞춘 정보를 담지만, 범용적으로 쓸 수 있게 커스텀 가능한 Config
    /// 현재 8바이트에 맞춰져 있어 in 키워드 없이 전달하는게 더 낫지만 필드가 더 늘어나면 관련 전달 코드들에 in 키워드 추가 필요
    /// </summary>
    public readonly struct ParseConfig
    {
        public readonly char ColSep;
        public readonly char ArrSep;
        public readonly char DictSep;
        public readonly char PairSep;

        public ParseConfig(char columnSep, char arraySep = ParseHelper.ArrSep, char dictSep = ParseHelper.DicSep, char pairSep = ParseHelper.PairSep)
        {
            ColSep = columnSep;
            ArrSep = arraySep;
            DictSep = dictSep;
            PairSep = pairSep;
        }

        public static readonly ParseConfig Default = new(ParseHelper.ColSep);
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

        [MethodImpl(Opt.Inline)]
        public readonly LineSplitter GetEnumerator() => this;

        [MethodImpl(Opt.Inline)]
        public static LineSplitter From(ReadOnlySpan<char> text) => new(text);

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
    public ref struct LineReader
    {
        private readonly ReadOnlySpan<char> _line;
        private readonly ParseConfig? _config;
        private int _curIdx;

        [MethodImpl(Opt.Inline)]
        public T Read<T>() => _config.HasValue
            ? ReadNext().To<T>(_config.Value)
            : ReadNext().To<T>();

        [MethodImpl(Opt.Inline)]
        public bool TryRead<T>(out T result) => _config.HasValue
            ? ReadNext().TryTo(out result, _config.Value)
            : ReadNext().TryTo(out result);

        [MethodImpl(Opt.Inline)]
        public static LineReader From(ReadOnlySpan<char> row, ParseConfig? config = null) => new(row, config);

        [MethodImpl(Opt.Inline)]
        public LineReader(ReadOnlySpan<char> line, ParseConfig? config = null)
        {
            _line = line;
            _config = config;
            _curIdx = 0;
        }

        public ReadOnlySpan<char> ReadNext()
        {
            if (_curIdx >= _line.Length)
                return ReadOnlySpan<char>.Empty;

            var remaining = _line[_curIdx..];
            int nextComma = remaining.IndexOfUnquoted(_config.HasValue ? _config.Value.ColSep : ParseHelper.ColSep);
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
        public const char DicSep = '|';
        public const char PairSep = '=';

        public delegate T Parser<T>(ReadOnlySpan<char> span);
        public delegate T ParserWC<T>(ReadOnlySpan<char> span, ParseConfig config);
        public delegate bool TryParser<T>(ReadOnlySpan<char> span, out T result);
        public delegate bool TryParserWC<T>(ReadOnlySpan<char> span, out T result, ParseConfig config);

        [MethodImpl(Opt.Inline)] public static T To<T>(this ReadOnlySpan<char> span) => Cache<T>.Parse(span);
        [MethodImpl(Opt.Inline)] public static T To<T>(this ReadOnlySpan<char> span, ParseConfig config) => Cache<T>.ParseWC(span, config);
        [MethodImpl(Opt.Inline)] public static bool TryTo<T>(this ReadOnlySpan<char> span, out T result) => Cache<T>.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryTo<T>(this ReadOnlySpan<char> span, out T result, ParseConfig config) => Cache<T>.TryParseWC(span, out result, config);

        [MethodImpl(Opt.Inline)] public static int ToInt(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : int.Parse(span);
        [MethodImpl(Opt.Inline)] public static long ToLong(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : long.Parse(span);
        [MethodImpl(Opt.Inline)] public static float ToFloat(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : float.Parse(span);
        [MethodImpl(Opt.Inline)] public static double ToDouble(this ReadOnlySpan<char> span) => span.IsEmpty ? 0 : double.Parse(span);
        [MethodImpl(Opt.Inline)] public static bool ToBool(this ReadOnlySpan<char> span) => span.Length == 1 && span[0] == '1' || bool.Parse(span);
        [MethodImpl(Opt.Inline)] public static string ToStr(this ReadOnlySpan<char> span) => span.ToString();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime(this ReadOnlySpan<char> span) => span.IsEmpty ? default : DateTime.Parse(span);

        [MethodImpl(Opt.Inline)] public static int ToInt(this ReadOnlySpan<char> span, ParseConfig _) => span.ToInt();
        [MethodImpl(Opt.Inline)] public static long ToLong(this ReadOnlySpan<char> span, ParseConfig _) => span.ToLong();
        [MethodImpl(Opt.Inline)] public static float ToFloat(this ReadOnlySpan<char> span, ParseConfig _) => span.ToFloat();
        [MethodImpl(Opt.Inline)] public static double ToDouble(this ReadOnlySpan<char> span, ParseConfig _) => span.ToDouble();
        [MethodImpl(Opt.Inline)] public static bool ToBool(this ReadOnlySpan<char> span, ParseConfig _) => span.ToBool();
        [MethodImpl(Opt.Inline)] public static string ToStr(this ReadOnlySpan<char> span, ParseConfig _) => span.ToStr();
        [MethodImpl(Opt.Inline)] public static DateTime ToDateTime(this ReadOnlySpan<char> span, ParseConfig _) => span.ToDateTime();

        [MethodImpl(Opt.Inline)] public static bool TryToInt(this ReadOnlySpan<char> span, out int result) => int.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToLong(this ReadOnlySpan<char> span, out long result) => long.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat(this ReadOnlySpan<char> span, out float result) => float.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble(this ReadOnlySpan<char> span, out double result) => double.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToBool(this ReadOnlySpan<char> span, out bool result) => bool.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToStr(this ReadOnlySpan<char> span, out string result) => !string.IsNullOrEmpty(result = span.ToStr());
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime(this ReadOnlySpan<char> span, out DateTime result) => DateTime.TryParse(span, out result);

        [MethodImpl(Opt.Inline)] public static bool TryToInt(this ReadOnlySpan<char> span, out int result, ParseConfig _) => int.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToLong(this ReadOnlySpan<char> span, out long result, ParseConfig _) => long.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToFloat(this ReadOnlySpan<char> span, out float result, ParseConfig _) => float.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToDouble(this ReadOnlySpan<char> span, out double result, ParseConfig _) => double.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToBool(this ReadOnlySpan<char> span, out bool result, ParseConfig _) => bool.TryParse(span, out result);
        [MethodImpl(Opt.Inline)] public static bool TryToStr(this ReadOnlySpan<char> span, out string result, ParseConfig _) => !string.IsNullOrEmpty(result = span.ToStr());
        [MethodImpl(Opt.Inline)] public static bool TryToDateTime(this ReadOnlySpan<char> span, out DateTime result, ParseConfig _) => DateTime.TryParse(span, out result);

        [MethodImpl(Opt.Inline)] public static T ToEnum<T>(this ReadOnlySpan<char> s) where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T>(this ReadOnlySpan<char> s) => s.ToArray(Cache<T>.Parse);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T>(this ReadOnlySpan<char> s) => s.ToList(Cache<T>.Parse);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V>(this ReadOnlySpan<char> s) => s.ToPair(Cache<K>.Parse, Cache<V>.Parse);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> s) => s.ToDictionary(Cache<K>.Parse, Cache<V>.Parse);

        [MethodImpl(Opt.Inline)] public static T ToEnum<T>(this ReadOnlySpan<char> s, ParseConfig _) where T : struct, Enum => s.Parse<T>();
        [MethodImpl(Opt.Inline)] public static T[] ToArray<T>(this ReadOnlySpan<char> s, ParseConfig c) => s.ToArray(Cache<T>.ParseWC, c);
        [MethodImpl(Opt.Inline)] public static List<T> ToList<T>(this ReadOnlySpan<char> s, ParseConfig c) => s.ToList(Cache<T>.ParseWC, c);
        [MethodImpl(Opt.Inline)] public static (K, V) ToPair<K, V>(this ReadOnlySpan<char> s, ParseConfig c) => s.ToPair(Cache<K>.ParseWC, Cache<V>.ParseWC, c);
        [MethodImpl(Opt.Inline)] public static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> s, ParseConfig c) => s.ToDictionary(Cache<K>.ParseWC, Cache<V>.ParseWC, c);

        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T>(this ReadOnlySpan<char> s, out T r) where T : struct, Enum => s.TryParse(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T>(this ReadOnlySpan<char> s, out T[] r) => s.TryToArray(out r, Cache<T>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T>(this ReadOnlySpan<char> s, out List<T> r) => s.TryToList(out r, Cache<T>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V>(this ReadOnlySpan<char> s, out (K, V) r) => s.TryToPair(out r, Cache<K>.TryParse, Cache<V>.TryParse);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V>(this ReadOnlySpan<char> s, out Dictionary<K, V> r) => s.TryToDictionary(out r, Cache<K>.TryParse, Cache<V>.TryParse);

        [MethodImpl(Opt.Inline)] public static bool TryToEnum<T>(this ReadOnlySpan<char> s, out T r, ParseConfig _) where T : struct, Enum => s.TryParse(out r);
        [MethodImpl(Opt.Inline)] public static bool TryToArray<T>(this ReadOnlySpan<char> s, out T[] r, ParseConfig c) => s.TryToArray(out r, Cache<T>.TryParseWC, c);
        [MethodImpl(Opt.Inline)] public static bool TryToList<T>(this ReadOnlySpan<char> s, out List<T> r, ParseConfig c) => s.TryToList(out r, Cache<T>.TryParseWC, c);
        [MethodImpl(Opt.Inline)] public static bool TryToPair<K, V>(this ReadOnlySpan<char> s, out (K, V) r, ParseConfig c) => s.TryToPair(out r, Cache<K>.TryParseWC, Cache<V>.TryParseWC, c);
        [MethodImpl(Opt.Inline)] public static bool TryToDictionary<K, V>(this ReadOnlySpan<char> s, out Dictionary<K, V> r, ParseConfig c) => s.TryToDictionary(out r, Cache<K>.TryParseWC, Cache<V>.TryParseWC, c);

        private static T[] ToArray<T>(this ReadOnlySpan<char> span, Parser<T> parser)
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return Array.Empty<T>();

            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(ArrSep, start);
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
                int idx = span.IndexOfUnquoted(ArrSep, start);
                if (idx == -1)
                {
                    result[arrIdx] = parser(span[start..]);
                    break;
                }
                result[arrIdx++] = parser(span[start..idx]);
                start = idx + 1;
            }
            return result;
        }

        private static T[] ToArray<T>(this ReadOnlySpan<char> span, ParserWC<T> parser, ParseConfig config)
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return Array.Empty<T>();

            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(config.ArrSep, start);
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
                int idx = span.IndexOfUnquoted(config.ArrSep, start);
                if (idx == -1)
                {
                    result[arrIdx] = parser(span[start..], config);
                    break;
                }
                result[arrIdx++] = parser(span[start..idx], config);
                start = idx + 1;
            }
            return result;
        }

        private static bool TryToArray<T>(this ReadOnlySpan<char> span, out T[] result, TryParser<T> parser)
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(ArrSep, start);
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
                int idx = span.IndexOfUnquoted(ArrSep, start);
                if (idx == -1)
                {
                    if (!parser(span[start..], out ret[arrIdx]))
                        return false;

                    break;
                }
                if (!parser(span[start..], out ret[arrIdx++]))
                    return false;

                start = idx + 1;
            }
            result = ret;
            return true;
        }

        private static bool TryToArray<T>(this ReadOnlySpan<char> span, out T[] result, TryParserWC<T> parser, ParseConfig config)
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            int count = 1;
            int start = 0;
            while (start < span.Length)
            {
                int idx = span.IndexOfUnquoted(config.ArrSep, start);
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
                int idx = span.IndexOfUnquoted(config.ArrSep, start);
                if (idx == -1)
                {
                    if (!parser(span[start..], out ret[arrIdx], config))
                        return false;

                    break;
                }
                if (!parser(span[start..], out ret[arrIdx++], config))
                    return false;

                start = idx + 1;
            }
            result = ret;
            return true;
        }

        [MethodImpl(Opt.Inline)]
        private static List<T> ToList<T>(this ReadOnlySpan<char> span, Parser<T> parser)
        {
            return new(span.ToArray(parser));
        }

        [MethodImpl(Opt.Inline)]
        private static List<T> ToList<T>(this ReadOnlySpan<char> span, ParserWC<T> parser, ParseConfig config)
        {
            return new(span.ToArray(parser, config));
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToList<T>(this ReadOnlySpan<char> span, out List<T> result, TryParser<T> parser)
        {
            if (span.TryToArray(out var arrayResult, parser))
            {
                result = new(arrayResult);
                return true;
            }
            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToList<T>(this ReadOnlySpan<char> span, out List<T> result, TryParserWC<T> parser, ParseConfig config)
        {
            if (span.TryToArray(out var arrayResult, parser, config))
            {
                result = new(arrayResult);
                return true;
            }
            result = null;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static (K, V) ToPair<K, V>(this ReadOnlySpan<char> span, Parser<K> kp, Parser<V> vp)
        {
            int idx = span.IndexOfUnquoted(PairSep);
            if (idx == -1)
                return default;

            return (kp(span[..idx]), vp(span[(idx + 1)..]));
        }

        [MethodImpl(Opt.Inline)]
        private static (K, V) ToPair<K, V>(this ReadOnlySpan<char> span, ParserWC<K> kp, ParserWC<V> vp, ParseConfig config)
        {
            int idx = span.IndexOfUnquoted(config.PairSep);
            if (idx == -1)
                return default;

            return (kp(span[..idx], config), vp(span[(idx + 1)..], config));
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToPair<K, V>(this ReadOnlySpan<char> span, out (K, V) result, TryParser<K> kp, TryParser<V> vp)
        {
            int idx = span.IndexOfUnquoted(PairSep);
            if (idx != -1)
            {
                if (kp(span[..idx], out K kResult) && vp(span[(idx + 1)..], out V vResult))
                {
                    result = (kResult, vResult);
                    return true;
                }
            }
            result = default;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryToPair<K, V>(this ReadOnlySpan<char> span, out (K, V) result, TryParserWC<K> kp, TryParserWC<V> vp, ParseConfig config)
        {
            int idx = span.IndexOfUnquoted(config.PairSep);
            if (idx != -1)
            {
                if (kp(span[..idx], out K kResult, config) && vp(span[(idx + 1)..], out V vResult, config))
                {
                    result = (kResult, vResult);
                    return true;
                }
            }
            result = default;
            return false;
        }

        private static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> span, Parser<K> kp, Parser<V> vp)
        {
            var result = new Dictionary<K, V>();
            if (span.IsEmpty || span.IsWhiteSpace())
                return result;

            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(DicSep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                var (k, v) = sub.ToPair(kp, vp);
                result.Add(k, v);
                if (idx == -1)
                    break;

                start = idx + 1;
            }
            return result;
        }

        private static Dictionary<K, V> ToDictionary<K, V>(this ReadOnlySpan<char> span, ParserWC<K> kp, ParserWC<V> vp, ParseConfig config)
        {
            var result = new Dictionary<K, V>();
            if (span.IsEmpty || span.IsWhiteSpace())
                return result;

            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(config.DictSep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                var (k, v) = sub.ToPair(kp, vp, config);
                result.Add(k, v);
                if (idx == -1)
                    break;

                start = idx + 1;
            }
            return result;
        }

        private static bool TryToDictionary<K, V>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, TryParser<K> kp, TryParser<V> vp)
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            var ret = new Dictionary<K, V>();
            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(DicSep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                if (!sub.TryToPair(out var pair, kp, vp))
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

        private static bool TryToDictionary<K, V>(this ReadOnlySpan<char> span, out Dictionary<K, V> result, TryParserWC<K> kp, TryParserWC<V> vp, ParseConfig config)
        {
            result = null;
            if (span.IsEmpty || span.IsWhiteSpace())
                return false;

            var ret = new Dictionary<K, V>();
            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int idx = span.IndexOfUnquoted(config.DictSep, start);
                var sub = idx == -1 ? span[start..] : span[start..idx];
                if (!sub.TryToPair(out var pair, kp, vp, config))
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

        private static class Cache<T>
        {
            public static readonly Parser<T> Parse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (Parser<T>)(object)(Parser<int>)ToInt,
                _ when typeof(T) == typeof(long) => (Parser<T>)(object)(Parser<long>)ToLong,
                _ when typeof(T) == typeof(float) => (Parser<T>)(object)(Parser<float>)ToFloat,
                _ when typeof(T) == typeof(double) => (Parser<T>)(object)(Parser<double>)ToDouble,
                _ when typeof(T) == typeof(bool) => (Parser<T>)(object)(Parser<bool>)ToBool,
                _ when typeof(T) == typeof(string) => (Parser<T>)(object)(Parser<string>)ToStr,
                _ when typeof(T) == typeof(DateTime) => (Parser<T>)(object)(Parser<DateTime>)ToDateTime,
                _ when typeof(T).IsEnum => CreateParser(nameof(ToEnum), typeof(T)),
                _ when typeof(T).IsArray => CreateParser(nameof(ToArray), typeof(T).GetElementType()),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateParser(nameof(ToList), typeof(T).GetGenericArguments()),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateParser(nameof(ToDictionary), typeof(T).GetGenericArguments()),
                _ => throw new ArgumentException($"no parser for {Parse}<{typeof(T).Name}>")
            };

            public static readonly ParserWC<T> ParseWC = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (ParserWC<T>)(object)(ParserWC<int>)ToInt,
                _ when typeof(T) == typeof(long) => (ParserWC<T>)(object)(ParserWC<long>)ToLong,
                _ when typeof(T) == typeof(float) => (ParserWC<T>)(object)(ParserWC<float>)ToFloat,
                _ when typeof(T) == typeof(double) => (ParserWC<T>)(object)(ParserWC<double>)ToDouble,
                _ when typeof(T) == typeof(bool) => (ParserWC<T>)(object)(ParserWC<bool>)ToBool,
                _ when typeof(T) == typeof(string) => (ParserWC<T>)(object)(ParserWC<string>)ToStr,
                _ when typeof(T) == typeof(DateTime) => (ParserWC<T>)(object)(ParserWC<DateTime>)ToDateTime,
                _ when typeof(T).IsEnum => CreateParserWC(nameof(ToEnum), typeof(T)),
                _ when typeof(T).IsArray => CreateParserWC(nameof(ToArray), typeof(T).GetElementType()),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateParserWC(nameof(ToList), typeof(T).GetGenericArguments()),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateParserWC(nameof(ToDictionary), typeof(T).GetGenericArguments()),
                _ => throw new ArgumentException($"no parser for {ParseWC}<{typeof(T).Name}>")
            };

            public static readonly TryParser<T> TryParse = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (TryParser<T>)(object)(TryParser<int>)TryToInt,
                _ when typeof(T) == typeof(long) => (TryParser<T>)(object)(TryParser<long>)TryToLong,
                _ when typeof(T) == typeof(float) => (TryParser<T>)(object)(TryParser<float>)TryToFloat,
                _ when typeof(T) == typeof(double) => (TryParser<T>)(object)(TryParser<double>)TryToDouble,
                _ when typeof(T) == typeof(bool) => (TryParser<T>)(object)(TryParser<bool>)TryToBool,
                _ when typeof(T) == typeof(string) => (TryParser<T>)(object)(TryParser<string>)TryToStr,
                _ when typeof(T) == typeof(DateTime) => (TryParser<T>)(object)(TryParser<DateTime>)TryToDateTime,
                _ when typeof(T).IsEnum => CreateTryParser(nameof(ToEnum), typeof(T)),
                _ when typeof(T).IsArray => CreateTryParser(nameof(ToArray), typeof(T).GetElementType()),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateTryParser(nameof(ToList), typeof(T).GetGenericArguments()),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateTryParser(nameof(ToDictionary), typeof(T).GetGenericArguments()),
                _ => throw new ArgumentException($"no parser for {TryParse}<{typeof(T).Name}>")
            };

            public static readonly TryParserWC<T> TryParseWC = typeof(T) switch
            {
                _ when typeof(T) == typeof(int) => (TryParserWC<T>)(object)(TryParserWC<int>)TryToInt,
                _ when typeof(T) == typeof(long) => (TryParserWC<T>)(object)(TryParserWC<long>)TryToLong,
                _ when typeof(T) == typeof(float) => (TryParserWC<T>)(object)(TryParserWC<float>)TryToFloat,
                _ when typeof(T) == typeof(double) => (TryParserWC<T>)(object)(TryParserWC<double>)TryToDouble,
                _ when typeof(T) == typeof(bool) => (TryParserWC<T>)(object)(TryParserWC<bool>)TryToBool,
                _ when typeof(T) == typeof(string) => (TryParserWC<T>)(object)(TryParserWC<string>)TryToStr,
                _ when typeof(T) == typeof(DateTime) => (TryParserWC<T>)(object)(TryParserWC<DateTime>)TryToDateTime,
                _ when typeof(T).IsEnum => CreateTryParserWC(nameof(ToEnum), typeof(T)),
                _ when typeof(T).IsArray => CreateTryParserWC(nameof(ToArray), typeof(T).GetElementType()),
                _ when IsGeneric(typeof(T), typeof(List<>)) => CreateTryParserWC(nameof(ToList), typeof(T).GetGenericArguments()),
                _ when IsGeneric(typeof(T), typeof(Dictionary<,>)) => CreateTryParserWC(nameof(ToDictionary), typeof(T).GetGenericArguments()),
                _ => throw new ArgumentException($"no parser for {TryParseWC}<{typeof(T).Name}>")
            };

            private static bool IsGeneric(Type type, Type genericDef)
                => type.IsGenericType && type.GetGenericTypeDefinition() == genericDef;

            private static Parser<T> CreateParser(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>) });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (Parser<T>)Delegate.CreateDelegate(typeof(Parser<T>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }

            private static ParserWC<T> CreateParserWC(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>), typeof(ParseConfig) });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (ParserWC<T>)Delegate.CreateDelegate(typeof(ParserWC<T>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }

            private static TryParser<T> CreateTryParser(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>), typeof(T).MakeByRefType() });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (TryParser<T>)Delegate.CreateDelegate(typeof(TryParser<T>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }

            private static TryParserWC<T> CreateTryParserWC(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName, new[] { typeof(ReadOnlySpan<char>), typeof(T).MakeByRefType(), typeof(ParseConfig) });
                if (method != null && method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(typeArgs);
                    return (TryParserWC<T>)Delegate.CreateDelegate(typeof(TryParserWC<T>), method);
                }
                throw new InvalidOperationException($"failed to bind wrapper method: {methodName}");
            }
        }
    }
}
