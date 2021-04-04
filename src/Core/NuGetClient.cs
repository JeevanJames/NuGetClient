using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Jeevan.NuGetClient.Internals;
using Jeevan.NuGetClient.JsonModels;

namespace Jeevan.NuGetClient
{
    /// <summary>
    ///     Client for the NuGet API.
    /// </summary>
    public sealed class NuGetClient : IDisposable
    {
        private readonly HttpClient _client;

        /// <summary>
        ///     A cache of essential details for each source. This is initialized for each source
        ///     whenever the source is accessed for the first time.
        /// </summary>
        /// <seealso cref="GetSourceDetails"/>
        private readonly Dictionary<string, SourceDetail> _sourceCaches;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NuGetClient"/> class, with the nuget.org
        ///     source.
        /// </summary>
        public NuGetClient()
            : this("https://api.nuget.org/v3/index.json")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="NuGetClient"/> class with the specified
        ///     <paramref name="sources"/>.
        /// </summary>
        /// <param name="sources">
        ///     One or more NuGet sources that this instance will use to interact with the API.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <paramref name="sources"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown if no sources are specified.</exception>
        public NuGetClient(params string[] sources)
        {
            if (sources is null)
                throw new ArgumentNullException(nameof(sources));
            if (sources.Length == 0)
            {
                throw new ArgumentException("No NuGet sources specified. Specify at least one source.",
                    nameof(sources));
            }

            Sources = sources;
            _client = HttpClientFactory.Create();
            _sourceCaches = new Dictionary<string, SourceDetail>(Sources.Count, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Gets the list of NuGet sources that this instance will query for any operation.
        /// </summary>
        public IReadOnlyList<string> Sources { get; }

        /// <summary>
        ///     Gets the specific version of a NuGet package (.nupkg file) as a <see cref="Stream"/>.
        /// </summary>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="version">The version of the NuGet package.</param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     The NuGet package as a <see cref="Stream"/>; or <c>null</c> if the specific package was
        ///     not found at any of the sources.
        /// </returns>
        public async Task<Stream?> GetPackageAsync(string packageId, NuGetVersion version,
            CancellationToken cancellationToken = default)
        {
            (_, Stream? result) = await ForEachSource(async (_, sourceDetail) =>
            {
                string id = packageId.ToLowerInvariant();
                string ver = version.ToString().ToLowerInvariant();
                string relativeUrl = $"{id}/{ver}/{id}.{ver}.nupkg";
                var packageUri = new Uri(sourceDetail.PackageBaseUri, relativeUrl);

                HttpResponseMessage response = await _client.GetAsync(packageUri, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return (false, default);
                Stream packageStream = await response.Content.ReadAsStreamAsync();
                return (true, packageStream);
            }, cancellationToken);

            return result;
        }

        /// <summary>
        ///     Gets all available versions of a NuGet package.
        /// </summary>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="includePrerelease">
        ///     Indicates whether to include pre-release versions of the package.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     A collection of all available versions of the package, in descending order. If the
        ///     package is not found, an empty collection is returned.
        /// </returns>
        public async Task<IReadOnlyList<NuGetVersion>> GetPackageVersionsAsync(string packageId,
            bool includePrerelease = false, CancellationToken cancellationToken = default)
        {
            (bool success, IEnumerable<NuGetVersion>? result) = await ForEachSource(async (_, sourceDetail) =>
            {
                string queryString = $"?q=PackageId:{packageId}&prerelease={includePrerelease}";
                var queryUri = new Uri(sourceDetail.SearchQueryServiceUri, queryString);

                Stream responseStream = await _client.GetStreamAsync(queryUri);
                SearchQueryResponseJsonModel? response = await JsonSerializer
                    .DeserializeAsync<SearchQueryResponseJsonModel>(responseStream,
                        cancellationToken: cancellationToken);
                if (response is null || response.TotalHits == 0)
                    return (false, default);
                if (response.Data.Count == 0 || response.Data[0].Versions.Length == 0)
                    return (false, default);
                return (true, response.Data[0].Versions.Select(v => new NuGetVersion(v.Version)));
            }, cancellationToken);

            return success && result is not null
                ? result.OrderByDescending(v => v).ToList()
                : Array.Empty<NuGetVersion>();
        }

        /// <summary>
        ///     Gets the latest version of a NuGet package.
        /// </summary>
        /// <param name="packageId">The ID of the NuGet package.</param>
        /// <param name="includePrerelease">
        ///     Indicates whether to consider pre-release versions of the package.
        /// </param>
        /// <param name="cancellationToken">
        ///     A cancellation token that can be used by other objects or threads to receive notice of
        ///     cancellation.
        /// </param>
        /// <returns>
        ///     The latest version of the specified NuGet package; <c>null</c> if the package was not found.
        /// </returns>
        public async Task<NuGetVersion?> GetPackageLatestVersionAsync(string packageId, bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<NuGetVersion> versions = await GetPackageVersionsAsync(packageId, includePrerelease,
                cancellationToken);
            return versions.FirstOrDefault();
        }

        /// <summary>
        ///     Helper method to run some logic over each NuGet source in sequence until a successful
        ///     result is achieved. This method also retrieves and caches source details, if it is not
        ///     already cached.
        /// </summary>
        /// <typeparam name="T">The type of the result expected by the caller.</typeparam>
        /// <param name="operation">A delegate the specifies the action to run over each source.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>
        ///     A tuple of a <see cref="bool"/> indicating whether a successful result was achieved, and
        ///     the result itself (if successful) or the default value (if unsuccessful).
        /// </returns>
        private async Task<(bool Success, T? Result)> ForEachSource<T>(SourceOperation<T> operation,
            CancellationToken cancellationToken)
        {
            foreach (string source in Sources)
            {
                // Look for a cached version of the source details. If one doesn't exist, create one
                // from the source.
                if (!_sourceCaches.TryGetValue(source, out SourceDetail? sourceDetail))
                {
                    sourceDetail = await GetSourceDetails(source, cancellationToken);
                    _sourceCaches.Add(source, sourceDetail);
                }

                (bool Success, T? Result) result = await operation(source, sourceDetail);
                if (result.Success)
                    return result;
            }

            return (false, default);
        }

        private async Task<SourceDetail> GetSourceDetails(string source, CancellationToken cancellationToken)
        {
            Stream sourceStream = await _client.GetStreamAsync(source);
            SourceJsonModel? sourceJsonModel = await JsonSerializer.DeserializeAsync<SourceJsonModel>(sourceStream,
                cancellationToken: cancellationToken);
            if (sourceJsonModel?.Resources is null)
                throw new InvalidOperationException($"Error parsing source '{source}'.");

            // PackageBaseAddress
            string? packageBaseUrl = sourceJsonModel.Resources.Find(
                r => r.Type.Equals("PackageBaseAddress/3.0.0", StringComparison.Ordinal))?.Id;
            if (packageBaseUrl is null)
                throw new InvalidOperationException($"Package base URL not specified for source '{source}'.");
            if (!Uri.TryCreate(packageBaseUrl, UriKind.Absolute, out Uri? packageBaseUri))
                throw new InvalidOperationException($"Package base URL for source '{source}' is not a valid URL.");

            // SearchQueryService
            var searchQueryServiceResource = sourceJsonModel.Resources
                .Where(r => SearchQueryServicePattern.IsMatch(r.Type))
                .Select(r =>
                {
                    Match match = SearchQueryServicePattern.Match(r.Type);
                    string version = match.Groups[1].Success ? match.Groups[1].Value : "0.1.0";
                    return new { Resource = r, Version = new NuGetVersion(version) };
                })
                .OrderByDescending(r => r.Version)
                .FirstOrDefault();
            if (searchQueryServiceResource is null)
                throw new InvalidOperationException($"Cannot find search query service for source '{source}'.");

            return new SourceDetail
            {
                PackageBaseUri = packageBaseUri,
                SearchQueryServiceUri = new Uri(searchQueryServiceResource.Resource.Id, UriKind.Absolute),
            };
        }

        private static readonly Regex SearchQueryServicePattern = new("^SearchQueryService(?:/(.+))?", RegexOptions.Compiled);

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _client.Dispose();
        }

        internal delegate Task<(bool Success, T? Result)> SourceOperation<T>(string source, SourceDetail sourceDetail);
    }
}
