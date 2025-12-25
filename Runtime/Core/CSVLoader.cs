using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public delegate T SpanParser<T>(ReadOnlySpan<char> span);

    public interface IDataFromCSV
    {
        bool Parse(ref RowReader reader);
    }

    public static class CSVLoader
    {
        public static List<T> Load<T>(string csvText, bool isSkipHeader = true) where T : IDataFromCSV, new()
        {
            if (string.IsNullOrEmpty(csvText))
                return null;

            var rowSplitter = RowSplitter.From(csvText);
            if (isSkipHeader)
            {
                rowSplitter.MoveNext();
            }
            var result = new List<T>(csvText.Length / 50);
            foreach (var row in rowSplitter)
            {
                if (row.IsWhiteSpace())
                    continue;

                var reader = RowReader.From(row);
                var data = ExpressionCache<T>.Creator();
                if (data.Parse(ref reader))
                {
                    result.Add(data);
                }
            }
            return result;
        }
    }

    public ref struct RowSplitter
    {
        private ReadOnlySpan<char> _remain;
        private ReadOnlySpan<char> _curRow;

        public readonly ReadOnlySpan<char> Current
        {
            [MethodImpl(Opt.Inline)]
            get => _curRow;
        }

        [MethodImpl(Opt.Inline)]
        public readonly RowSplitter GetEnumerator() => this;

        [MethodImpl(Opt.Inline)]
        public static RowSplitter From(ReadOnlySpan<char> text) => new(text);

        [MethodImpl(Opt.Inline)]
        public RowSplitter(ReadOnlySpan<char> text) : this() => _remain = text;

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

    public ref struct RowReader
    {
        private readonly ReadOnlySpan<char> _row;

        private int _curIdx;

        [MethodImpl(Opt.Inline)]
        public static RowReader From(ReadOnlySpan<char> row) => new(row);

        [MethodImpl(Opt.Inline)]
        public RowReader(ReadOnlySpan<char> row) : this() => _row = row;

        // 이 함수는 인라인 강제하지 않는 편이 좋을듯함. (호출하는 곳이 많을 것이므로)
        public ReadOnlySpan<char> ReadNext()
        {
            if (_curIdx >= _row.Length)
                return ReadOnlySpan<char>.Empty;

            var remaining = _row[_curIdx..];
            int nextComma = remaining.IndexOfUnquoted(',');
            if (nextComma == -1)
            {
                _curIdx = _row.Length;
                return remaining.TrimQuotes();
            }
            else
            {
                _curIdx += nextComma + 1;
                return remaining[..nextComma].TrimQuotes();
            }
        }
    }

    /// <summary>
    /// Span 기반 파싱을 돕는 정적 헬퍼 클래스입니다.
    /// 자동 타입 추론을 위한 Parse<T> 메서드가 핵심입니다.
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        /// 주어진 Span을 타입 T로 파싱합니다.
        /// 타입에 따라 적절한 파서가 자동으로 선택됩니다 (박싱 없음).
        /// </summary>
        public static T Parse<T>(ReadOnlySpan<char> span) => Cache<T>.Parse(span);

        // ---------------------------------------------------------
        // 개별 타입 파서 구현 (CSVParser<T>에서 연결해서 사용)
        // ---------------------------------------------------------

        public static int ToInt(ReadOnlySpan<char> span) => span.IsEmpty ? 0 : int.Parse(span);
        public static long ToLong(ReadOnlySpan<char> span) => span.IsEmpty ? 0 : long.Parse(span);
        public static float ToFloat(ReadOnlySpan<char> span) => span.IsEmpty ? 0f : float.Parse(span);
        public static double ToDouble(ReadOnlySpan<char> span) => span.IsEmpty ? 0 : double.Parse(span);
        public static bool ToBool(ReadOnlySpan<char> span) => span.Length == 1 && span[0] == '1' || bool.Parse(span);
        public static string ToStr(ReadOnlySpan<char> span) => span.ToString();
        public static DateTime ToDateTime(ReadOnlySpan<char> span) => span.IsEmpty ? default : DateTime.Parse(span);

        // ---------------------------------------------------------
        // 제네릭 래퍼 메서드 (리플렉션 바인딩용)
        // ---------------------------------------------------------

        public static T EnumWrapper<T>(ReadOnlySpan<char> span) where T : struct, Enum => span.IsEmpty ? default : EnumHelper.Parse<T>(span, true);
        public static T[] ArrayWrapper<T>(ReadOnlySpan<char> span) => ToArray(span, Cache<T>.Parse);
        public static List<T> ListWrapper<T>(ReadOnlySpan<char> span) => ToList(span, Cache<T>.Parse);
        public static Dictionary<K, V> DictWrapper<K, V>(ReadOnlySpan<char> span) => ToDictionary(span, Cache<K>.Parse, Cache<V>.Parse);

        // ---------------------------------------------------------
        // 실제 컬렉션 파싱 로직
        // ---------------------------------------------------------

        public static T[] ToArray<T>(ReadOnlySpan<char> span, SpanParser<T> parser, char separator = ';')
        {
            if (span.IsEmpty || span.IsWhiteSpace())
                return Array.Empty<T>();

            int len = span.Length;
            int count = 1;
            for (int i = 0; i < len; ++i)
            {
                if (span[i] == separator)
                    ++count;
            }
            var result = new T[count];
            int idx = 0, start = 0;
            for (int i = 0; i < len; ++i)
            {
                if (span[i] == separator)
                {
                    result[idx++] = parser(span[start..i].TrimQuotes());
                    start = i + 1;
                }
            }
            result[idx] = parser(span[start..].TrimQuotes());
            return result;
        }

        public static List<T> ToList<T>(ReadOnlySpan<char> span, SpanParser<T> parser, char separator = ';')
        {
            T[] arr = ToArray(span, parser, separator);
            return new List<T>(arr);
        }

        public static Dictionary<K, V> ToDictionary<K, V>(
            ReadOnlySpan<char> span,
            SpanParser<K> kParser,
            SpanParser<V> vParser,
            char itemSeparator = ';',
            char kvSeparator = '=')
        {
            var dict = new Dictionary<K, V>();
            if (span.IsEmpty || span.IsWhiteSpace())
                return dict;

            int len = span.Length;
            int start = 0;
            while (start < len)
            {
                int sepIdx = span.IndexOfUnquoted(itemSeparator, start);
                var pair = sepIdx == -1 ? span[start..] : span[start..sepIdx];
                ParseKvPair(pair.TrimQuotes(), dict, kParser, vParser, kvSeparator);
                if (sepIdx == -1)
                    break;

                start = sepIdx + 1;
            }
            return dict;
        }

        [MethodImpl(Opt.Inline)]
        private static void ParseKvPair<K, V>(ReadOnlySpan<char> pairSpan, Dictionary<K, V> dict, SpanParser<K> kParser, SpanParser<V> vParser, char kvSep)
        {
            int eqIndex = pairSpan.IndexOfUnquoted(kvSep);
            if (eqIndex != -1)
            {
                K key = kParser(pairSpan[..eqIndex].TrimQuotes());
                V val = vParser(pairSpan[(eqIndex + 1)..].TrimQuotes());
                dict[key] = val;
            }
        }

        /// <summary>
        /// 내부적으로 T 타입에 대한 파서를 캐싱하는 클래스입니다.
        /// 정적 생성자에서 단 한 번만 리플렉션으로 파서를 연결합니다.
        /// </summary>
        private static class Cache<T>
        {
            public static readonly SpanParser<T> Parse;

            static Cache() => Parse = CreateParser();

            private static SpanParser<T> CreateParser()
            {
                var type = typeof(T);
                if (type == typeof(int)) return (SpanParser<T>)(object)(SpanParser<int>)ToInt;
                if (type == typeof(long)) return (SpanParser<T>)(object)(SpanParser<long>)ToLong;
                if (type == typeof(float)) return (SpanParser<T>)(object)(SpanParser<float>)ToFloat;
                if (type == typeof(double)) return (SpanParser<T>)(object)(SpanParser<double>)ToDouble;
                if (type == typeof(bool)) return (SpanParser<T>)(object)(SpanParser<bool>)ToBool;
                if (type == typeof(string)) return (SpanParser<T>)(object)(SpanParser<string>)ToStr;
                if (type == typeof(DateTime)) return (SpanParser<T>)(object)(SpanParser<DateTime>)ToDateTime;
                if (type.IsEnum)
                    return CreateWrapper(nameof(EnumWrapper), type);

                if (type.IsArray)
                    return CreateWrapper(nameof(ArrayWrapper), type.GetElementType());

                if (IsGenericType(type, typeof(List<>)))
                    return CreateWrapper(nameof(ListWrapper), type.GetGenericArguments()[0]);

                if (IsGenericType(type, typeof(Dictionary<,>)))
                    return CreateWrapper(nameof(DictWrapper), type.GetGenericArguments());

                throw new ArgumentException($"[CSV] No parser for {type.Name}");
            }

            [MethodImpl(Opt.Inline)]
            private static bool IsGenericType(Type type, Type genericDef)
                => type.IsGenericType && type.GetGenericTypeDefinition() == genericDef;

            [MethodImpl(Opt.Inline)]
            private static SpanParser<T> CreateWrapper(string methodName, params Type[] typeArgs)
            {
                var method = typeof(ParseHelper).GetMethod(methodName).MakeGenericMethod(typeArgs);
                return (SpanParser<T>)Delegate.CreateDelegate(typeof(SpanParser<T>), method);
            }
        }
    }
}
