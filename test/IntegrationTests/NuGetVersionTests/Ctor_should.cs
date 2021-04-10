using System;
using System.Collections;
using System.Collections.Generic;

using Semver;

using Shouldly;

using Xunit;

namespace Jeevan.NuGetClient.IntegrationTests.NuGetVersionTests
{
    public sealed class Ctor_should
    {
        [Theory]
        [InlineData("1.2.3")]
        [InlineData("1.2.3+456")]
        [InlineData("1.2.3-beta.4")]
        [InlineData("1.2.3-beta.4+567")]
        [InlineData("1.2.3.4")]
        public void Initialize_from_version_strings(string versionStr)
        {
            var versionFromCtor = new NuGetVersion(versionStr);
            NuGetVersion versionFromImplicitCast = versionStr;

            versionFromCtor.ToString().ShouldBe(versionStr);
            versionFromImplicitCast.ToString().ShouldBe(versionStr);
        }

        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData("Jeevan")]
        [InlineData("1a.2b.3c")]
        public void Throw_for_invalid_version_strings(string versionStr)
        {
            Should.Throw<ArgumentException>(() => new NuGetVersion(versionStr));
            Should.Throw<ArgumentException>(() =>
            {
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
                NuGetVersion _ = versionStr;
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
            });
        }

        [Theory]
        [MemberData(nameof(GetSemanticVersions))]
        public void Initialize_from_semver(SemVersion semver)
        {
            NuGetVersion version = semver;
            version.ToString().ShouldBe(semver.ToString());
        }

        private static IEnumerable<object[]> GetSemanticVersions()
        {
            yield return new object[] { new SemVersion(1, 2, 3) };
            yield return new object[] { new SemVersion(1, 2, 3, "beta.4") };
            yield return new object[] { new SemVersion(1, 2, 3, "beta.4", "567") };
        }

        [Theory]
        [MemberData(nameof(GetVersions))]
        public void Initialize_from_version(Version fullVer)
        {
            NuGetVersion version = fullVer;
            version.ToString().ShouldBe(fullVer.ToString());
        }

        private static IEnumerable<object[]> GetVersions()
        {
            yield return new object[] { new Version(1, 2) };
            yield return new object[] { new Version(1, 2, 3) };
            yield return new object[] { new Version(1, 2, 3, 4) };
        }
    }
}
