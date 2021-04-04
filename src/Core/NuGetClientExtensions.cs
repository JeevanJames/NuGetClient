using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jeevan.NuGetClient
{
    public static class NuGetClientExtensions
    {
        /// <summary>
        ///     Gets the latest version of the specified NuGet package.
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="includePrerelease">
        ///     Indicates whether to consider pre-release versions of the package when looking for the latest version.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     The NuGet package as a <see cref="Stream"/>; or <c>null</c> if the specific package was
        ///     not found at any of the sources.
        /// </returns>
        public static async Task<Stream?> GetLatestPackageAsync(this NuGetClient client, string packageId,
            bool includePrerelease = false, CancellationToken cancellationToken = default)
        {
            NuGetVersion? latestVersion = await client.GetPackageLatestVersionAsync(packageId, includePrerelease,
                cancellationToken);
            if (latestVersion is null)
                return null;

            return await client.GetPackageAsync(packageId, latestVersion.Value, cancellationToken);
        }

        /// <summary>
        ///     Gets the contents of a NuGet package for specific target framework moniker (TFM).
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        /// <param name="tfm">The target framework moniker to get the contents for.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A sequence of <see cref="PackageContent"/> objects, one for each file in the package.
        /// </returns>
        public static async IAsyncEnumerable<PackageContent> GetPackageContentsForTfmAsync(this NuGetClient client,
            string packageId, NuGetVersion version, string tfm,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.GetPackageAsync(packageId, version, cancellationToken);
            if (packageStream is null)
                yield break;

            cancellationToken.ThrowIfCancellationRequested();

            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
            IEnumerable<ZipArchiveEntry> tfmEntries = archive.Entries
                .Where(e => e.FullName.StartsWith($"lib/{tfm}/", StringComparison.OrdinalIgnoreCase));
            foreach (ZipArchiveEntry tfmEntry in tfmEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new PackageContent(tfmEntry.Name, tfmEntry.Open());
            }
        }

        /// <summary>
        ///     Gets all target framework monikers (TFMs) for a NuGet package.
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A collection of available TFMs in the specified package; an empty sequence if the
        ///     package was not found.
        /// </returns>
        public static async Task<IEnumerable<string>> GetPackageTfmsAsync(this NuGetClient client, string packageId,
            NuGetVersion version, CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.GetPackageAsync(packageId, version, cancellationToken);
            if (packageStream is null)
                return Enumerable.Empty<string>();

            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
            IEnumerable<string> tfms = archive.Entries
                .Where(e => e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    Match match = TfmPattern.Match(e.FullName);
                    return match.Success && match.Groups[1].Success ? match.Groups[1].Value : null;
                })
                .Where(tfm => tfm is not null)
                .Select(tfm => tfm!)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            return tfms;
        }

        private static readonly Regex TfmPattern = new(@"^lib/(.+)/", RegexOptions.Compiled);
    }
}
