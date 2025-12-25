using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class EnumHelper
    {
        [MethodImpl(Opt.Inline)]
        public static ReadOnlySpan<T> GetValues<T>() where T : struct, Enum => new(Cache<T>.Values);

        [MethodImpl(Opt.Inline)]
        public static int Count<T>() where T : struct, Enum => Cache<T>.Count;

        [MethodImpl(Opt.Inline)]
        public static bool IsDefined<T>(T value) where T : struct, Enum => Cache<T>.ValueSet.Contains(value);

        [MethodImpl(Opt.Inline)]
        public static bool TrueForAll<T>(Predicate<T> match) where T : struct, Enum
        {
            var arr = Cache<T>.Values;
            int len = arr.Length;
            for (int i = 0; i < len; ++i)
            {
                if (!match(arr[i]))
                    return false;
            }
            return true;
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(Action<T> action) where T : struct, Enum
        {
            var arr = Cache<T>.Values;
            int len = arr.Length;
            for (int i = 0; i < len; ++i)
            {
                action(arr[i]);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static T Parse<T>(this string str, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (!str.TryParse(out T result, ignoreCase, ignoreSpace))
                throw new ArgumentException($"'{str}' could not be converted to enum '{typeof(T)}'.");

            return result;
        }

        [MethodImpl(Opt.Inline)]
        public static T Parse<T>(this ReadOnlySpan<char> str, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (!str.TryParse(out T result, ignoreCase, ignoreSpace))
                throw new ArgumentException($"'{str.ToString()}' could not be converted to enum '{typeof(T)}'.");

            return result;
        }

        /// <summary>
        /// Enum.TryParse보다 훨씬 최적화된 파싱 함수
        /// </summary>
        public static bool TryParse<T>(this string str, out T result, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(str))
            {
                result = default;
                return false;
            }
            if (!ignoreSpace && (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[^1])))
            {
                result = default;
                return false;
            }
            var map = ignoreCase ? Cache<T>.StringMapIgnoreCase : Cache<T>.StringMap;
            if (map.TryGetValue(str, out result))
                return true;

            // ignoreSpace인 경우 str.Trim()해서 체크해야 하는데, GC 발생을 피하기 위해 Span버전으로 넘겨서 처리한다.
            return ignoreSpace
                ? str.AsSpan().TryParse(out result, ignoreCase, ignoreSpace)
                : str.AsSpan().TryParseFromIntForm(out result);
        }

        /// <summary>
        /// Span버전은 GC 발생 안되는 것을 최우선으로 하기 때문에
        /// Dictionary으로 탐색 불가능하여 선형탐색으로 찾는다. (추후 .NET 9의 AlternateLookup 사용 가능해지면 수정 필요)
        /// </summary>
        public static bool TryParse<T>(this ReadOnlySpan<char> str, out T result, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            var span = ignoreSpace ? str.Trim() : str;
            if (span.IsEmpty)
            {
                result = default;
                return false;
            }
            int spanLen = span.Length;
            var option = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var names = Cache<T>.Names;
            int count = Cache<T>.Count;
            for (int i = 0; i < count; ++i)
            {
                if (names[i].Length != spanLen)
                    continue;

                if (span.Equals(names[i], option))
                {
                    result = Cache<T>.Values[i];
                    return true;
                }
            }
            return span.TryParseFromIntForm(out result);
        }

        /// <summary>
        /// "1"과 같이 숫자값인 문자열을 받았을 때, enum의 실제값 중 일치하는 값이 있다면 변환해준다.
        /// </summary>
        [MethodImpl(Opt.Inline)]
        private static bool TryParseFromIntForm<T>(this ReadOnlySpan<char> str, out T result) where T : struct, Enum
        {
            if (int.TryParse(str, out int intVal))
            {
                int idx = Array.IndexOf(Cache<T>.IntValues, intVal, 0);
                if (idx != -1)
                {
                    result = Cache<T>.Values[idx];
                    return true;
                }
            }
            result = default;
            return false;
        }

        private static class Cache<T> where T : struct, Enum
        {
            public static readonly T[] Values;
            public static readonly int Count;
            public static readonly string[] Names;
            public static readonly int[] IntValues;
            public static readonly HashSet<T> ValueSet;
            public static readonly Dictionary<string, T> StringMap;
            public static readonly Dictionary<string, T> StringMapIgnoreCase;

            static Cache()
            {
                Values = (T[])Enum.GetValues(typeof(T));
                Count = Values.Length;
                Names = Enum.GetNames(typeof(T));
                IntValues = new int[Count];
                ValueSet = new(Values);
                StringMap = new(Count);
                StringMapIgnoreCase = new(Count, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < Count; ++i)
                {
                    var val = Values[i];
                    var name = Names[i];
                    IntValues[i] = (int)(object)val; // 최초 1회 파싱이므로 박싱 발생해도 괜찮다.
                    StringMap.Add(name, val);
                    StringMapIgnoreCase.TryAdd(name, val); // ignore case 중복 허용 (첫 번째 값 우선)
                }
            }
        }
    }
}
