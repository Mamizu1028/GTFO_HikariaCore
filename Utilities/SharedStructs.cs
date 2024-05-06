using System.Text.RegularExpressions;

namespace Hikaria.Core;

public struct Version : IComparable<Version>, IComparable, IEquatable<Version>
{
    private readonly int _major = 0;

    private readonly int _minor = 0;

    private readonly int _patch = 0;

    private static Regex strictRegex = new Regex("^\n            \\s*v?\n            ([0-9]|[1-9][0-9]+)       # major version\n            \\.\n            ([0-9]|[1-9][0-9]+)       # minor version\n            \\.\n            ([0-9]|[1-9][0-9]+)       # patch version\n            (\\-([0-9A-Za-z\\-\\.]+))?   # pre-release version\n            (\\+([0-9A-Za-z\\-\\.]+))?   # build metadata\n            \\s*\n            $", RegexOptions.IgnorePatternWhitespace);

    private static Regex looseRegex = new Regex("^\n            [v=\\s]*\n            (\\d+)                     # major version\n            \\.\n            (\\d+)                     # minor version\n            \\.\n            (\\d+)                     # patch version\n            (\\-?([0-9A-Za-z\\-\\.]+))?  # pre-release version\n            (\\+([0-9A-Za-z\\-\\.]+))?   # build metadata\n            \\s*\n            $", RegexOptions.IgnorePatternWhitespace);

    public int Major => _major;

    public int Minor => _minor;

    public int Patch => _patch;

    public Version(string input, bool loose = false)
    {
        Match match = (loose ? looseRegex : strictRegex).Match(input);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid version string: {input}");
        }

        _major = int.Parse(match.Groups[1].Value);
        _minor = int.Parse(match.Groups[2].Value);
        _patch = int.Parse(match.Groups[3].Value);
    }

    public Version(int major, int minor, int patch)
    {
        _major = major;
        _minor = minor;
        _patch = patch;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public override int GetHashCode()
    {
        int num = 17;
        num = num * 23 + Major.GetHashCode();
        num = num * 23 + Minor.GetHashCode();
        num = num * 23 + Patch.GetHashCode();

        return num;
    }

    public bool Equals(Version other)
    {
        return CompareTo(other) == 0;
    }

    public int CompareTo(object obj)
    {
        if (obj != null)
        {
            if (obj is Version other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException("Object is not a Version");
        }

        return 1;
    }

    public int CompareTo(Version other)
    {
        foreach (int item in PartComparisons(other))
        {
            if (item != 0)
            {
                return item;
            }
        }

        return 0;
    }

    private IEnumerable<int> PartComparisons(Version other)
    {
        yield return Major.CompareTo(other.Major);
        yield return Minor.CompareTo(other.Minor);
        yield return Patch.CompareTo(other.Patch);
    }

    public override bool Equals(object other)
    {
        if (other != null)
        {
            if (other is Version ver)
            {
                return Equals(ver);
            }

            throw new ArgumentException("Object is not a Version");
        }
        return false;
    }

    public static Version Parse(string input, bool loose = false)
    {
        return new Version(input, loose);
    }

    public static bool TryParse(string input, out Version result)
    {
        return TryParse(input, loose: false, out result);
    }

    public static bool TryParse(string input, bool loose, out Version result)
    {
        try
        {
            result = Parse(input, loose);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    public static bool operator ==(Version a, Version b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Version a, Version b)
    {
        return !(a == b);
    }

    public static bool operator >(Version a, Version b)
    {
        return a.CompareTo(b) > 0;
    }

    public static bool operator >=(Version a, Version b)
    {
        return a.CompareTo(b) >= 0;
    }

    public static bool operator <(Version a, Version b)
    {
        return a.CompareTo(b) < 0;
    }

    public static bool operator <=(Version a, Version b)
    {
        return a.CompareTo(b) <= 0;
    }
}