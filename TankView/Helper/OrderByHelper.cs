using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TankView.Helper
{
    public static class EnumerableHelpers
    {
        public static IOrderedEnumerable<TSource> OrderByWithDirection<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool descending)
        {
            if(source == null) return Array.Empty<TSource>().OrderBy(keySelector);
            return descending ? source.OrderByDescending(keySelector)
                       : source.OrderBy(keySelector);
        }

        public static IOrderedQueryable<TSource> OrderByWithDirection<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool descending)
        {
            return descending ? source.OrderByDescending(keySelector)
                       : source.OrderBy(keySelector);
        }
    }
}
