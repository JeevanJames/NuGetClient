// unset

using System;

namespace Jeevan.NuGetClient.Internals
{
    /// <summary>
    ///     Cached information about a NuGet source, so we don't need to retrieve it multiple times
    ///     during the lifetime of the client.
    /// </summary>
    internal sealed class SourceDetail
    {
        internal Uri PackageBaseUri { get; set; } = null!;

        internal Uri SearchQueryServiceUri { get; set; } = null!;
    }
}
