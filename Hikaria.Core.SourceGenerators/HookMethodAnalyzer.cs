using Hikaria.Core.SourceGenerators.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Hikaria.Core.SourceGenerators;

internal static class HookMethodAnalyzer
{
    public static HookInfo? Analyze(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not IMethodSymbol methodSymbol) return null;
        if (ctx.TargetNode is not MethodDeclarationSyntax methodDecl) return null;

        var diagnostics = new List<DiagnosticInfo>();
        var location = methodDecl.Identifier.GetLocation().GetLineSpan();

        // 1. 容器类信息 + EZD001 / EZD007
        var containing = ExtractContainingType(methodSymbol, methodDecl, location, diagnostics);

        // 2. hook 方法形态 + EZD002 / EZD008
        ValidateHookMethodForm(methodSymbol, methodDecl, location, diagnostics);

        // 3. attribute 数据(DeclaringType / MethodName / Static / Member / TypeArguments)
        var attr = ctx.Attributes.FirstOrDefault();
        if (attr is null)
        {
            // 极端情况:既然 ForAttributeWithMetadataName 命中了,理论上 attr 必存在;无 attr 直接放弃此 hook
            return null;
        }

        var attrInfo = ExtractAttributeInfo(attr, location, diagnostics);

        // 4. EZD003 末参 / EZD004 首参
        ValidateSignature(methodSymbol, attrInfo.IsStatic, attrInfo.Member, location, diagnostics);

        // 5. EZD009 / EZD010
        ValidateMemberAndMethodName(attrInfo, location, diagnostics);

        // 6. EZD012 / EZD014
        ValidateTypeArguments(attrInfo, location, diagnostics);

        // 7. 抽取参数信息 + ReturnType
        var parameters = ExtractParameters(methodSymbol);
        var returnSyntax = TypeSyntax(methodSymbol.ReturnType);

        // 8. 派生 Stem
        var stem = DeriveStem(methodSymbol.Name);

        // 9. 解糖目标方法名
        var targetMethodName = DesugarTargetMethodName(attrInfo);

        return new HookInfo(
            ContainingNamespace: containing.Namespace,
            ContainingTypeChain: containing.Chain,
            ContainingIsPartial: containing.IsPartial,
            ContainingIsStatic: containing.IsStatic,
            ContainingAccessibility: containing.Accessibility,
            HookMethodName: methodSymbol.Name,
            Stem: stem,
            ReturnTypeSyntax: returnSyntax,
            Parameters: parameters,
            HookIsStatic: methodSymbol.IsStatic,
            TargetDeclaringTypeSyntax: attrInfo.DeclaringTypeSyntax,
            TargetMethodNameOrCtor: targetMethodName,
            TargetMember: attrInfo.Member,
            TargetIsStatic: attrInfo.IsStatic,
            TargetTypeArgumentSyntax: attrInfo.TypeArgumentSyntax,
            HookSpan: location,
            Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    // -------- helpers,后续 Task 填实现 --------
    private static (string Namespace, EquatableArray<string> Chain, bool IsPartial, bool IsStatic, Accessibility Accessibility)
        ExtractContainingType(IMethodSymbol m, MethodDeclarationSyntax decl, FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        var container = m.ContainingType;
        var ns = container.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : container.ContainingNamespace.ToDisplayString();

        // 嵌套类链(由外向内)
        var chain = new List<string>();
        var t = container;
        while (t is not null)
        {
            chain.Insert(0, t.Name);
            t = t.ContainingType;
        }

        // EZD007: 不允许包含类是泛型
        if (container.IsGenericType || container.TypeParameters.Length > 0)
        {
            diags.Add(new DiagnosticInfo("EZD007",
                string.Format("[NativeDetour] hook 不能位于泛型类 '{0}<...>' 中", container.Name),
                loc));
        }

        // EZD001: 检查每层是否 partial
        bool allPartial = true;
        var declSyntax = decl.Parent as TypeDeclarationSyntax;
        while (declSyntax is not null)
        {
            bool isPartial = declSyntax.Modifiers.Any(m2 => m2.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
            if (!isPartial)
            {
                allPartial = false;
                diags.Add(new DiagnosticInfo("EZD001",
                    string.Format("类 '{0}' 含有 [NativeDetour] 方法,必须声明为 partial 才能由源生成器扩展", declSyntax.Identifier.Text),
                    loc));
            }
            declSyntax = declSyntax.Parent as TypeDeclarationSyntax;
        }

        return (
            ns,
            new EquatableArray<string>(chain),
            allPartial,
            container.IsStatic,
            container.DeclaredAccessibility);
    }

    private static void ValidateHookMethodForm(
        IMethodSymbol m, MethodDeclarationSyntax decl, FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        if (!m.IsStatic)
        {
            diags.Add(new DiagnosticInfo("EZD002",
                string.Format("[NativeDetour] hook 方法 '{0}' 必须是 static", m.Name),
                loc));
        }

        string? badModifier = null;
        if (m.IsAbstract)                                       badModifier = "abstract";
        else if (m.IsVirtual)                                   badModifier = "virtual";
        else if (m.IsExtern)                                    badModifier = "extern";
        else if (m.IsAsync)                                     badModifier = "async";
        else if (m.IsPartialDefinition)                         badModifier = "partial";

        if (badModifier is not null)
        {
            diags.Add(new DiagnosticInfo("EZD008",
                string.Format("[NativeDetour] hook 方法 '{0}' 不能是 {1}", m.Name, badModifier),
                loc));
        }
    }

    private static AttrInfo ExtractAttributeInfo(
        AttributeData attr, FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        string declaringTypeSyntax = "global::System.Object"; // 兜底,避免后续 emit 出语法错误
        string methodName = string.Empty;
        bool isStatic = false;
        NativeDetourMember member = NativeDetourMember.Method;
        var typeArgsList = new List<string>();

        // 构造器参数
        var ctorArgs = attr.ConstructorArguments;
        if (ctorArgs.Length >= 1)
        {
            if (ctorArgs[0].Value is INamedTypeSymbol declType && declType.TypeKind != TypeKind.Error)
            {
                declaringTypeSyntax = declType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            else
            {
                diags.Add(new DiagnosticInfo("EZD011",
                    "[NativeDetour] 的 DeclaringType 无法解析为已知类型",
                    loc));
            }
        }
        if (ctorArgs.Length >= 2 && ctorArgs[1].Value is string mn)
        {
            methodName = mn;
        }
        else if (ctorArgs.Length == 1)
        {
            // 单参 ctor → 自动 Constructor
            member = NativeDetourMember.Constructor;
            methodName = string.Empty;
        }

        // 命名参数
        foreach (var kvp in attr.NamedArguments)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            switch (key)
            {
                case "Static":
                    isStatic = (bool)(value.Value ?? false);
                    break;
                case "Member":
                    member = (NativeDetourMember)(int)(value.Value ?? 0);
                    break;
                case "TypeArguments":
                    if (!value.Values.IsDefaultOrEmpty)
                    {
                        foreach (var tArg in value.Values)
                        {
                            if (tArg.Value is ITypeSymbol ts)
                                typeArgsList.Add(ts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        }
                    }
                    break;
            }
        }

        return new AttrInfo(
            DeclaringTypeSyntax: declaringTypeSyntax,
            MethodName: methodName,
            IsStatic: isStatic,
            Member: member,
            TypeArgumentSyntax: new EquatableArray<string>(typeArgsList));
    }

    private const string Il2CppMethodInfoFullName = "global::Il2CppInterop.Runtime.Runtime.Il2CppMethodInfo*";

    private static void ValidateSignature(
        IMethodSymbol m, bool targetIsStatic, NativeDetourMember member,
        FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        var ps = m.Parameters;

        // 末参必须是 Il2CppMethodInfo*
        if (ps.Length == 0)
        {
            diags.Add(new DiagnosticInfo("EZD003",
                $"[NativeDetour] hook 方法 '{m.Name}' 的最后一个参数必须是 'Il2CppMethodInfo*' 类型,实际为 '(无参)'",
                loc));
            return;
        }

        var last = ps[ps.Length - 1];
        var lastSyntax = last.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (lastSyntax != Il2CppMethodInfoFullName)
        {
            diags.Add(new DiagnosticInfo("EZD003",
                $"[NativeDetour] hook 方法 '{m.Name}' 的最后一个参数必须是 'Il2CppMethodInfo*' 类型,实际为 '{lastSyntax}'",
                loc));
        }

        // 非静态目标(普通实例方法 / 实例构造器 / 实例 getter|setter)首参必须是 IntPtr
        if (!targetIsStatic)
        {
            if (ps.Length < 1 || ps[0].Type.SpecialType != SpecialType.System_IntPtr)
            {
                diags.Add(new DiagnosticInfo("EZD004",
                    $"[NativeDetour] 实例方法 hook '{m.Name}' 的第一个参数必须是 IntPtr;若目标是静态方法,请在 attribute 上设置 'Static = true'",
                    loc));
            }
        }
    }

    private static void ValidateMemberAndMethodName(
        AttrInfo attr, FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        // EZD009: 构造器不应填非空 MethodName
        if (attr.Member == NativeDetourMember.Constructor && !string.IsNullOrEmpty(attr.MethodName))
        {
            diags.Add(new DiagnosticInfo("EZD009",
                "当 Member=Constructor 时,MethodName 应省略或为空(SG 自动解糖为 \".ctor\")",
                loc));
        }

        // EZD010: getter/setter 必须有 MethodName
        if ((attr.Member == NativeDetourMember.Getter || attr.Member == NativeDetourMember.Setter)
            && string.IsNullOrWhiteSpace(attr.MethodName))
        {
            diags.Add(new DiagnosticInfo("EZD010",
                $"Member={attr.Member} 时必须提供属性名作为 MethodName",
                loc));
        }
    }

    private static void ValidateTypeArguments(
        AttrInfo attr, FileLinePositionSpan loc, List<DiagnosticInfo> diags)
    {
        bool hasTypeArgs = !attr.TypeArgumentSyntax.IsEmpty;
        if (!hasTypeArgs) return;

        // EZD012: TypeArguments 仅 Method 模式有意义
        if (attr.Member != NativeDetourMember.Method)
        {
            diags.Add(new DiagnosticInfo("EZD012",
                $"TypeArguments 仅在 Member=Method 时有效,当前 Member={attr.Member} 将被忽略",
                loc));
            return;
        }

        // EZD014: 提示试验性
        diags.Add(new DiagnosticInfo("EZD014",
            "方法级泛型 detour 是试验性能力,请验证目标方法能正确解析",
            loc));
    }

    private static EquatableArray<ParamInfo> ExtractParameters(IMethodSymbol m)
    {
        var list = new List<ParamInfo>(m.Parameters.Length);
        foreach (var p in m.Parameters)
        {
            list.Add(new ParamInfo(
                TypeSyntax: p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Name: p.Name));
        }
        return new EquatableArray<ParamInfo>(list);
    }

    private static string TypeSyntax(ITypeSymbol t)
        => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string DeriveStem(string hookName)
        => hookName.EndsWith("_Hook") ? hookName.Substring(0, hookName.Length - "_Hook".Length) : hookName;

    private static string DesugarTargetMethodName(AttrInfo attr) => attr.Member switch
    {
        NativeDetourMember.Constructor => ".ctor",
        NativeDetourMember.Getter      => "get_" + attr.MethodName,
        NativeDetourMember.Setter      => "set_" + attr.MethodName,
        _                              => attr.MethodName,
    };

    internal record struct AttrInfo(
        string DeclaringTypeSyntax,
        string MethodName,
        bool IsStatic,
        NativeDetourMember Member,
        EquatableArray<string> TypeArgumentSyntax);
}
