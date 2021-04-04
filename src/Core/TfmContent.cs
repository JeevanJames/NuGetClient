using System.IO;

namespace Jeevan.NuGetClient
{
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
