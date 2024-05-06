namespace Hikaria.Core;

public class Version : SemanticVersioning.Version
{
    public Version(string input, bool loose = false) : base(input, loose)
    {
    }

    public Version(int major, int minor, int patch, string preRelease = null, string build = null) : base(major, minor, patch, preRelease, build)
    {
    }
}
