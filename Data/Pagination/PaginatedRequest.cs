using System.Runtime.Serialization;

namespace Pagination
{

    [DataContract]
    public class PaginatedRequest : IPaginatedRequest
    {
        public PaginatedRequest()
        {
        }

        public PaginatedRequest(int skip, int take = 10)
        {
            Skip = skip;
            Take = take;
        }

        [DataMember(Name = "s")]
        public int Skip { get; set; } = 0;

        [DataMember(Name = "t")]
        public int Take { get; set; } = 20;
    }
}
