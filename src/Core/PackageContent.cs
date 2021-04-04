using System;
using System.IO;

namespace Jeevan.NuGetClient
{
    public sealed class PackageContent
    {
        internal PackageContent(string name, Stream stream)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        ///     Gets the name of the content in the package.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets a <see cref="Stream"/> that represents the contents.
        /// </summary>
        public Stream Stream { get; }
    }
}
