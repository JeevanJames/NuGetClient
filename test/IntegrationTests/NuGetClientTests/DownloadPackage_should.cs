using System.IO;
using System.Threading.Tasks;

using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;

using Jeevan.NuGetClient.IntegrationTests.Common;

using Shouldly;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.NuGetClientTests
{
    public sealed class DownloadPackage_should : NuGetSourcesBaseTests
    {
        public DownloadPackage_should(NuGetSourcesFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        [UseReporter(typeof(XUnit2Reporter))]
        public async Task Download_a_nuget_package()
        {
            Stream? packageStream = await Fixture.Client.GetPackageAsync("Collections.NET", "1.7.0");
            packageStream.ShouldNotBeNull();

            await using var ms = new MemoryStream();
            packageStream.Seek(0, SeekOrigin.Begin);
            await packageStream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            Approvals.VerifyBinaryFile(ms.ToArray(), ".nupkg.zip");
        }
    }
}
