using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Semver;

namespace Jeevan.NuGetClient
{
    public static class NuGetClientExtensions
    {
        public static async Task<string?> DownloadPackageToFileAsync(this NuGetClient client, string packageId, SemVersion version,
            string filePath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.DownloadPackageAsync(packageId, version, cancellationToken);
            if (packageStream is null)
                return null;

            await using var fileStream = new FileStream(filePath, overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write, FileShare.Read);
            packageStream.Seek(0, SeekOrigin.Begin);
            await packageStream.CopyToAsync(fileStream, cancellationToken);

            return Path.GetFullPath(filePath);
        }

        public static async IAsyncEnumerable<TfmContent> GetPackageContentsForTfmAsync(this NuGetClient client,
            string packageId, SemVersion version, string tfm)
        {
            Stream? packageStream = await client.DownloadPackageAsync(packageId, version);
            if (packageStream is null)
                yield break;

            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
            IEnumerable<ZipArchiveEntry> tfmEntries = archive.Entries
                .Where(e => e.FullName.StartsWith($"lib/{tfm}/", StringComparison.OrdinalIgnoreCase));
            foreach (ZipArchiveEntry tfmEntry in tfmEntries)
                yield return new TfmContent(tfmEntry.Name, tfmEntry.Open());
        }

        public static async Task DownloadPackageContentsForTfmAsync(this NuGetClient client, string packageId,
            SemVersion version, string tfm, string downloadDirectory)
        {
            if (!Directory.Exists(downloadDirectory))
                Directory.CreateDirectory(downloadDirectory);

            await foreach (TfmContent content in client.GetPackageContentsForTfmAsync(packageId, version, tfm))
            {
                string filePath = Path.Combine(downloadDirectory, content.Name);
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                    FileShare.Read);
                if (content.Stream.CanSeek)
                    content.Stream.Seek(0, SeekOrigin.Begin);
                await content.Stream.CopyToAsync(fileStream);
            }
        }

        public static async Task<IEnumerable<string>> GetPackageTfmsAsync(this NuGetClient client, string packageId,
            SemVersion version, CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.DownloadPackageAsync(packageId, version, cancellationToken);
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

    public sealed class TfmContent
    {
        internal TfmContent(string name, Stream stream)
        {
            Name = name;
            Stream = stream;
        }

        public string Name { get; }

        public Stream Stream { get; }
    }
}
