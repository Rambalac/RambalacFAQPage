using System.Text.Json.Serialization;

namespace RambalacHome.Function
{
    public class IpGeocode
    {
        public class CountryRegionModel
        {
            [JsonPropertyName("isoCode")]
            public string IsoCode { get; set; }
        }

        [JsonPropertyName("countryRegion")]
        public CountryRegionModel CountryRegion { get; set; }
    }
}
