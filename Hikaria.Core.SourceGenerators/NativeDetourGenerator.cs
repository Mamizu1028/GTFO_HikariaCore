using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Hikaria.Core.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public sealed class NativeDetourGenerator : IIncrementalGenerator
{
    public const string AttributeFullName = "Hikaria.Core.Detour.NativeDetourAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hookInfos = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => HookMethodAnalyzer.Analyze(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        var collected = hookInfos.Collect();

        context.RegisterSourceOutput(collected, static (ctx, all) =>
        {
            // 跨 hook 检测产生新诊断
            var crossDiags = DetectCrossHookConflicts(all);

            // 报告 per-hook 诊断
            foreach (var info in all)
            {
                foreach (var d in info.Diagnostics)
                {
                    ctx.ReportDiagnostic(MakeDiagnostic(d));
                }
            }

            // 跨 hook 诊断
            foreach (var d in crossDiags)
            {
                ctx.ReportDiagnostic(MakeDiagnostic(d));
            }

            // ---- emit ----

            // 按容器类分组,每组生成一个伙伴文件
            var byContainer = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<HookInfo>>();
            foreach (var info in all)
            {
                // 跳过有 fatal 诊断的(避免 emit 半成品引发链式编译错)
                var hasFatal = info.Diagnostics.AsImmutableArray()
                    .Any(d => d.Id is "EZD001" or "EZD002" or "EZD003" or "EZD004"
                                  or "EZD007" or "EZD008" or "EZD009" or "EZD010" or "EZD011");
                if (hasFatal) continue;

                var key = info.ContainingNamespace + "::" + string.Join("+", info.ContainingTypeChain.AsImmutableArray());
                if (!byContainer.TryGetValue(key, out var list))
                {
                    list = new System.Collections.Generic.List<HookInfo>();
                    byContainer[key] = list;
                }
                list.Add(info);
            }

            foreach (var kv in byContainer)
            {
                var any = kv.Value[0];
                var src = Emitters.CompanionEmitter.Emit(
                    any.ContainingNamespace,
                    any.ContainingTypeChain.AsImmutableArray(),
                    kv.Value);
                var hint = string.Join("+", any.ContainingTypeChain.AsImmutableArray()) + ".NativeDetour.g.cs";
                ctx.AddSource(hint, src);
            }
        });
    }

    private static System.Collections.Generic.List<DiagnosticInfo> DetectCrossHookConflicts(
        System.Collections.Immutable.ImmutableArray<HookInfo> all)
    {
        var diags = new System.Collections.Generic.List<DiagnosticInfo>();

        // EZD005: 同目标 (DeclaringType, MethodName, Member, TypeArguments) 不能被多个 hook 注解
        var byTarget = new System.Collections.Generic.Dictionary<string, HookInfo>();
        foreach (var info in all)
        {
            // 解析失败的目标跳过(EZD011 已经报过)
            if (info.Diagnostics.AsImmutableArray().Any(d => d.Id == "EZD011")) continue;
            var key = $"{info.TargetDeclaringTypeSyntax}.{info.TargetMethodNameOrCtor}<{info.TargetMember}>[{string.Join(",", info.TargetTypeArgumentSyntax.AsImmutableArray())}]";
            if (byTarget.TryGetValue(key, out var existing))
            {
                diags.Add(new DiagnosticInfo("EZD005",
                    $"目标方法 '{info.TargetDeclaringTypeSyntax}.{info.TargetMethodNameOrCtor}' 已被 '{existing.HookMethodName}' 注解,不能再由 '{info.HookMethodName}' 重复 detour",
                    info.HookSpan));
            }
            else
            {
                byTarget[key] = info;
            }
        }

        // EZD006: 同 partial class 内 Stem 同名冲突
        var byContainerStem = new System.Collections.Generic.Dictionary<string, HookInfo>();
        foreach (var info in all)
        {
            var containerKey = info.ContainingNamespace + "." + string.Join("+", info.ContainingTypeChain.AsImmutableArray());
            var key = containerKey + "::" + info.Stem;
            if (byContainerStem.TryGetValue(key, out var existing))
            {
                diags.Add(new DiagnosticInfo("EZD006",
                    $"类 '{containerKey}' 中存在多个 hook 方法导致生成名称 '{info.Stem}_Handle' 冲突",
                    info.HookSpan));
            }
            else
            {
                byContainerStem[key] = info;
            }
        }

        return diags;
    }

    private static Diagnostic MakeDiagnostic(DiagnosticInfo info)
    {
        var descriptor = ResolveDescriptor(info.Id);
        var location = Location.Create(info.Span.Path, default, info.Span.Span);
        // 用 dynamic message 覆盖 descriptor.MessageFormat:analyzer 已经把参数代入好,
        // 这里包一个新的 descriptor,把 info.Message 当成 messageFormat。
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                descriptor.Id, descriptor.Title, info.Message,
                descriptor.Category, descriptor.DefaultSeverity, descriptor.IsEnabledByDefault),
            location);
    }

    private static DiagnosticDescriptor ResolveDescriptor(string id) => id switch
    {
        "EZD001" => Diagnostics.EZD001_NotPartial,
        "EZD002" => Diagnostics.EZD002_NotStatic,
        "EZD003" => Diagnostics.EZD003_BadLastParam,
        "EZD004" => Diagnostics.EZD004_BadFirstParam,
        "EZD005" => Diagnostics.EZD005_DuplicateTarget,
        "EZD006" => Diagnostics.EZD006_StemConflict,
        "EZD007" => Diagnostics.EZD007_GenericContainer,
        "EZD008" => Diagnostics.EZD008_BadModifier,
        "EZD009" => Diagnostics.EZD009_CtorWithMethodName,
        "EZD010" => Diagnostics.EZD010_PropMissingName,
        "EZD011" => Diagnostics.EZD011_UnresolvedDeclaringType,
        "EZD012" => Diagnostics.EZD012_TypeArgsIgnored,
        "EZD014" => Diagnostics.EZD014_GenericMethodExperimental,
        _ => throw new System.InvalidOperationException($"未知 EZD: {id}"),
    };
}
