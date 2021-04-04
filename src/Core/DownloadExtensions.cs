using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jeevan.NuGetClient
{
    /// <summary>
    ///     Extensions on <see cref="NuGetClient"/> specifically for downloading package or package
    ///     contents to the file system.
    /// </summary>
    public static class DownloadExtensions
    {
        public static async Task<FileInfo?> DownloadPackageAsync(this NuGetClient client, string packageId,
            NuGetVersion version, string directory, string? fileName = null, bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.GetPackageAsync(packageId, version, cancellationToken);
            if (packageStream is null)
                return null;

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{packageId}.{version}.nupkg";
            string filePath = Path.Combine(directory, fileName);

            await using var fileStream = new FileStream(filePath, overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write, FileShare.Read);
            if (packageStream.CanSeek)
                packageStream.Seek(0, SeekOrigin.Begin);
            await packageStream.CopyToAsync(fileStream, cancellationToken);

            return new FileInfo(filePath);
        }

        public static async Task<FileInfo?> DownloadLatestPackageAsync(this NuGetClient client, string packageId,
            string directory, string? fileName = null, bool overwrite = false, bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            NuGetVersion? latestVersion = await client.GetPackageLatestVersionAsync(packageId, includePrerelease,
                cancellationToken);
            if (latestVersion is null)
                return null;

            return await client.DownloadPackageAsync(packageId, latestVersion.Value, directory, fileName, overwrite,
                cancellationToken);
        }

        public static async Task DownloadPackageContentsForTfmAsync(this NuGetClient client, string packageId,
            NuGetVersion version, string tfm, string downloadDirectory)
        {
            if (!Directory.Exists(downloadDirectory))
                Directory.CreateDirectory(downloadDirectory);

            await foreach (PackageContent content in client.GetPackageContentsForTfmAsync(packageId, version, tfm))
            {
                string filePath = Path.Combine(downloadDirectory, content.Name);
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                    FileShare.Read);
                if (content.Stream.CanSeek)
                    content.Stream.Seek(0, SeekOrigin.Begin);
                await content.Stream.CopyToAsync(fileStream);
            }
        }
    }
}
