using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pagination
{

    public interface IPaginatedResponse : IPaginatedRequest
    {
        [JsonPropertyName("hm")]
        bool HasMore { get; }

        [JsonPropertyName("ct")]
        string ContinuationToken { get; }

        [JsonPropertyName("tt")]
        long? Total { get; }
    }

    public interface IPaginatedResponse<TItem>: IPaginatedResponse
    {
        IList<TItem> Items { get; set; }

    }
}
