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

using Semver;

namespace Jeevan.NuGetClient
{
    public sealed class NuGetClient : IDisposable
    {
        private readonly string[] _sources;
        private readonly Dictionary<string, SourceDetail> _sourceCaches;
        private readonly HttpClient _client;

        public NuGetClient()
            : this("https://api.nuget.org/v3/index.json")
        {
        }

        public NuGetClient(params string[] sources)
        {
            if (sources is null)
                throw new ArgumentNullException(nameof(sources));
            if (sources.Length == 0)
            {
                throw new ArgumentException("No NuGet sources specified. Specify at least one source.",
                    nameof(sources));
            }

            _sources = sources;
            _sourceCaches = new Dictionary<string, SourceDetail>(_sources.Length, StringComparer.OrdinalIgnoreCase);
            _client = HttpClientFactory.Create();
        }

        public IReadOnlyList<string> Sources => _sources;

        public async Task<IReadOnlyList<SemVersion>> GetPackageVersionsAsync(string packageName,
            bool includePrerelease = false, CancellationToken cancellationToken = default)
        {
            (bool success, IEnumerable<SemVersion>? result) = await ForEachSource(async (_, sourceDetail) =>
            {
                string queryString = $"?q=PackageId:{packageName}&prerelease={includePrerelease}";
                var queryUri = new Uri(sourceDetail.SearchQueryServiceUri, queryString);

                Stream responseStream = await _client.GetStreamAsync(queryUri);
                SearchQueryResponseJsonModel? response = await JsonSerializer.DeserializeAsync<SearchQueryResponseJsonModel>(
                    responseStream, cancellationToken: cancellationToken);
                if (response is null || response.TotalHits == 0)
                    return (false, default);
                if (response.Data.Count == 0 || response.Data[0].Versions.Length == 0)
                    return (false, default);
                return (true, response.Data[0].Versions.Select(v => SemVersion.Parse(v.Version)));
            }, cancellationToken);

            return success && result is not null
                ? result.OrderByDescending(v => v).ToList()
                : Array.Empty<SemVersion>();
        }

        public async Task<SemVersion?> GetLatestPackageVersionAsync(string packageId, bool includePrerelease = false,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<SemVersion> versions = await GetPackageVersionsAsync(packageId, includePrerelease,
                cancellationToken);
            return versions.FirstOrDefault();
        }

        public async Task<Stream?> DownloadPackageAsync(string packageName, SemVersion version,
            CancellationToken cancellationToken = default)
        {
            (_, Stream? result) = await ForEachSource(async (_, sourceDetail) =>
            {
                string id = packageName.ToLowerInvariant();
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
            foreach (string source in _sources)
            {
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
                    return new { Resource = r, Version = SemVersion.Parse(version) };
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
    }

    internal delegate Task<(bool Success, T? Result)> SourceOperation<T>(string source, SourceDetail sourceDetail);
}
