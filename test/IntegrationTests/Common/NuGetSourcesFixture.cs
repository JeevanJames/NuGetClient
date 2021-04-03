using System;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.Common
{
    public sealed class NuGetSourcesFixture
    {
        public NuGetClient Client { get; } = new(
            "https://www.myget.org/F/jeevanjames/api/v3/index.json",
            "https://api.nuget.org/v3/index.json");
    }

    [CollectionDefinition(nameof(NuGetSourcesFixture))]
    public sealed class NuGetSourcesFixtureCollection : ICollectionFixture<NuGetSourcesFixture>
    {
    }

    [Collection(nameof(NuGetSourcesFixture))]
    public abstract class NuGetSourcesBaseTests
    {
        protected NuGetSourcesBaseTests(NuGetSourcesFixture fixture)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }

        protected NuGetSourcesFixture Fixture { get; }
    }
}
