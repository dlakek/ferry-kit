using System;
using System.Linq.Expressions;

namespace FerryKit.Core
{
    public static class ExpressionCache<T> where T : new()
    {
        public static readonly Func<T> New = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
    }
}
