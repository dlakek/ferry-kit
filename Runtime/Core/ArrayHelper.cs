using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class ArrayHelper
    {
        [MethodImpl(Opt.Inline)]
        public static int IndexOf<T>(this T[] collection, T item)
        {
            return Array.IndexOf(collection, item);
        }

        [MethodImpl(Opt.Inline)]
        public static int IndexOf<T>(this T[] collection, T item, int index)
        {
            return Array.IndexOf(collection, item, index);
        }

        [MethodImpl(Opt.Inline)]
        public static int IndexOf<T>(this T[] collection, T item, int index, int count)
        {
            return Array.IndexOf(collection, item, index, count);
        }

        [MethodImpl(Opt.Inline)]
        public static int LastIndexOf<T>(this T[] collection, T item)
        {
            return Array.LastIndexOf(collection, item);
        }

        [MethodImpl(Opt.Inline)]
        public static int LastIndexOf<T>(this T[] collection, T item, int index)
        {
            return Array.LastIndexOf(collection, item, index);
        }

        [MethodImpl(Opt.Inline)]
        public static int LastIndexOf<T>(this T[] collection, T item, int index, int count)
        {
            return Array.LastIndexOf(collection, item, index, count);
        }

        [MethodImpl(Opt.Inline)]
        public static bool Contains<T>(this T[] collection, T item)
        {
            return Array.IndexOf(collection, item) != -1;
        }

        [MethodImpl(Opt.Inline)]
        public static void Reverse<T>(this T[] collection)
        {
            Array.Reverse(collection);
        }

        [MethodImpl(Opt.Inline)]
        public static void Reverse<T>(this T[] collection, int index, int count)
        {
            Array.Reverse(collection, index, count);
        }

        [MethodImpl(Opt.Inline)]
        public static void Sort<T>(this T[] collection)
        {
            Array.Sort(collection);
        }

        [MethodImpl(Opt.Inline)]
        public static void Sort<T>(this T[] collection, Comparison<T> comparison)
        {
            Array.Sort(collection, comparison);
        }

        [MethodImpl(Opt.Inline)]
        public static void Sort<T>(this T[] collection, IComparer<T> comparer)
        {
            Array.Sort(collection, comparer);
        }

        [MethodImpl(Opt.Inline)]
        public static int BinarySearch<T>(this T[] collection, T item)
        {
            return Array.BinarySearch(collection, item);
        }

        [MethodImpl(Opt.Inline)]
        public static int BinarySearch<T>(this T[] collection, T item, IComparer<T> comparer)
        {
            return Array.BinarySearch(collection, item, comparer);
        }

        [MethodImpl(Opt.Inline)]
        public static int FindIndex<T>(this T[] collection, Predicate<T> match)
        {
            return collection.FindIndex(0, collection.Length, match);
        }

        [MethodImpl(Opt.Inline)]
        public static int FindIndex<T>(this T[] collection, int startIndex, Predicate<T> match)
        {
            return collection.FindIndex(startIndex, collection.Length - startIndex, match);
        }

        [MethodImpl(Opt.Inline)]
        public static int FindIndex<T>(this T[] collection, int startIndex, int count, Predicate<T> match)
        {
            int end = startIndex + count;
            for (int i = startIndex; i < end; ++i)
            {
                if (match(collection[i]))
                    return i;
            }
            return -1;
        }

        [MethodImpl(Opt.Inline)]
        public static int FindLastIndex<T>(this T[] collection, Predicate<T> match)
        {
            return collection.FindLastIndex(collection.Length - 1, collection.Length, match);
        }

        [MethodImpl(Opt.Inline)]
        public static int FindLastIndex<T>(this T[] collection, int startIndex, Predicate<T> match)
        {
            return collection.FindLastIndex(startIndex, startIndex + 1, match);
        }

        [MethodImpl(Opt.Inline)]
        public static int FindLastIndex<T>(this T[] collection, int startIndex, int count, Predicate<T> match)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; --i)
            {
                if (match(collection[i]))
                    return i;
            }
            return -1;
        }

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
        public static bool Exists<T>(this T[] collection, Predicate<T> match)
        {
            return collection.FindIndex(match) != -1;
        }

        [MethodImpl(Opt.Inline)]
        public static bool TrueForAll<T>(this T[] collection, Predicate<T> match)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                if (!match(collection[i]))
                    return false;
            }
            return true;
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this T[] collection, Action<T> action)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                action(collection[i]);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this T[] collection, Action<T, int> action)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                action(collection[i], i);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static T[] FindAll<T>(this T[] collection, Predicate<T> match)
        {
            int len = collection.Length;
            var list = new List<T>(len);
            for (int i = 0; i < len; ++i)
            {
                if (match(collection[i]))
                {
                    list.Add(collection[i]);
                }
            }
            return list.ToArray();
        }

        [MethodImpl(Opt.Inline)]
        public static TOutput[] ConvertAll<T, TOutput>(this T[] collection, Converter<T, TOutput> converter)
        {
            int len = collection.Length;
            var output = new TOutput[len];
            for (int i = 0; i < len; ++i)
            {
                output[i] = converter(collection[i]);
            }
            return output;
        }

        [MethodImpl(Opt.Inline)]
        public static T[] GetRange<T>(this T[] collection, int index, int count)
        {
            return collection[index..(index + count)];
        }

        [MethodImpl(Opt.Inline)]
        public static T GetOrDefault<T>(this T[] collection, int index)
        {
            return (uint)index < (uint)collection.Length ? collection[index] : default;
        }
    }
}
