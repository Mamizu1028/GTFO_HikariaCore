using Hikaria.Core.SourceGenerators.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;

namespace Hikaria.Core.SourceGenerators;

/// <summary>对应 [NativeDetour] 标注的 hook 方法的全部编译期信息;value-equatable。</summary>
public sealed record HookInfo(
    // ---- 包含类信息 ----
    string ContainingNamespace,
    EquatableArray<string> ContainingTypeChain,
    bool ContainingIsPartial,
    bool ContainingIsStatic,
    Accessibility ContainingAccessibility,

    // ---- hook 方法信息 ----
    string HookMethodName,
    string Stem,
    string ReturnTypeSyntax,
    EquatableArray<ParamInfo> Parameters,
    bool HookIsStatic,

    // ---- attribute 信息 ----
    string TargetDeclaringTypeSyntax,
    string TargetMethodNameOrCtor,
    NativeDetourMember TargetMember,
    bool TargetIsStatic,
    EquatableArray<string> TargetTypeArgumentSyntax,

    // ---- 位置 ----
    FileLinePositionSpan HookSpan,

    // ---- 诊断 ----
    EquatableArray<DiagnosticInfo> Diagnostics);

public sealed record ParamInfo(string TypeSyntax, string Name) : IEquatable<ParamInfo>;

public sealed record DiagnosticInfo(string Id, string Message, FileLinePositionSpan Span)
    : IEquatable<DiagnosticInfo>;

/// <summary>SG 内部副本,与运行时 NativeDetourMember enum 一一对应。</summary>
public enum NativeDetourMember
{
    Method,
    Constructor,
    Getter,
    Setter,
}
