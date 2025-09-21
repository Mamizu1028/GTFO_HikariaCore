using Hikaria.Core.Managers;
using SNetwork;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Hikaria.Core;

#region Popup
public struct pPopupMessage
{
    public PopupMessage UnpackPopupMessage()
    {
        return new()
        {
            BlinkInContent = BlinkInContent,
            BlinkTimeInterval = BlinkTimeInterval,
            Header = Header,
            UpperText = UpperText,
            LowerText = LowerText,
            PopupType = PopupType,
            OnCloseCallback = PopupMessageManager.EmptyAction
        };
    }

    public pPopupMessage(string header, string upperText, string lowerText, bool blinkInContent = true, float blinkTimeInterval = 0.2f, PopupType type = PopupType.BoosterImplantMissed)
    {
        Header = header;
        UpperText = upperText;
        LowerText = lowerText;
        BlinkInContent = blinkInContent;
        BlinkTimeInterval = blinkTimeInterval;
        PopupType = type;
    }

    public bool BlinkInContent = true;
    public float BlinkTimeInterval = 0.2f;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Header;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string UpperText;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string LowerText;
    public PopupType PopupType = PopupType.BoosterImplantMissed;
}
#endregion

#region ModList ModInfo
public struct pModList : SNetworkExt.IReplicatedPlayerData
{
    public pModList(SNet_Player player, List<pModInfo> modList)
    {
        Array.Fill(Mods, new());
        PlayerData.SetPlayer(player);
        ModCount = Math.Clamp(modList.Count, 0, MOD_SYNC_COUNT);
        for (int i = 0; i < ModCount; i++)
        {
            Mods[i] = modList[i];
        }
    }

    public pModList()
    {
        Array.Fill(Mods, new());
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = MOD_SYNC_COUNT)]
    public pModInfo[] Mods = new pModInfo[MOD_SYNC_COUNT];

    public int ModCount = 0;

    public const int MOD_SYNC_COUNT = 256; // 到底是什么神人会装上百个mod???

    public SNetStructs.pPlayer PlayerData { get; set; } = new();
}

public struct pModInfo
{
    public pModInfo(string name, string guid, Version version)
    {
        Name = name;
        GUID = guid;
        Version = version;
    }

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string Name = string.Empty;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string GUID = string.Empty;
    public Version Version = default;
}
#endregion

#region Version VersionRange
/// <summary>
/// 表示符合语义化版本规范的版本号。
/// </summary>
/// <remarks>
/// 版本号由三部分组成：主版本号.次版本号.修订号 (例如 1.0.0)。
/// 主版本号：当你做了不兼容的 API 修改时递增。
/// 次版本号：当你做了向下兼容的功能性新增时递增。
/// 修订号：当你做了向下兼容的问题修正时递增。
/// </remarks>
public readonly struct Version : IComparable<Version>, IComparable, IEquatable<Version>
{
    public static readonly Version ZeroVersion = new Version(0, 0, 0);

    /// <summary>
    /// 用于解析版本号的正则表达式。
    /// 仅匹配形如 "0.0.0" 的简单版本号格式。
    /// </summary>
    private static readonly Regex strictRegex = new Regex(@"^
            \s*
            ([0-9]+)       # major version
            \.
            ([0-9]+)       # minor version
            \.
            ([0-9]+)       # patch version
            \s*
            $", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    /// <summary>
    /// 主版本号。
    /// </summary>
    private readonly int _major = 0;

    /// <summary>
    /// 次版本号。
    /// </summary>
    private readonly int _minor = 0;

    /// <summary>
    /// 修订号。
    /// </summary>
    private readonly int _patch = 0;

    /// <summary>
    /// 获取主版本号。当进行不兼容的 API 更改时递增。
    /// </summary>
    public int Major => _major;

    /// <summary>
    /// 获取次版本号。当添加向下兼容的功能时递增。
    /// </summary>
    public int Minor => _minor;

    /// <summary>
    /// 获取修订号。当进行向下兼容的错误修复时递增。
    /// </summary>
    public int Patch => _patch;

    /// <summary>
    /// 使用版本字符串初始化 <see cref="Version"/> 结构的新实例。
    /// </summary>
    /// <param name="input">表示版本的字符串，必须是 "X.Y.Z" 格式，其中 X、Y、Z 为非负整数。</param>
    /// <param name="loose">指示是否使用宽松模式解析版本字符串。</param>
    /// <exception cref="ArgumentException">当版本字符串格式无效时抛出。</exception>
    public Version(string input)
    {
        Match match = strictRegex.Match(input);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid version string: {input}. Version must be in format 'X.Y.Z' where X, Y, Z are non-negative integers.");
        }

        _major = int.Parse(match.Groups[1].Value);
        _minor = int.Parse(match.Groups[2].Value);
        _patch = int.Parse(match.Groups[3].Value);
    }

    /// <summary>
    /// 使用指定的主版本号、次版本号和修订号初始化 <see cref="Version"/> 结构的新实例。
    /// </summary>
    /// <param name="major">主版本号。</param>
    /// <param name="minor">次版本号。</param>
    /// <param name="patch">修订号。</param>
    public Version(int major, int minor, int patch)
    {
        _major = major;
        _minor = minor;
        _patch = patch;
    }

    /// <summary>
    /// 返回表示当前版本的字符串。
    /// </summary>
    /// <returns>表示当前版本的字符串，格式为 "Major.Minor.Patch"。</returns>
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    /// <summary>
    /// 返回此实例的哈希代码。
    /// </summary>
    /// <returns>32 位有符号整数哈希代码。</returns>
    public override int GetHashCode()
    {
        int num = 17;
        num = num * 23 + Major.GetHashCode();
        num = num * 23 + Minor.GetHashCode();
        num = num * 23 + Patch.GetHashCode();

        return num;
    }

    /// <summary>
    /// 确定指定的 <see cref="Version"/> 是否等于当前 <see cref="Version"/>。
    /// </summary>
    /// <param name="other">要与当前版本比较的版本。</param>
    /// <returns>如果指定的版本等于当前版本，则为 true；否则为 false。</returns>
    public bool Equals(Version other)
    {
        return CompareTo(other) == 0;
    }

    /// <summary>
    /// 将当前版本与另一个对象进行比较，并返回一个整数，该整数指示当前版本是小于、等于还是大于另一个对象。
    /// </summary>
    /// <param name="obj">要比较的对象。</param>
    /// <returns>
    /// 一个值，指示要比较的对象的相对顺序。
    /// 如果小于零，则当前版本小于 obj。
    /// 如果为零，则当前版本等于 obj。
    /// 如果大于零，则当前版本大于 obj 或 obj 为 null。
    /// </returns>
    /// <exception cref="ArgumentException">obj 不是 <see cref="Version"/>。</exception>
    public int CompareTo(object obj)
    {
        if (obj == null)
            return 1;

        if (obj is Version other)
            return CompareTo(other);

        throw new ArgumentException("Object is not a Version");
    }

    /// <summary>
    /// 将当前版本与另一个版本进行比较，并返回一个整数，该整数指示当前版本是小于、等于还是大于另一个版本。
    /// </summary>
    /// <param name="other">要比较的版本。</param>
    /// <returns>
    /// 一个值，指示要比较的版本的相对顺序。
    /// 如果小于零，则当前版本小于 other。
    /// 如果为零，则当前版本等于 other。
    /// 如果大于零，则当前版本大于 other。
    /// </returns>
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

    /// <summary>
    /// 生成当前版本与另一个版本各部分的比较结果。
    /// </summary>
    /// <param name="other">要比较的版本。</param>
    /// <returns>包含各部分比较结果的枚举。</returns>
    private IEnumerable<int> PartComparisons(Version other)
    {
        yield return Major.CompareTo(other.Major);
        yield return Minor.CompareTo(other.Minor);
        yield return Patch.CompareTo(other.Patch);
    }

    /// <summary>
    /// 确定指定的对象是否等于当前版本。
    /// </summary>
    /// <param name="other">要与当前版本比较的对象。</param>
    /// <returns>如果指定的对象等于当前版本，则为 true；否则为 false。</returns>
    public override bool Equals(object other)
    {
        if (other == null)
            return false;

        if (other is Version ver)
            return Equals(ver);

        return false;
    }

    /// <summary>
    /// 从版本字符串创建新的 <see cref="Version"/> 实例。
    /// </summary>
    /// <param name="input">表示版本的字符串，必须是 "X.Y.Z" 格式，其中 X、Y、Z 为非负整数。</param>
    /// <returns>表示指定版本的 <see cref="Version"/> 实例。</returns>
    /// <exception cref="ArgumentException">当版本字符串格式无效时抛出。</exception>
    public static Version Parse(string input)
    {
        return new Version(input);
    }

    /// <summary>
    /// 尝试将版本字符串转换为 <see cref="Version"/> 实例。
    /// </summary>
    /// <param name="input">表示版本的字符串，必须是 "X.Y.Z" 格式，其中 X、Y、Z 为非负整数。</param>
    /// <param name="result">当方法返回时，如果转换成功，则包含已解析的版本；否则为默认值。</param>
    /// <returns>如果 input 成功转换，则为 true；否则为 false。</returns>
    public static bool TryParse(string input, out Version result)
    {
        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// 确定两个指定的 <see cref="Version"/> 实例是否相等。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 等于 b，则为 true；否则为 false。</returns>
    public static bool operator ==(Version a, Version b)
    {
        return a.Equals(b);
    }

    /// <summary>
    /// 确定两个指定的 <see cref="Version"/> 实例是否不相等。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 不等于 b，则为 true；否则为 false。</returns>
    public static bool operator !=(Version a, Version b)
    {
        return !(a == b);
    }

    /// <summary>
    /// 确定第一个指定的 <see cref="Version"/> 是否大于第二个指定的 <see cref="Version"/>。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 大于 b，则为 true；否则为 false。</returns>
    public static bool operator >(Version a, Version b)
    {
        return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// 确定第一个指定的 <see cref="Version"/> 是否大于或等于第二个指定的 <see cref="Version"/>。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 大于或等于 b，则为 true；否则为 false。</returns>
    public static bool operator >=(Version a, Version b)
    {
        return a.CompareTo(b) >= 0;
    }

    /// <summary>
    /// 确定第一个指定的 <see cref="Version"/> 是否小于第二个指定的 <see cref="Version"/>。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 小于 b，则为 true；否则为 false。</returns>
    public static bool operator <(Version a, Version b)
    {
        return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// 确定第一个指定的 <see cref="Version"/> 是否小于或等于第二个指定的 <see cref="Version"/>。
    /// </summary>
    /// <param name="a">要比较的第一个版本。</param>
    /// <param name="b">要比较的第二个版本。</param>
    /// <returns>如果 a 小于或等于 b，则为 true；否则为 false。</returns>
    public static bool operator <=(Version a, Version b)
    {
        return a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// 将版本字符串隐式转换为 <see cref="Version"/> 实例。
    /// </summary>
    /// <param name="versionString">表示版本的字符串，必须是 "X.Y.Z" 格式，其中 X、Y、Z 为非负整数。</param>
    /// <returns>表示指定版本的 <see cref="Version"/> 实例。</returns>
    /// <exception cref="ArgumentException">当版本字符串格式无效时抛出。</exception>
    public static implicit operator Version(string versionString)
    {
        return Parse(versionString);
    }

    /// <summary>
    /// 创建从当前版本到指定最大版本的版本范围。
    /// </summary>
    /// <param name="maxVersion">范围的最大版本。</param>
    /// <param name="includeMin">是否包含当前版本。</param>
    /// <param name="includeMax">是否包含最大版本。</param>
    /// <returns>表示指定范围的 <see cref="VersionRange"/>。</returns>
    public VersionRange To(Version maxVersion, bool includeMin = true, bool includeMax = true)
    {
        return new VersionRange(this, maxVersion, includeMin, includeMax);
    }

    /// <summary>
    /// 创建包含当前版本及以上所有版本的范围。
    /// </summary>
    /// <param name="includeThis">是否包含当前版本。</param>
    /// <returns>表示当前版本及以上所有版本的 <see cref="VersionRange"/>。</returns>
    public VersionRange AndAbove(bool includeThis = true)
    {
        return VersionRange.GreaterThan(this, includeThis);
    }

    /// <summary>
    /// 创建包含当前版本及以下所有版本的范围。
    /// </summary>
    /// <param name="includeThis">是否包含当前版本。</param>
    /// <returns>表示当前版本及以下所有版本的 <see cref="VersionRange"/>。</returns>
    public VersionRange AndBelow(bool includeThis = true)
    {
        return VersionRange.LessThan(this, includeThis);
    }
}


/// <summary>
/// 表示版本的范围。
/// </summary>
public struct VersionRange
{
    // 匹配版本范围的正则表达式
    // 支持三种格式:
    // 1. [1.0.0, 2.0.0), (1.0.0, 2.0.0], [1.0.0, 2.0.0], (1.0.0, 2.0.0) - 区间表示法
    // 2. >=1.0.0, >1.0.0, <=2.0.0, <2.0.0 - 比较操作符表示法
    // 3. 1.x.x, 1.1.x - 通配符表示法
    private static readonly Regex RangeRegex = new Regex(
        @"^\s*(?:(?:([\[\(])\s*([0-9]+\.[0-9]+\.[0-9]+)\s*,\s*([0-9]+\.[0-9]+\.[0-9]+)\s*([\]\)]))|(?:([<>]=?)\s*([0-9]+\.[0-9]+\.[0-9]+))|(?:([0-9]+)(?:\.([0-9]+|x))?(?:\.([0-9]+|x))?))$",
        RegexOptions.Compiled);

    /// <summary>
    /// 获取范围的最小版本。值为 "0.0.0" 时表示不限制最小版本。
    /// </summary>
    public Version Min { get; }

    /// <summary>
    /// 获取范围的最大版本。值为 "0.0.0" 时表示不限制最大版本。
    /// </summary>
    public Version Max { get; }

    /// <summary>
    /// 获取一个值，指示范围是否包含最小版本。
    /// </summary>
    public bool IncludeMin { get; }

    /// <summary>
    /// 获取一个值，指示范围是否包含最大版本。
    /// </summary>
    public bool IncludeMax { get; }

    /// <summary>
    /// 初始化 VersionRange 结构的新实例。
    /// </summary>
    /// <param name="min">最小版本。值为 "0.0.0" 时表示不限制最小版本。</param>
    /// <param name="max">最大版本。值为 "0.0.0" 时表示不限制最大版本。</param>
    /// <param name="includeMin">是否包含最小版本。</param>
    /// <param name="includeMax">是否包含最大版本。</param>
    /// <exception cref="ArgumentException">当最小版本和最大版本同时为 "0.0.0" 时抛出。</exception>
    public VersionRange(Version min, Version max, bool includeMin = true, bool includeMax = true)
    {
        // 验证参数
        if (min.Equals(Version.ZeroVersion) && max.Equals(Version.ZeroVersion))
        {
            throw new ArgumentException("MinVersion 和 MaxVersion 不能同时为 \"0.0.0\"");
        }

        Min = min;
        Max = max;
        IncludeMin = includeMin;
        IncludeMax = includeMax;
    }

    /// <summary>
    /// 确定指定的版本是否在此范围内。
    /// </summary>
    /// <param name="version">要检查的版本。</param>
    /// <returns>如果版本在范围内，则为 true；否则为 false。</returns>
    public bool Contains(Version version)
    {
        if (version.Equals(Version.ZeroVersion))
            return false;

        // 检查最小版本
        if (!Min.Equals(Version.ZeroVersion))
        {
            int minComparison = version.CompareTo(Min);
            if (minComparison < 0 || (minComparison == 0 && !IncludeMin))
                return false;
        }

        // 检查最大版本
        if (!Max.Equals(Version.ZeroVersion))
        {
            int maxComparison = version.CompareTo(Max);
            if (maxComparison > 0 || (maxComparison == 0 && !IncludeMax))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 返回表示当前版本范围的字符串。
    /// </summary>
    /// <returns>表示当前版本范围的字符串，格式为区间表示法，如 "[1.0.0, 2.0.0)"。</returns>
    public override string ToString()
    {
        string minBracket = IncludeMin ? "[" : "(";
        string maxBracket = IncludeMax ? "]" : ")";

        string minStr = Min.Equals(Version.ZeroVersion) ? "0.0.0" : Min.ToString();
        string maxStr = Max.Equals(Version.ZeroVersion) ? "∞" : Max.ToString();

        return $"{minBracket}{minStr}, {maxStr}{maxBracket}";
    }

    /// <summary>
    /// 从字符串解析版本范围。
    /// </summary>
    /// <param name="input">要解析的字符串。</param>
    /// <returns>表示指定范围的 VersionRange。</returns>
    /// <exception cref="ArgumentException">当输入字符串格式无效时抛出。</exception>
    /// <remarks>
    /// 支持三种格式：
    /// <list type="bullet">
    /// <item><description>区间表示法：[1.0.0, 2.0.0)、(1.0.0, 2.0.0]、[1.0.0, 2.0.0]、(1.0.0, 2.0.0)</description></item>
    /// <item><description>比较操作符表示法：>=1.0.0、>1.0.0、<=2.0.0、<2.0.0</description></item>
    /// <item><description>通配符表示法：1.x.x、1.1.x</description></item>
    /// </list>
    /// </remarks>
    public static VersionRange Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("输入字符串不能为空", nameof(input));

        Match match = RangeRegex.Match(input);
        if (!match.Success)
            throw new ArgumentException($"无效的版本范围格式: {input}", nameof(input));

        // 区间表示法: [1.0.0, 2.0.0)
        if (match.Groups[1].Success)
        {
            // 解析边界括号
            string leftBracket = match.Groups[1].Value;
            string rightBracket = match.Groups[4].Value;
            bool includeMin = leftBracket == "[";
            bool includeMax = rightBracket == "]";

            // 解析最小和最大版本
            string minStr = match.Groups[2].Value;
            string maxStr = match.Groups[3].Value;

            Version min = Version.Parse(minStr);
            Version max = Version.Parse(maxStr);

            return new VersionRange(min, max, includeMin, includeMax);
        }
        // 比较操作符表示法: >=1.0.0 或 <2.0.0
        else if (match.Groups[5].Success)
        {
            string op = match.Groups[5].Value;
            string versionStr = match.Groups[6].Value;
            Version version = Version.Parse(versionStr);

            switch (op)
            {
                case ">":
                    return GreaterThan(version, false);
                case ">=":
                    return GreaterThan(version, true);
                case "<":
                    return LessThan(version, false);
                case "<=":
                    return LessThan(version, true);
                default:
                    throw new ArgumentException($"不支持的操作符: {op}", nameof(input));
            }
        }
        // 通配符表示法: 1.x.x 或 1.1.x
        else
        {
            int major = int.Parse(match.Groups[7].Value);

            // 解析次版本号
            bool hasMinor = match.Groups[8].Success && match.Groups[8].Value != "x";
            int minor = hasMinor ? int.Parse(match.Groups[8].Value) : 0;

            // 解析修订号
            bool hasPatch = match.Groups[9].Success && match.Groups[9].Value != "x";
            int patch = hasPatch ? int.Parse(match.Groups[9].Value) : 0;

            // 创建最小和最大版本
            Version min = new Version(major, hasMinor ? minor : 0, hasPatch ? patch : 0);
            Version max;

            if (!hasMinor)
            {
                // 1.x.x 表示 [1.0.0, 2.0.0)
                max = new Version(major + 1, 0, 0);
            }
            else if (!hasPatch)
            {
                // 1.1.x 表示 [1.1.0, 1.2.0)
                max = new Version(major, minor + 1, 0);
            }
            else
            {
                // 精确版本，如 1.1.1
                max = min;
            }

            return new VersionRange(min, max, true, !max.Equals(min));
        }
    }

    /// <summary>
    /// 尝试从字符串解析版本范围。
    /// </summary>
    /// <param name="input">要解析的字符串。</param>
    /// <param name="range">当方法返回时，如果转换成功，则包含已解析的版本范围；否则为默认值。</param>
    /// <returns>如果 input 成功转换，则为 true；否则为 false。</returns>
    public static bool TryParse(string input, out VersionRange range)
    {
        try
        {
            range = Parse(input);
            return true;
        }
        catch
        {
            range = default;
            return false;
        }
    }

    /// <summary>
    /// 创建表示所有大于或等于指定版本的范围。
    /// </summary>
    /// <param name="minVersion">最小版本。</param>
    /// <param name="includeMin">是否包含最小版本。</param>
    /// <returns>表示大于或等于指定版本的范围。</returns>
    public static VersionRange GreaterThan(Version minVersion, bool includeMin = true)
    {
        return new VersionRange(minVersion, new Version(0, 0, 0), includeMin, false);
    }

    /// <summary>
    /// 创建表示所有小于或等于指定版本的范围。
    /// </summary>
    /// <param name="maxVersion">最大版本。</param>
    /// <param name="includeMax">是否包含最大版本。</param>
    /// <returns>表示小于或等于指定版本的范围。</returns>
    public static VersionRange LessThan(Version maxVersion, bool includeMax = true)
    {
        return new VersionRange(new Version(0, 0, 0), maxVersion, false, includeMax);
    }

    /// <summary>
    /// 将字符串隐式转换为 <see cref="VersionRange"/> 实例。
    /// </summary>
    /// <param name="rangeString">表示版本范围的字符串。</param>
    /// <returns>表示指定版本范围的 <see cref="VersionRange"/> 实例。</returns>
    /// <exception cref="ArgumentException">当版本范围字符串格式无效时抛出。</exception>
    /// <remarks>
    /// 支持三种格式：
    /// <list type="bullet">
    /// <item><description>区间表示法：[1.0.0, 2.0.0)、(1.0.0, 2.0.0]、[1.0.0, 2.0.0]、(1.0.0, 2.0.0)</description></item>
    /// <item><description>比较操作符表示法：>=1.0.0、>1.0.0、<=2.0.0、<2.0.0</description></item>
    /// <item><description>通配符表示法：1.x.x、1.1.x</description></item>
    /// </list>
    /// </remarks>
    public static implicit operator VersionRange(string rangeString)
    {
        return Parse(rangeString);
    }
}

#endregion

#region Network LowRes
/// <summary>
/// UFloat24 结构体，使用3个字节存储浮点数，提供更高精度的0-1范围浮点数表示
/// </summary>
public struct UFloat24
{
    /// <summary>
    /// 浮点数值，范围为0到1
    /// </summary>
    public float Value
    {
        get
        {
            // 将三个字节组合成一个24位整数，然后除以最大值(2^24-1)得到0-1范围的浮点数
            int combinedValue = (internalValue1 << 16) | (internalValue2 << 8) | internalValue3;
            return combinedValue * convOut;
        }
        set
        {
            // 将0-1范围的浮点数转换为24位整数，然后分解为3个字节
            int combinedValue = (int)(Mathf.Clamp01(value) * convIn);
            internalValue1 = (byte)((combinedValue >> 16) & 0xFF); // 高8位
            internalValue2 = (byte)((combinedValue >> 8) & 0xFF);  // 中8位
            internalValue3 = (byte)(combinedValue & 0xFF);         // 低8位
        }
    }

    /// <summary>
    /// 设置值，将实际值除以最大值，转换为0-1范围
    /// </summary>
    /// <param name="v">实际值</param>
    /// <param name="maxValue">最大值</param>
    public void Set(float v, float maxValue)
    {
        this.Value = v / maxValue;
    }

    /// <summary>
    /// 获取实际值，将0-1范围的值乘以最大值
    /// </summary>
    /// <param name="maxValue">最大值</param>
    /// <returns>实际值</returns>
    public float Get(float maxValue)
    {
        return this.Value * maxValue;
    }

    /// <summary>
    /// 内部存储值 - 高8位
    /// </summary>
    public byte internalValue1;

    /// <summary>
    /// 内部存储值 - 中8位
    /// </summary>
    public byte internalValue2;

    /// <summary>
    /// 内部存储值 - 低8位
    /// </summary>
    public byte internalValue3;

    /// <summary>
    /// 输出转换系数：1/(2^24-1)
    /// </summary>
    private const float convOut = 1.0f / 16777215.0f; // 1/(2^24-1)

    /// <summary>
    /// 输入转换系数：2^24-1
    /// </summary>
    private const float convIn = 16777215.0f; // 2^24-1
}

/// <summary>
/// SFloat24 结构体，使用3个字节存储有符号浮点数，提供更高精度的-1到1范围浮点数表示
/// </summary>
public struct SFloat24
{
    /// <summary>
    /// 浮点数值，范围为-1到1
    /// </summary>
    public float Value
    {
        get
        {
            // 将三个字节组合成一个24位整数
            int combinedValue = (internalValue1 << 16) | (internalValue2 << 8) | internalValue3;

            // 将24位无符号整数映射到-1到1范围
            // 0 映射到 -1，16777215(2^24-1) 映射到 1
            return (float)combinedValue * convOut - 1.0f;
        }
        set
        {
            // 将-1到1范围的浮点数限制在有效范围内
            float clampedValue = Mathf.Clamp(value, -1.0f, 1.0f);

            // 将-1到1范围的浮点数转换为0到2^24-1范围的整数
            int combinedValue = (int)((clampedValue + 1.0f) * halfConvIn);

            // 分解为3个字节
            internalValue1 = (byte)((combinedValue >> 16) & 0xFF); // 高8位
            internalValue2 = (byte)((combinedValue >> 8) & 0xFF);  // 中8位
            internalValue3 = (byte)(combinedValue & 0xFF);         // 低8位
        }
    }

    /// <summary>
    /// 设置值，将实际值除以最大值，转换为-1到1范围
    /// </summary>
    /// <param name="v">实际值</param>
    /// <param name="maxValue">最大值（绝对值）</param>
    public void Set(float v, float maxValue)
    {
        this.Value = Mathf.Clamp(v / maxValue, -1.0f, 1.0f);
    }

    /// <summary>
    /// 获取实际值，将-1到1范围的值乘以最大值
    /// </summary>
    /// <param name="maxValue">最大值（绝对值）</param>
    /// <returns>实际值</returns>
    public float Get(float maxValue)
    {
        return this.Value * maxValue;
    }

    /// <summary>
    /// 内部存储值 - 高8位
    /// </summary>
    public byte internalValue1;

    /// <summary>
    /// 内部存储值 - 中8位
    /// </summary>
    public byte internalValue2;

    /// <summary>
    /// 内部存储值 - 低8位
    /// </summary>
    public byte internalValue3;

    /// <summary>
    /// 输出转换系数：2/(2^24-1)
    /// 因为我们需要将0到2^24-1映射到-1到1的范围，所以系数是2/(2^24-1)
    /// </summary>
    private const float convOut = 2.0f / 16777215.0f;

    /// <summary>
    /// 输入转换系数的一半：(2^24-1)/2
    /// 用于将-1到1的范围映射到0到2^24-1
    /// </summary>
    private const float halfConvIn = 16777215.0f / 2.0f;
}
#endregion