// unset

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jeevan.NuGetClient.JsonModels
{
    internal sealed class SourceJsonModel
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = null!;

        [JsonPropertyName("resources")]
        public List<ResourceJsonModel> Resources { get; set; } = null!;

        internal sealed class ResourceJsonModel
        {
            [JsonPropertyName("@id")]
            public string Id { get; set; } = null!;

            [JsonPropertyName("@type")]
            public string Type { get; set; } = null!;

            [JsonPropertyName("comment")]
            public string? Comment { get; set; }
        }
    }
}
