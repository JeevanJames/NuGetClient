using System.IO;
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
    }
}
