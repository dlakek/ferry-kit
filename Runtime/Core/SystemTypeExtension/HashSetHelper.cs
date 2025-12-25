using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FerryKit.Core
{
    public static class HashSetHelper
    {
        [MethodImpl(Opt.Inline)]
        public static void ForEach<T>(this HashSet<T> collection, Action<T> action)
        {
            foreach (var e in collection)
            {
                action(e);
            }
        }
    }
}
