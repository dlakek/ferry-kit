using System;
using System.Linq.Expressions;

namespace FerryKit
{
    public static class ExpressionCache<T> where T : new()
    {
        public static readonly Func<T> Creator = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
    }
}
