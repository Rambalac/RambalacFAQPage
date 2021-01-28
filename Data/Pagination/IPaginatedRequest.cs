using System.Text.Json.Serialization;

namespace Pagination
{
    public interface IPaginatedRequest
    {
        [JsonPropertyName("s")]

        int Skip { get; }

        [JsonPropertyName("t")]
        int Take { get; }
    }
}
