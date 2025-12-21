using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit
{
    public static class EnumHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> GetValues<T>() where T : struct, Enum => new(Cache<T>.Values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefined<T>(T value) where T : struct, Enum => Cache<T>.ValueSet.Contains(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<T>() where T : struct, Enum => Cache<T>.Values.Length;

        public static void ForEach<T>(Action<T> action) where T : struct, Enum
        {
            var values = Cache<T>.Values;
            int length = values.Length;
            for (int i = 0; i < length; ++i)
            {
                action(values[i]);
            }
        }

        public static T ToEnum<T>(this string str, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (!str.TryToEnum(out T result, ignoreCase, ignoreSpace))
                throw new ArgumentException($"'{str}' could not be converted to enum '{typeof(T)}'.");

            return result;
        }

        public static bool TryToEnum<T>(this string str, out T result, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(str))
            {
                result = default;
                return false;
            }
            if (!ignoreSpace && str.HasWhiteSpace())
            {
                result = default;
                return false;
            }
            var stringMap = ignoreCase ? Cache<T>.StringMapIgnoreCase : Cache<T>.StringMap;
            if (stringMap.TryGetValue(str, out result))
                return true;

            // Enum.TryParse는 비용이 크므로, 정확히 일치하는 문자열에 대해선 위에서 먼저 처리되도록 한다.
            return Enum.TryParse(str, ignoreCase, out result) && IsDefined(result);
        }

        private static class Cache<T> where T : struct, Enum
        {
            public static readonly T[] Values;
            public static readonly int Count;
            public static readonly HashSet<T> ValueSet;
            public static readonly Dictionary<string, T> StringMap;
            public static readonly Dictionary<string, T> StringMapIgnoreCase;

            static Cache()
            {
                Values = (T[])Enum.GetValues(typeof(T));
                Count = Values.Length;
                ValueSet = new(Values);
                StringMap = new(Count);
                StringMapIgnoreCase = new(Count, StringComparer.OrdinalIgnoreCase);

                var names = Enum.GetNames(typeof(T));
                for (int i = 0; i < Count; ++i)
                {
                    var val = Values[i];
                    var name = names[i];
                    StringMap.Add(name, val);
                    StringMapIgnoreCase.TryAdd(name, val); // ignore case인 경우 중복될 수 있으며, 이땐 첫 번째 값을 우선함
                }
            }
        }
    }
}
