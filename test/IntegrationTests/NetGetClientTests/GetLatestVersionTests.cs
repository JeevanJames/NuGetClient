using System.Threading.Tasks;

using Jeevan.NuGetClient.IntegrationTests.Common;

using Semver;

using Shouldly;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.NetGetClientTests
{
    public sealed class GetLatestVersionTests : NuGetSourcesBaseTests
    {
        public GetLatestVersionTests(NuGetSourcesFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData("ContentProvider", false, "0.63.0+47")]
        [InlineData("Collections.NET", false, "1.7.0")]
        public async Task Should_get_latest_version_for_valid_package(string packageId, bool includePrerelease,
            string version)
        {
            SemVersion? latestVersion = await Fixture.Client.GetPackageLatestVersionAsync(packageId, includePrerelease);

            latestVersion.ShouldNotBeNull();
            latestVersion!.ToString().ShouldBe(version);
        }

        [Theory]
        [InlineData("Non.Existing.Package")]
        public async Task Should_throw_for_non_existing_packages(string packageId)
        {
            SemVersion? latestVersion = await Fixture.Client.GetPackageLatestVersionAsync(packageId);

            latestVersion.ShouldBeNull();
        }
    }
}
