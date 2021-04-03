using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jeevan.NuGetClient.JsonModels
{
    internal sealed class SearchQueryResponseJsonModel
    {
        [JsonPropertyName("totalHits")]
        public int TotalHits { get; set; }

        [JsonPropertyName("data")]
        public List<DataJsonModel> Data { get; set; } = null!;

        internal sealed class DataJsonModel
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("version")]
            public string Version { get; set; } = null!;

            [JsonPropertyName("versions")]
            public DataVersionJsonModel[] Versions { get; set; } = null!;
        }

        public sealed class DataVersionJsonModel
        {
            [JsonPropertyName("version")]
            public string Version { get; set; } = null!;
        }
    }
}
