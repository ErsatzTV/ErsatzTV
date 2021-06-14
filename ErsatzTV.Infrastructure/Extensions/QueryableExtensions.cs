using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<Option<T>> SelectOneAsync<T, TKey>(
            this IQueryable<T> enumerable,
            Expression<Func<T, TKey>> keySelector,
            Expression<Func<T, bool>> predicate) where T : class =>
            await enumerable.OrderBy(keySelector).FirstOrDefaultAsync(predicate);
    }
}
