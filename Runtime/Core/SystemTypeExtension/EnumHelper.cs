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

        /// <summary>
        /// Bitwise OR operation (|=) for generic enums
        /// </summary>
        [MethodImpl(Opt.Inline)]
        public static void AccumulateFlag<T>(this ref T target, T next) where T : struct, Enum
        {
            // Since JIT removes dead code based on the size of T,
            // only the corresponding operation is performed at runtime without condition checking.
            if (Unsafe.SizeOf<T>() == 4) // int, uint
            {
                Unsafe.As<T, int>(ref target) |= Unsafe.As<T, int>(ref next);
            }
            else if (Unsafe.SizeOf<T>() == 8) // long, ulong
            {
                Unsafe.As<T, long>(ref target) |= Unsafe.As<T, long>(ref next);
            }
            else if (Unsafe.SizeOf<T>() == 1) // byte, sbyte
            {
                Unsafe.As<T, byte>(ref target) |= Unsafe.As<T, byte>(ref next);
            }
            else if (Unsafe.SizeOf<T>() == 2) // short, ushort
            {
                Unsafe.As<T, short>(ref target) |= Unsafe.As<T, short>(ref next);
            }
            else
            {
                throw new NotSupportedException($"Enum size {Unsafe.SizeOf<T>()} not supported");
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
        /// A parsing function that is much more optimized than Enum.TryParse
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

            // In case of ignoreSpace, str.Trim() should be used to check,
            // but to avoid GC occurrence, it is passed on to the Span version and processed.
            return ignoreSpace
                ? str.AsSpan().TryParse(out result, ignoreCase, ignoreSpace)
                : str.AsSpan().TryParseFromIntForm(out result);
        }

        /// <summary>
        /// Since the Span version prioritizes preventing GC,
        /// it cannot be searched using a dictionary, so it uses linear search.
        /// (This will need to be modified when AlternateLookup in .NET 9 becomes available.)
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
        /// When a string with a numeric value such as "1" is received,
        /// it is converted if there is a matching value among the actual values ​​of the enum.
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
                    IntValues[i] = (int)(object)val; // Since this is the first parsing, it is okay if boxing occurs.
                    StringMap.Add(name, val);
                    StringMapIgnoreCase.TryAdd(name, val); // ignore case allows duplicates (first value takes precedence)
                }
            }
        }
    }
}
