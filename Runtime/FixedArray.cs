using FerryKit.Core;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FerryKit
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedArray2<T> : ITryParsable where T : unmanaged
    {
        public const int Capacity = 2;

        [SerializeField] private uint _count;
        [SerializeField] private T _item0;
        [SerializeField] private T _item1;

        public readonly int Count => (int)_count;
        public readonly T this[int index] => FixedArrayHelper.Get(in _item0, _count, Capacity, (uint)index);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ReadOnlySpan<char> value, P policy) where P : struct, IParsePolicy
            => FixedArrayHelper.TryParse(value, policy, Capacity, ref _count, ref _item0);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            => TryParse(reader.ReadNext(), reader.Policy);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedArray3<T> : ITryParsable where T : unmanaged
    {
        public const int Capacity = 3;

        [SerializeField] private uint _count;
        [SerializeField] private T _item0;
        [SerializeField] private T _item1;
        [SerializeField] private T _item2;

        public readonly int Count => (int)_count;
        public readonly T this[int index] => FixedArrayHelper.Get(in _item0, _count, Capacity, (uint)index);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ReadOnlySpan<char> value, P policy) where P : struct, IParsePolicy
            => FixedArrayHelper.TryParse(value, policy, Capacity, ref _count, ref _item0);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            => TryParse(reader.ReadNext(), reader.Policy);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedArray4<T> : ITryParsable where T : unmanaged
    {
        public const int Capacity = 4;

        [SerializeField] private uint _count;
        [SerializeField] private T _item0;
        [SerializeField] private T _item1;
        [SerializeField] private T _item2;
        [SerializeField] private T _item3;

        public readonly int Count => (int)_count;
        public readonly T this[int index] => FixedArrayHelper.Get(in _item0, _count, Capacity, (uint)index);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ReadOnlySpan<char> value, P policy) where P : struct, IParsePolicy
            => FixedArrayHelper.TryParse(value, policy, Capacity, ref _count, ref _item0);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            => TryParse(reader.ReadNext(), reader.Policy);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedArray5<T> : ITryParsable where T : unmanaged
    {
        public const int Capacity = 5;

        [SerializeField] private uint _count;
        [SerializeField] private T _item0;
        [SerializeField] private T _item1;
        [SerializeField] private T _item2;
        [SerializeField] private T _item3;
        [SerializeField] private T _item4;

        public readonly int Count => (int)_count;
        public readonly T this[int index] => FixedArrayHelper.Get(in _item0, _count, Capacity, (uint)index);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ReadOnlySpan<char> value, P policy) where P : struct, IParsePolicy
            => FixedArrayHelper.TryParse(value, policy, Capacity, ref _count, ref _item0);

        [MethodImpl(Opt.Inline)]
        public bool TryParse<P>(ref LineReader<P> reader) where P : struct, IParsePolicy
            => TryParse(reader.ReadNext(), reader.Policy);
    }

    /// <summary>
    /// A helper class for managing fixed-size arrays, providing methods for safe access and parsing of elements.
    /// FixedArray item fields must remain contiguous, ordered, and of the same unmanaged type.
    /// StructLayout.Sequential plus the editor layout test protects this Unsafe.Add contract.
    /// </summary>
    internal static class FixedArrayHelper
    {
        [MethodImpl(Opt.Inline)]
        public static T Get<T>(in T first, uint count, uint capacity, uint index) where T : unmanaged
            => index < count && index < capacity
            ? Unsafe.Add(ref Unsafe.AsRef(in first), index)
            : default;

        public static bool TryParse<T, P>(ReadOnlySpan<char> value, P policy, uint capacity, ref uint count, ref T first)
            where T : unmanaged
            where P : struct, IParsePolicy
        {
            uint previousCount = count > capacity ? capacity : count;
            if (value.IsEmpty || value.IsWhiteSpace())
            {
                ClearRange(ref first, 0, previousCount);
                count = 0;
                return true;
            }
            Span<T> parsed = stackalloc T[(int)capacity];
            uint parsedCount = 0;
            var remaining = value;
            while (true)
            {
                if (parsedCount >= capacity)
                    return false;

                int idx = remaining.IndexOfUnquoted(policy.ArrSep, policy.EscapeMode);
                var token = idx == -1 ? remaining : remaining[..idx];
                if (!token.TryTo(out T item, policy))
                    return false;

                parsed[(int)parsedCount] = item;
                ++parsedCount;
                if (idx == -1)
                    return Complete(ref count, ref first, previousCount, parsed, parsedCount);

                remaining = remaining[(idx + 1)..];
                if (remaining.IsEmpty)
                    return Complete(ref count, ref first, previousCount, parsed, parsedCount);
            }
        }

        [MethodImpl(Opt.Inline)]
        private static bool Complete<T>(ref uint count, ref T first, uint previousCount, ReadOnlySpan<T> parsed, uint parsedCount)
            where T : unmanaged
        {
            for (uint i = 0; i < parsedCount; ++i)
            {
                Unsafe.Add(ref first, i) = parsed[(int)i];
            }
            ClearRange(ref first, parsedCount, previousCount);
            count = parsedCount;
            return true;
        }

        [MethodImpl(Opt.Inline)]
        private static void ClearRange<T>(ref T first, uint start, uint endExclusive) where T : unmanaged
        {
            for (uint i = start; i < endExclusive; ++i)
            {
                Unsafe.Add(ref first, i) = default;
            }
        }
    }
}
