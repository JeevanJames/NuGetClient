using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Jeevan.NuGetClient
{
    public static class DownloadExtensions
    {
        public static async Task<string?> DownloadPackageAsync(this NuGetClient client, string packageId,
            NuGetVersion version, string filePath, bool overwrite = false,
            CancellationToken cancellationToken = default)
        {
            Stream? packageStream = await client.GetPackageAsync(packageId, version, cancellationToken);
            if (packageStream is null)
                return null;

            await using var fileStream = new FileStream(filePath, overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write, FileShare.Read);
            packageStream.Seek(0, SeekOrigin.Begin);
            await packageStream.CopyToAsync(fileStream, cancellationToken);

            return Path.GetFullPath(filePath);
        }

        public static async Task DownloadPackageContentsForTfmAsync(this NuGetClient client, string packageId,
            NuGetVersion version, string tfm, string downloadDirectory)
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
    }
}
