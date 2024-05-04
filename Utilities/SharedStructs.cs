namespace Hikaria.Core.Utilities;

public struct Version : IComparable<Version>, IEquatable<Version>
{
    public Version(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public string ToVersionString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public bool Equals(Version version)
    {
        return Major == version.Major && Minor == version.Minor && Patch == version.Patch;
    }

    public static bool operator ==(Version v1, Version v2)
    {
        return v1.Equals(v2);
    }

    public static bool operator !=(Version v1, Version v2)
    {
        return !v1.Equals(v2);
    }

    public static bool operator <(Version v1, Version v2)
    {
        return v1.CompareTo(v2) < 0;
    }

    public static bool operator >(Version v1, Version v2)
    {
        return v1.CompareTo(v2) > 0;
    }

    public static bool operator <=(Version v1, Version v2)
    {
        return v1.CompareTo(v2) <= 0;
    }

    public static bool operator >=(Version v1, Version v2)
    {
        return v1.CompareTo(v2) >= 0;
    }

    public int CompareTo(Version value)
    {
        if (Major != value.Major)
        {
            if (Major > value.Major)
            {
                return 1;
            }
            return -1;
        }
        else if (Minor != value.Minor)
        {
            if (Minor > value.Minor)
            {
                return 1;
            }
            return -1;
        }
        else
        {
            if (Patch == value.Patch)
            {
                return 0;
            }
            if (Patch > value.Patch)
            {
                return 1;
            }
            return -1;
        }
    }

    public int Major = 0;
    public int Minor = 0;
    public int Patch = 0;
}
