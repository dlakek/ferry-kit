using System;

namespace FerryKit
{
    public static class ArrayHelper
    {
        public static void ForEach<T>(this T[] collection, Action<T> action)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                action(collection[i]);
            }
        }

        public static void ForEach<T>(this T[] collection, Action<T, int> action)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                action(collection[i], i);
            }
        }

        public static int FindIndex<T>(this T[] collection, Predicate<T> match)
        {
            int len = collection.Length;
            for (int i = 0; i < len; ++i)
            {
                if (match(collection[i]))
                    return i;
            }
            return -1;
        }

        public static T Find<T>(this T[] collection, Predicate<T> match)
        {
            int idx = collection.FindIndex(match);
            return idx != -1
                ? collection[idx]
                : default;
        }
    }
}
