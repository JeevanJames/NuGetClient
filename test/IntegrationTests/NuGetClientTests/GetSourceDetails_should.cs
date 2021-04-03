using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.NuGetClientTests
{
    public sealed class GetSourceDetails_should
    {
        [Theory]
        [InlineData("https://invalid-nuget-feed.com", typeof(HttpRequestException))]
        [InlineData("https://yahoo.com", typeof(JsonException))]
        [InlineData("https://azuresearch-usnc.nuget.org/query", typeof(InvalidOperationException))]
        public async Task Throw_for_invalid_sources(string sourceUri, Type expectedExceptionType)
        {
            var client = new NuGetClient(sourceUri);
            await Should.ThrowAsync(async () =>
                await client.GetLatestPackageVersionAsync("Serilog"), expectedExceptionType);
        }
    }
}
