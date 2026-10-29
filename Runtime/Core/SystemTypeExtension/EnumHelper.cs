using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class EnumHelper
    {
        private const int _linearValueThreshold = 16;
        private const int _binaryValueThreshold = 64;

        private static readonly IFormatProvider _format = CultureInfo.InvariantCulture;

        [MethodImpl(Opt.Inline)] public static int Count<T>() where T : struct, Enum => ValueCache<T>.Count;
        [MethodImpl(Opt.Inline)] public static ReadOnlySpan<T> GetValues<T>() where T : struct, Enum => new(ValueCache<T>.Values);
        [MethodImpl(Opt.Inline)] public static bool IsDefined<T>(this T value) where T : struct, Enum => ValueLookupCache<T>.Contains(value);
        [MethodImpl(Opt.Inline)] public static ulong ToUInt64Bits<T>(this T value) where T : struct, Enum => ToUInt64BitsUnchecked(value);
        [MethodImpl(Opt.Inline)] public static void AccumulateFlag<T>(this ref T target, T next) where T : struct, Enum => AccumulateFlagUnchecked(ref target, next);

        [MethodImpl(Opt.Inline)]
        public static bool TrueForAll<T>(Predicate<T> match) where T : struct, Enum
        {
            var values = ValueCache<T>.Values;
            for (int i = 0; i < values.Length; ++i)
            {
                if (!match(values[i]))
                    return false;
            }
            return true;
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(Action<T> action) where T : struct, Enum
        {
            var values = ValueCache<T>.Values;
            for (int i = 0; i < values.Length; ++i)
            {
                action(values[i]);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static T Parse<T>(this string value, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (!value.TryParse(out T result, ignoreCase, ignoreSpace))
                return ThrowConvertFailException<T>(value);

            return result;
        }

        [MethodImpl(Opt.Inline)]
        public static T Parse<T>(this ReadOnlySpan<char> value, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (!value.TryParse(out T result, ignoreCase, ignoreSpace))
                return ThrowConvertFailException<T>(value.ToString());

            return result;
        }

        [MethodImpl(Opt.Inline)]
        public static bool TryParse<T>(this string value, out T result, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value))
            {
                result = default;
                return false;
            }
            if (value.HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck())
            {
                if (ignoreSpace)
                    return value.AsSpan().TryParseUnchecked(out result, ignoreCase, ignoreSpace);

                result = default;
                return false;
            }
            if (value.IsNumericStartSkipEmptyCheck())
                return NumericCache<T>.TryParse(value.AsSpan(), out result);

            return ignoreCase
                ? StringCache<T, IgnoreCaseMode>.TryGetValue(value, out result)
                : StringCache<T, OrdinalMode>.TryGetValue(value, out result);
        }

        [MethodImpl(Opt.Inline)]
        public static bool TryParse<T>(this ReadOnlySpan<char> value, out T result, bool ignoreCase = false, bool ignoreSpace = true) where T : struct, Enum
        {
            return value.TryParseUnchecked(out result, ignoreCase, ignoreSpace);
        }

        [MethodImpl(Opt.Inline)]
        internal static bool TryParseUnchecked<T>(this ReadOnlySpan<char> value, out T result, bool ignoreCase = false, bool ignoreSpace = true)
        {
            if (ignoreSpace)
            {
                value = value.Trim();
            }
            if (value.IsEmpty || (!ignoreSpace && value.HasLeadingOrTrailingWhiteSpaceSkipEmptyCheck()))
            {
                result = default;
                return false;
            }
            if (value.IsNumericStartSkipEmptyCheck())
                return NumericCache<T>.TryParse(value, out result);

            return ignoreCase
                ? SpanCache<T, IgnoreCaseMode>.TryGetValue(value, out result)
                : SpanCache<T, OrdinalMode>.TryGetValue(value, out result);
        }

        [MethodImpl(Opt.Inline)]
        internal static void AccumulateFlagUnchecked<T>(ref T target, T next)
        {
            _ = Unsafe.SizeOf<T>() switch
            {
                sizeof(byte) => Unsafe.As<T, byte>(ref target) |= Unsafe.As<T, byte>(ref next),
                sizeof(ushort) => Unsafe.As<T, ushort>(ref target) |= Unsafe.As<T, ushort>(ref next),
                sizeof(uint) => Unsafe.As<T, uint>(ref target) |= Unsafe.As<T, uint>(ref next),
                sizeof(ulong) => Unsafe.As<T, ulong>(ref target) |= Unsafe.As<T, ulong>(ref next),
                _ => ThrowUnsupportedEnumSize<T>(),
            };
        }

        [MethodImpl(Opt.Inline)]
        private static ulong ToUInt64BitsUnchecked<T>(T value)
        {
            return Unsafe.SizeOf<T>() switch
            {
                sizeof(byte) => Unsafe.As<T, byte>(ref value),
                sizeof(ushort) => Unsafe.As<T, ushort>(ref value),
                sizeof(uint) => Unsafe.As<T, uint>(ref value),
                sizeof(ulong) => Unsafe.As<T, ulong>(ref value),
                _ => ThrowUnsupportedEnumSize<T>(),
            };
        }

        [MethodImpl(Opt.NoInline)]
        private static T ThrowConvertFailException<T>(string value)
        {
            throw new ArgumentException($"'{value}' could not be converted to enum '{typeof(T)}'.");
        }

        [MethodImpl(Opt.NoInline)]
        private static ulong ThrowUnsupportedEnumSize<T>()
        {
            throw new NotSupportedException($"Enum '{typeof(T)}' uses an unsupported storage size: {Unsafe.SizeOf<T>()} bytes.");
        }

        private static class ValueCache<T>
        {
            public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
            public static readonly int Count = Values.Length;
        }

        private static class NameCache<T>
        {
            public static readonly string[] Values = Enum.GetNames(typeof(T));
        }

        private static class NumericCache<T>
        {
            private const NumberStyles _style = NumberStyles.Integer;

            private static readonly TypeCode _underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));

            public static bool TryParse(ReadOnlySpan<char> str, out T result) => _underlyingTypeCode switch
            {
                TypeCode.SByte => TryParsedValue(sbyte.TryParse(str, _style, _format, out sbyte value), value, out result),
                TypeCode.Byte => TryParsedValue(byte.TryParse(str, _style, _format, out byte value), value, out result),
                TypeCode.Int16 => TryParsedValue(short.TryParse(str, _style, _format, out short value), value, out result),
                TypeCode.UInt16 => TryParsedValue(ushort.TryParse(str, _style, _format, out ushort value), value, out result),
                TypeCode.Int32 => TryParsedValue(int.TryParse(str, _style, _format, out int value), value, out result),
                TypeCode.UInt32 => TryParsedValue(uint.TryParse(str, _style, _format, out uint value), value, out result),
                TypeCode.Int64 => TryParsedValue(long.TryParse(str, _style, _format, out long value), value, out result),
                TypeCode.UInt64 => TryParsedValue(ulong.TryParse(str, _style, _format, out ulong value), value, out result),
                _ => Fail(out result)
            };

            [MethodImpl(Opt.Inline)]
            private static bool TryParsedValue<TValue>(bool success, TValue value, out T result) where TValue : struct => success
                ? ValueLookupCache<T>.TryGetValue(Unsafe.As<TValue, T>(ref value), out result)
                : Fail(out result);

            [MethodImpl(Opt.Inline)]
            private static bool Fail(out T result)
            {
                result = default;
                return false;
            }
        }

        private static class ValueLookupCache<T>
        {
            private enum LookupMode : byte
            {
                Linear,
                Binary,
                HashSet,
            }

            private static readonly LookupMode _mode = ValueCache<T>.Count < _linearValueThreshold
                ? LookupMode.Linear
                : ValueCache<T>.Count < _binaryValueThreshold
                    ? LookupMode.Binary
                    : LookupMode.HashSet;

            private static readonly ulong[] _sortedBits = _mode == LookupMode.HashSet ? null : CreateSortedBits();
            private static readonly HashSet<T> _values = _mode == LookupMode.HashSet ? new(ValueCache<T>.Values) : null;

            [MethodImpl(Opt.Inline)]
            public static bool TryGetValue(T value, out T result)
            {
                if (Contains(value))
                {
                    result = value;
                    return true;
                }
                result = default;
                return false;
            }

            [MethodImpl(Opt.Inline)]
            public static bool Contains(T value) => _mode switch
            {
                LookupMode.Linear => LinearSearch(_sortedBits, ToUInt64BitsUnchecked(value)),
                LookupMode.Binary => BinarySearch(_sortedBits, ToUInt64BitsUnchecked(value)),
                LookupMode.HashSet => _values.Contains(value),
                _ => false,
            };

            [MethodImpl(Opt.Inline)]
            private static bool LinearSearch(ulong[] values, ulong target)
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    if (values[i] == target)
                        return true;
                }
                return false;
            }

            [MethodImpl(Opt.Inline)]
            private static bool BinarySearch(ulong[] values, ulong target)
            {
                int low = 0;
                int high = values.Length - 1;
                while (low <= high)
                {
                    int middle = low + ((high - low) >> 1);
                    var value = values[middle];
                    if (value == target)
                        return true;

                    if (value < target)
                    {
                        low = middle + 1;
                    }
                    else
                    {
                        high = middle - 1;
                    }
                }
                return false;
            }

            private static ulong[] CreateSortedBits()
            {
                var values = ValueCache<T>.Values;
                var result = new ulong[values.Length];
                for (int i = 0; i < values.Length; ++i)
                {
                    result[i] = ToUInt64BitsUnchecked(values[i]);
                }
                return result;
            }
        }

        private readonly struct OrdinalMode { }
        private readonly struct IgnoreCaseMode { }

        private static class Compare<TMode>
        {
            public static readonly StringComparer Comparer = typeof(TMode) == typeof(IgnoreCaseMode) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            public static StringComparison Comparison
            {
                [MethodImpl(Opt.Inline)]
                get => typeof(TMode) == typeof(IgnoreCaseMode) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            }
        }

        private static class StringCache<T, TMode> where T : struct, Enum
        {
            private static readonly Dictionary<string, T> _values = CreateStringCache();

            [MethodImpl(Opt.Inline)]
            public static bool TryGetValue(string key, out T result) => _values.TryGetValue(key, out result);

            private static Dictionary<string, T> CreateStringCache()
            {
                var result = new Dictionary<string, T>(ValueCache<T>.Count, Compare<TMode>.Comparer);
                var values = ValueCache<T>.Values;
                var names = NameCache<T>.Values;
                for (int i = 0; i < names.Length; ++i)
                {
                    result.TryAdd(names[i], values[i]);
                }
                return result;
            }
        }

        private static class SpanCache<T, TMode>
        {
            private static readonly int[] _indices = CreateSortedNameIndices();

            [MethodImpl(Opt.Inline)]
            public static bool TryGetValue(ReadOnlySpan<char> key, out T result)
            {
                return _indices == null
                    ? LinearSearch(key, out result)
                    : BinarySearch(key, out result);
            }

            [MethodImpl(Opt.Inline)]
            private static bool LinearSearch(ReadOnlySpan<char> key, out T result)
            {
                var comp = Compare<TMode>.Comparison;
                var names = NameCache<T>.Values;
                for (int i = 0; i < names.Length; ++i)
                {
                    if (names[i].Length == key.Length && key.Equals(names[i], comp))
                    {
                        result = ValueCache<T>.Values[i];
                        return true;
                    }
                }
                result = default;
                return false;
            }

            [MethodImpl(Opt.Inline)]
            private static bool BinarySearch(ReadOnlySpan<char> key, out T result)
            {
                var comp = Compare<TMode>.Comparison;
                var names = NameCache<T>.Values;
                var indices = _indices;
                int found = -1;
                int low = 0;
                int high = indices.Length - 1;
                while (low <= high)
                {
                    int middle = low + ((high - low) >> 1);
                    int index = indices[middle];
                    int compare = key.CompareTo(names[index].AsSpan(), comp);
                    if (compare > 0)
                    {
                        low = middle + 1;
                    }
                    else if (compare < 0)
                    {
                        high = middle - 1;
                    }
                    else if (comp == StringComparison.OrdinalIgnoreCase)
                    {
                        found = index;
                        high = middle - 1;
                    }
                    else
                    {
                        result = ValueCache<T>.Values[index];
                        return true;
                    }
                }
                if (found != -1)
                {
                    result = ValueCache<T>.Values[found];
                    return true;
                }
                result = default;
                return false;
            }

            private static int[] CreateSortedNameIndices()
            {
                int count = ValueCache<T>.Count;
                if (count < _linearValueThreshold)
                    return null;

                var names = NameCache<T>.Values;
                var indices = new int[count];
                for (int i = 0; i < count; ++i)
                {
                    indices[i] = i;
                }
                indices.Sort((left, right) =>
                {
                    int comparison = Compare<TMode>.Comparer.Compare(names[left], names[right]);
                    return comparison != 0 ? comparison : left.CompareTo(right);
                });
                return indices;
            }
        }
    }
}
