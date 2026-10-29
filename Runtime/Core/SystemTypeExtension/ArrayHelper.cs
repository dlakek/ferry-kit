using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class ArrayHelper
    {
        [MethodImpl(Opt.Inline)] public static int IndexOf<T>(this T[] collection, T item) => Array.IndexOf(collection, item);
        [MethodImpl(Opt.Inline)] public static int IndexOf<T>(this T[] collection, T item, int index) => Array.IndexOf(collection, item, index);
        [MethodImpl(Opt.Inline)] public static int IndexOf<T>(this T[] collection, T item, int index, int count) => Array.IndexOf(collection, item, index, count);
        [MethodImpl(Opt.Inline)] public static int LastIndexOf<T>(this T[] collection, T item) => Array.LastIndexOf(collection, item);
        [MethodImpl(Opt.Inline)] public static int LastIndexOf<T>(this T[] collection, T item, int index) => Array.LastIndexOf(collection, item, index);
        [MethodImpl(Opt.Inline)] public static int LastIndexOf<T>(this T[] collection, T item, int index, int count) => Array.LastIndexOf(collection, item, index, count);
        [MethodImpl(Opt.Inline)] public static bool Contains<T>(this T[] collection, T item) => Array.IndexOf(collection, item) != -1;
        [MethodImpl(Opt.Inline)] public static void Reverse<T>(this T[] collection) => Array.Reverse(collection);
        [MethodImpl(Opt.Inline)] public static void Reverse<T>(this T[] collection, int index, int count) => Array.Reverse(collection, index, count);
        [MethodImpl(Opt.Inline)] public static void Sort<T>(this T[] collection) => Array.Sort(collection);
        [MethodImpl(Opt.Inline)] public static void Sort<T>(this T[] collection, Comparison<T> comparison) => Array.Sort(collection, comparison);
        [MethodImpl(Opt.Inline)] public static void Sort<T>(this T[] collection, IComparer<T> comparer) => Array.Sort(collection, comparer);
        [MethodImpl(Opt.Inline)] public static int BinarySearch<T>(this T[] collection, T item) => Array.BinarySearch(collection, item);
        [MethodImpl(Opt.Inline)] public static int BinarySearch<T>(this T[] collection, T item, IComparer<T> comparer) => Array.BinarySearch(collection, item, comparer);
        [MethodImpl(Opt.Inline)] public static int FindIndex<T>(this T[] collection, Predicate<T> match) => Array.FindIndex(collection, match);
        [MethodImpl(Opt.Inline)] public static int FindIndex<T>(this T[] collection, int startIndex, Predicate<T> match) => Array.FindIndex(collection, startIndex, match);
        [MethodImpl(Opt.Inline)] public static int FindLastIndex<T>(this T[] collection, Predicate<T> match) => Array.FindLastIndex(collection, match);
        [MethodImpl(Opt.Inline)] public static int FindLastIndex<T>(this T[] collection, int startIndex, Predicate<T> match) => Array.FindLastIndex(collection, startIndex, match);
        [MethodImpl(Opt.Inline)] public static bool Exists<T>(this T[] collection, Predicate<T> match) => collection.FindIndex(match) != -1;
        [MethodImpl(Opt.Inline)] public static T[] GetRange<T>(this T[] collection, int index, int count) => collection[index..(index + count)];
        [MethodImpl(Opt.Inline)] public static T GetOrDefault<T>(this T[] collection, int index) => unchecked((uint)index) < (uint)collection.Length ? collection[index] : default;

        [MethodImpl(Opt.Inline)]
        public static int FindIndex<T>(this T[] collection, int startIndex, int count, Predicate<T> match)
            => Array.FindIndex(collection, startIndex, count, match);

        [MethodImpl(Opt.Inline)]
        public static int FindLastIndex<T>(this T[] collection, int startIndex, int count, Predicate<T> match)
            => Array.FindLastIndex(collection, startIndex, count, match);

        [MethodImpl(Opt.Inline)]
        public static T Find<T>(this T[] collection, Predicate<T> match)
        {
            int idx = collection.FindIndex(match);
            return idx != -1 ? collection[idx] : default;
        }

        [MethodImpl(Opt.Inline)]
        public static T FindLast<T>(this T[] collection, Predicate<T> match)
        {
            int idx = collection.FindLastIndex(match);
            return idx != -1 ? collection[idx] : default;
        }

        [MethodImpl(Opt.Inline)]
        public static bool TrueForAll<T>(this T[] collection, Predicate<T> match)
            => Array.TrueForAll(collection, match);

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this T[] collection, Action<T> action)
            => Array.ForEach(collection, action);

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this T[] collection, Action<T, int> action)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            for (int i = 0; i < collection.Length; ++i)
            {
                action(collection[i], i);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static T[] FindAll<T>(this T[] collection, Predicate<T> match)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            List<T> list = null;
            for (int i = 0; i < collection.Length; ++i)
            {
                if (match(collection[i]))
                {
                    (list ??= new(collection.Length - i)).Add(collection[i]);
                }
            }
            return list?.ToArray() ?? Array.Empty<T>();
        }

        [MethodImpl(Opt.Inline)]
        public static TOutput[] ConvertAll<T, TOutput>(this T[] collection, Converter<T, TOutput> converter)
            => Array.ConvertAll(collection, converter);
    }
}
