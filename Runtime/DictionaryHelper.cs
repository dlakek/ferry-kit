using System;
using System.Collections.Generic;

namespace FerryKit
{
    public static class DictionaryHelper
    {
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> collection, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> collection, Action<TKey, TValue> action)
        {
            foreach (var e in collection)
            {
                action(e.Key, e.Value);
            }
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection collection, Action<TKey> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection collection, Action<TValue> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> collection, TKey key) where TValue : new()
        {
            if (!collection.TryGetValue(key, out var value))
            {
                if (typeof(TValue).IsValueType)
                {
                    value = default;
                }
                else
                {
                    value = ExpressionCache<TValue>.Creator();
                }
                collection.Add(key, value);
            }
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> collection, TKey key, TValue defaultValue)
        {
            if (!collection.TryGetValue(key, out var value))
            {
                value = defaultValue;
                collection.Add(key, value);
            }
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> collection, TKey key, Func<TValue> creator)
        {
            if (!collection.TryGetValue(key, out var value))
            {
                value = creator();
                collection.Add(key, value);
            }
            return value;
        }
    }
}
