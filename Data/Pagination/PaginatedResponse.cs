using System.Collections.Generic;
using System.Linq;

namespace Pagination
{
    public class PaginatedResponse<TItem> : IPaginatedResponse<TItem>
    {
        public IList<TItem> Items { get; set; }

        public bool HasMore { get; set; }

        public string ContinuationToken { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public long? Total { get; set; }

        public PaginatedRequest NextPage()
        {
            return new PaginatedRequest
            {
                Skip = Skip + Take,
                Take = Take,
            };
        }
    }


}
