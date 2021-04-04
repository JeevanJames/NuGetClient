using System.Collections.Generic;
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
        /// <summary>
        ///     Downloads a specific version of a NuGet package to a directory.
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        /// <param name="directory">The directory to save the NuGet package to.</param>
        /// <param name="fileName">
        ///     Optional file name for the downloaded package. If not specified, then the name is
        ///     <c>[package id].[version].nupkg</c>.
        /// </param>
        /// <param name="overwrite">
        ///     Indicates whether to overwrite the package file, if it already exists (default: false).
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A <see cref="FileInfo"/> instance denoting the downloaded file; <c>null</c> if the
        ///     package could not be downloaded and saved.
        /// </returns>
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

        /// <summary>
        ///     Downloads the latest version of a NuGet package to a directory.
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="directory">The directory to save the NuGet package to.</param>
        /// <param name="fileName">
        ///     Optional file name for the downloaded package. If not specified, then the name is
        ///     <c>[package id].[version].nupkg</c>.
        /// </param>
        /// <param name="overwrite">
        ///     Indicates whether to overwrite the package file, if it already exists (default: false).
        /// </param>
        /// <param name="includePrerelease">
        ///     Indicates whether to consider pre-release versions of the package when calculating the
        ///     version of the latest package.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A <see cref="FileInfo"/> instance denoting the downloaded file; <c>null</c> if the
        ///     package could not be downloaded and saved.
        /// </returns>
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

        /// <summary>
        ///     Downloads the contents of a specific target framework moniker (TFM) folder in a NuGet
        ///     package to a directory.
        /// </summary>
        /// <param name="client">The <see cref="NuGetClient"/> instance.</param>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        /// <param name="tfm">The target framework moniker (TFM) to download the contents for.</param>
        /// <param name="directory">The directory to save the contents to.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A collection of <see cref="FileInfo"/> instances, one for each file downloaded.
        /// </returns>
        public static async Task<IReadOnlyList<FileInfo>> DownloadPackageContentsForTfmAsync(this NuGetClient client,
            string packageId, NuGetVersion version, string tfm, string directory,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var downloadedFiles = new List<FileInfo>();
            await foreach (PackageContent content in client.GetPackageContentsForTfmAsync(packageId, version, tfm,
                cancellationToken))
            {
                string filePath = Path.Combine(directory, content.Name);
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                    FileShare.Read);
                if (content.Stream.CanSeek)
                    content.Stream.Seek(0, SeekOrigin.Begin);
                await content.Stream.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
                downloadedFiles.Add(new FileInfo(filePath));
            }

            return downloadedFiles;
        }
    }
}
