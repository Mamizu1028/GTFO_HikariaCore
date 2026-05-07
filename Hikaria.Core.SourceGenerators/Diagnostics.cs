using Microsoft.CodeAnalysis;

namespace Hikaria.Core.SourceGenerators;

internal static class Diagnostics
{
    private const string Category = "NativeDetour";

    public static readonly DiagnosticDescriptor EZD001_NotPartial = new(
        "EZD001", "包含 [NativeDetour] 方法的类必须声明为 partial",
        "类 '{0}' 含有 [NativeDetour] 方法,必须声明为 partial 才能由源生成器扩展",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD002_NotStatic = new(
        "EZD002", "[NativeDetour] hook 方法必须 static",
        "[NativeDetour] hook 方法 '{0}' 必须是 static",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD003_BadLastParam = new(
        "EZD003", "hook 末参必须是 Il2CppMethodInfo*",
        "[NativeDetour] hook 方法 '{0}' 的最后一个参数必须是 'Il2CppMethodInfo*' 类型,实际为 '{1}'",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD004_BadFirstParam = new(
        "EZD004", "实例方法首参必须是 IntPtr",
        "[NativeDetour] 实例方法 hook '{0}' 的第一个参数必须是 IntPtr;若目标是静态方法,请在 attribute 上设置 'Static = true'",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD005_DuplicateTarget = new(
        "EZD005", "目标方法被多个 hook 重复 detour",
        "目标方法 '{0}' 已被 '{1}' 注解,不能再由 '{2}' 重复 detour",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD006_StemConflict = new(
        "EZD006", "Stem 命名冲突",
        "类 '{0}' 中存在多个 hook 方法导致生成名称 '{1}_Handle' 冲突",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD007_GenericContainer = new(
        "EZD007", "包含类不能是泛型",
        "[NativeDetour] hook 不能位于泛型类 '{0}<...>' 中",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD008_BadModifier = new(
        "EZD008", "hook 方法形态不合法",
        "[NativeDetour] hook 方法 '{0}' 不能是 {1}",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD009_CtorWithMethodName = new(
        "EZD009", "构造器不应填 MethodName",
        "当 Member=Constructor 时,MethodName 应省略或为空(SG 自动解糖为 \".ctor\")",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD010_PropMissingName = new(
        "EZD010", "Getter/Setter 必须有属性名",
        "Member={0} 时必须提供属性名作为 MethodName",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD011_UnresolvedDeclaringType = new(
        "EZD011", "DeclaringType 必须可解析",
        "[NativeDetour] 的 DeclaringType 无法解析为已知类型",
        Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor EZD012_TypeArgsIgnored = new(
        "EZD012", "TypeArguments 与 Member 不兼容",
        "TypeArguments 仅在 Member=Method 时有效,当前 Member={0} 将被忽略",
        Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor EZD014_GenericMethodExperimental = new(
        "EZD014", "方法级泛型 detour 是试验性能力",
        "方法级泛型 detour 是试验性能力,请验证目标方法能正确解析",
        Category, DiagnosticSeverity.Info, true);
}
