using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class DictionaryHelper
    {
        [MethodImpl(Opt.Inline)]
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> collection, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue> collection, Action<TKey, TValue> action)
        {
            foreach (var e in collection)
            {
                action(e.Key, e.Value);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue>.KeyCollection collection, Action<TKey> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        [MethodImpl(Opt.Inline)]
        public static void ForEach<TKey, TValue>(this Dictionary<TKey, TValue>.ValueCollection collection, Action<TValue> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }

        [MethodImpl(Opt.Inline)]
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

        [MethodImpl(Opt.Inline)]
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> collection, TKey key, TValue defaultValue)
        {
            if (!collection.TryGetValue(key, out var value))
            {
                value = defaultValue;
                collection.Add(key, value);
            }
            return value;
        }

        [MethodImpl(Opt.Inline)]
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
