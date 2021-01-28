using System;
using System.Linq;
using System.Threading.Tasks;

namespace Pagination
{
    public static class PaginationExtensions
    {
        public static PaginatedResponse<TTo> SelectPage<TItem, TTo>(this PaginatedResponse<TItem> page, Func<TItem, TTo> func)
        {
            return new PaginatedResponse<TTo>
            {
                Items = page.Items.Select(func).ToList(),
                HasMore = page.HasMore,
                ContinuationToken = page.ContinuationToken,
                Skip = page.Skip,
                Take = page.Take,
                Total = page.Total,
            };
        }
    }
}
