using System.Threading.Tasks;

using Jeevan.NuGetClient.IntegrationTests.Common;

using Semver;

using Shouldly;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.NuGetClientTests
{
    public sealed class GetLatestVersion_should : NuGetSourcesBaseTests
    {
        public GetLatestVersion_should(NuGetSourcesFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData("ContentProvider", false, "0.63.0+47")]
        [InlineData("Collections.NET", false, "1.7.0")]
        public async Task Get_latest_version_for_valid_package(string packageId, bool includePrerelease,
            string version)
        {
            SemVersion? latestVersion = await Fixture.Client.GetPackageLatestVersionAsync(packageId, includePrerelease);

            latestVersion.ShouldNotBeNull();
            latestVersion!.ToString().ShouldBe(version);
        }

        [Theory]
        [InlineData("Non.Existing.Package")]
        public async Task Throw_for_non_existing_packages(string packageId)
        {
            SemVersion? latestVersion = await Fixture.Client.GetPackageLatestVersionAsync(packageId);

            latestVersion.ShouldBeNull();
        }
    }
}
