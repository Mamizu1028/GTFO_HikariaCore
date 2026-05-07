using System.Collections.Concurrent;
using BepInEx.Unity.IL2CPP.Hook;

namespace Hikaria.Core.Detour;

public static class NativeDetourRegistry
{
    private static readonly ConcurrentDictionary<nint, INativeDetour> s_byPointer = new();

    private static readonly ConcurrentDictionary<INativeDetour, nint> s_byDetour = new();

    public static bool IsRegistered(nint methodPointer)
        => methodPointer != 0 && s_byPointer.ContainsKey(methodPointer);

    internal static void Register(nint methodPointer, INativeDetour detour)
    {
        if (methodPointer == 0) throw new ArgumentException("方法指针不可为 0", nameof(methodPointer));
        if (detour == null)     throw new ArgumentNullException(nameof(detour));

        if (!s_byPointer.TryAdd(methodPointer, detour))
            throw new NativeDetourConflictException(methodPointer);

        s_byDetour.TryAdd(detour, methodPointer);
    }

    internal static void Unregister(INativeDetour detour)
    {
        if (detour == null) return;
        if (s_byDetour.TryRemove(detour, out var ptr))
            s_byPointer.TryRemove(ptr, out _);
    }

    internal static void DisposeAll()
    {
        foreach (var kv in s_byPointer.ToArray())
        {
            var d = kv.Value;
            try { d.Undo();    } catch { /* swallow */ }
            try { d.Free();    } catch { /* swallow */ }
            try { d.Dispose(); } catch { /* swallow */ }
        }
        s_byPointer.Clear();
        s_byDetour.Clear();
    }
}
