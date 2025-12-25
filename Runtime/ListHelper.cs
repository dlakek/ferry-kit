using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit
{
    public static class ListHelper
    {
        [MethodImpl(Opt.Inline)]
        public static void Assign<T>(this List<T> collection, int count, T value = default)
        {
            collection.Clear();
            if (collection.Capacity < count)
            {
                collection.Capacity = count;
            }
            for (int i = 0; i < count; ++i)
            {
                collection.Add(value);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void Assign<T>(this List<T> collection, int count, Func<T> creator)
        {
            collection.Clear();
            if (collection.Capacity < count)
            {
                collection.Capacity = count;
            }
            for (int i = 0; i < count; ++i)
            {
                collection.Add(creator());
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void Append<T>(this List<T> collection, int count, T value = default)
        {
            if (collection.Capacity < collection.Count + count)
            {
                collection.Capacity = collection.Count + count;
            }
            for (int i = 0; i < count; ++i)
            {
                collection.Add(value);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void Append<T>(this List<T> collection, int count, Func<T> creator)
        {
            if (collection.Capacity < collection.Count + count)
            {
                collection.Capacity = collection.Count + count;
            }
            for (int i = 0; i < count; ++i)
            {
                collection.Add(creator());
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this List<T> collection, Action<T, int> action)
        {
            int len = collection.Count;
            for (int i = 0; i < len; ++i)
            {
                action(collection[i], i);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this LinkedList<T> collection, Action<T> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this LinkedList<T> collection, Action<T, int> action)
        {
            int i = -1;
            foreach (var e in collection)
            {
                action(e, ++i);
            }
        }
    }
}
