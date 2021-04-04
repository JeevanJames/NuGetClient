using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jeevan.NuGetClient
{
    public static class NuGetClientExtensions
    {
        public static async Task<Stream?> GetLatestPackageAsync(this NuGetClient client, string packageId,
            bool includePrerelease = false, CancellationToken cancellationToken = default)
        {
            NuGetVersion? latestVersion = await client.GetPackageLatestVersionAsync(packageId, includePrerelease,
                cancellationToken);
            if (latestVersion is null)
                return null;

            return await client.GetPackageAsync(packageId, latestVersion.Value, cancellationToken);
        }

        public static async IAsyncEnumerable<TfmContent> GetPackageContentsForTfmAsync(this NuGetClient client,
            string packageId, NuGetVersion version, string tfm)
        {
            Stream? packageStream = await client.GetPackageAsync(packageId, version);
            if (packageStream is null)
                yield break;

            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
            IEnumerable<ZipArchiveEntry> tfmEntries = archive.Entries
                .Where(e => e.FullName.StartsWith($"lib/{tfm}/", StringComparison.OrdinalIgnoreCase));
            foreach (ZipArchiveEntry tfmEntry in tfmEntries)
                yield return new TfmContent(tfmEntry.Name, tfmEntry.Open());
        }

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
