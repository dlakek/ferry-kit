using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static bool IsDefined<T>(this T value) where T : struct, Enum => Cache<T>.ValueSet.Contains(value);

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
            // The storage size is constant for each closed generic enum type,
            // enabling JIT/AOT compilers to fold the switch to the matching operation.
            _ = Unsafe.SizeOf<T>() switch
            {
                sizeof(byte) => Unsafe.As<T, byte>(ref target) |= Unsafe.As<T, byte>(ref next),
                sizeof(ushort) => Unsafe.As<T, ushort>(ref target) |= Unsafe.As<T, ushort>(ref next),
                sizeof(uint) => Unsafe.As<T, uint>(ref target) |= Unsafe.As<T, uint>(ref next),
                sizeof(ulong) => Unsafe.As<T, ulong>(ref target) |= Unsafe.As<T, ulong>(ref next),
                _ => throw CreateUnsupportedEnumSizeException<T>(Unsafe.SizeOf<T>()),
            };
        }

        [MethodImpl(Opt.Inline)]
        public static ulong ToUInt64Bits<T>(T value) where T : struct, Enum
        {
            return Unsafe.SizeOf<T>() switch
            {
                sizeof(byte) => Unsafe.As<T, byte>(ref value),
                sizeof(ushort) => Unsafe.As<T, ushort>(ref value),
                sizeof(uint) => Unsafe.As<T, uint>(ref value),
                sizeof(ulong) => Unsafe.As<T, ulong>(ref value),
                _ => throw CreateUnsupportedEnumSizeException<T>(Unsafe.SizeOf<T>()),
            };
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
                : str.AsSpan().TryParseFromNumericForm(out result);
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
            return span.TryParseFromNumericForm(out result);
        }

        /// <summary>
        /// When a string with a numeric value such as "1" is received,
        /// it is converted if there is a matching value among the actual values ​​of the enum.
        /// </summary>
        [MethodImpl(Opt.Inline)]
        private static bool TryParseFromNumericForm<T>(this ReadOnlySpan<char> str, out T result) where T : struct, Enum
        {
            if (str.TryParseUnderlyingValue<T>(out ulong numericValue))
            {
                int idx = Array.IndexOf(Cache<T>.NumericValues, numericValue, 0);
                if (idx != -1)
                {
                    result = Cache<T>.Values[idx];
                    return true;
                }
            }
            result = default;
            return false;
        }

        [MethodImpl(Opt.Inline)]
        private static bool TryParseUnderlyingValue<T>(this ReadOnlySpan<char> str, out ulong result) where T : struct, Enum
        {
            switch (Cache<T>.UnderlyingTypeCode)
            {
                case TypeCode.SByte:
                    bool sbyteParsed = sbyte.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteValue);
                    result = unchecked((byte)sbyteValue);
                    return sbyteParsed;
                case TypeCode.Byte:
                    bool byteParsed = byte.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteValue);
                    result = byteValue;
                    return byteParsed;
                case TypeCode.Int16:
                    bool shortParsed = short.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortValue);
                    result = unchecked((ushort)shortValue);
                    return shortParsed;
                case TypeCode.UInt16:
                    bool ushortParsed = ushort.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortValue);
                    result = ushortValue;
                    return ushortParsed;
                case TypeCode.Int32:
                    bool intParsed = int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue);
                    result = unchecked((uint)intValue);
                    return intParsed;
                case TypeCode.UInt32:
                    bool uintParsed = uint.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintValue);
                    result = uintValue;
                    return uintParsed;
                case TypeCode.Int64:
                    bool longParsed = long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue);
                    result = unchecked((ulong)longValue);
                    return longParsed;
                case TypeCode.UInt64:
                    return ulong.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
                default:
                    result = default;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static NotSupportedException CreateUnsupportedEnumSizeException<T>(int size) where T : struct, Enum
            => new($"Enum '{typeof(T)}' uses an unsupported storage size: {size} bytes.");

        private static class Cache<T> where T : struct, Enum
        {
            public static readonly T[] Values;
            public static readonly int Count;
            public static readonly string[] Names;
            public static readonly ulong[] NumericValues;
            public static readonly TypeCode UnderlyingTypeCode;
            public static readonly HashSet<T> ValueSet;
            public static readonly Dictionary<string, T> StringMap;
            public static readonly Dictionary<string, T> StringMapIgnoreCase;

            static Cache()
            {
                Values = (T[])Enum.GetValues(typeof(T));
                Count = Values.Length;
                Names = Enum.GetNames(typeof(T));
                NumericValues = new ulong[Count];
                UnderlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));
                ValueSet = new(Values);
                StringMap = new(Count);
                StringMapIgnoreCase = new(Count, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < Count; ++i)
                {
                    var val = Values[i];
                    var name = Names[i];
                    NumericValues[i] = ToUInt64Bits(val);
                    StringMap.Add(name, val);
                    StringMapIgnoreCase.TryAdd(name, val); // ignore case allows duplicates (first value takes precedence)
                }
            }
        }
    }
}
