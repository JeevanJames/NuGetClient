using System;

using Semver;

namespace Jeevan.NuGetClient
{
    public readonly struct NuGetVersion : IEquatable<NuGetVersion>, IComparable<NuGetVersion>
    {
        private readonly SemVersion? _semver;
        private readonly Version? _fullVer;

        public NuGetVersion(string version)
        {
            if (version is null)
                throw new ArgumentNullException(nameof(version));

            if (SemVersion.TryParse(version, out SemVersion semver))
            {
                _semver = semver;
                _fullVer = null;
            }
            else if (Version.TryParse(version, out Version fullVer))
            {
                _fullVer = fullVer;
                _semver = null;
            }
            else
                throw new ArgumentException("Invalid version specified.", nameof(version));
        }

        internal NuGetVersion(SemVersion semver)
        {
            _semver = semver ?? throw new ArgumentNullException(nameof(semver));
            _fullVer = null;
        }

        internal NuGetVersion(Version fullVer)
        {
            _fullVer = fullVer ?? throw new ArgumentNullException(nameof(fullVer));
            _semver = null;
        }

        public bool Equals(NuGetVersion other)
        {
            return Equals(_semver, other._semver) && Equals(_fullVer, other._fullVer);
        }

        public override bool Equals(object? obj)
        {
            return obj is NuGetVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_semver, _fullVer);
        }

        public override string ToString()
        {
            return _semver is not null ? _semver.ToString() : _fullVer!.ToString();
        }

        public int CompareTo(NuGetVersion other)
        {
            if (_semver is not null && other._semver is not null)
                return _semver.CompareTo(other._semver);
            if (_fullVer is not null && other._fullVer is not null)
                return _fullVer.CompareTo(other._fullVer);

            SemVersion thisSemver = _semver ?? new SemVersion(_fullVer!.Major, _fullVer.Minor, _fullVer.Revision);
            SemVersion otherSemver = other._semver
                ?? new SemVersion(other._fullVer!.Major, other._fullVer.Minor, other._fullVer.Revision);
            return thisSemver.CompareTo(otherSemver);
        }

        // ReSharper disable ArrangeMethodOrOperatorBody

        public static bool operator ==(NuGetVersion left, NuGetVersion right) => left.Equals(right);

        public static bool operator !=(NuGetVersion left, NuGetVersion right) => !left.Equals(right);

        public static implicit operator NuGetVersion(string version) => new(version);

        public static implicit operator string(NuGetVersion version) => version.ToString();

        public static implicit operator NuGetVersion(SemVersion semver) => new(semver);

        public static implicit operator SemVersion?(NuGetVersion version) => version._semver;

        public static implicit operator NuGetVersion(Version fullVer) => new(fullVer);

        public static implicit operator Version?(NuGetVersion version) => version._fullVer;
    }
}
