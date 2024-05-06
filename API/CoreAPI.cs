using Hikaria.Core.Features.Accessibility;
using Hikaria.Core.Interfaces;
using SNetwork;
using static Hikaria.Core.Features.Accessibility.ModList;

namespace Hikaria.Core;

public static class CoreAPI
{
    public static bool IsPlayerInstalledCore(SNet_Player player, Version version = default)
    {
        return IsPlayerInstalledMod(player, PluginInfo.GUID, version);
    }

    public static bool IsPlayerInstalledMod(SNet_Player player, string guid, Version version = default)
    {
        if (player == null || player.IsBot) return false;
        if (player.IsLocal)
        {
            return InstalledMods.TryGetValue(guid, out var info1) && info1.Version >= version;
        }
        return PlayerModsLookup.TryGetValue(player.Lookup, out var lookup) && lookup.TryGetValue(guid, out var info2) && info2.Version >= version;
    }

    public static void RegisterSelf<T>(T instance)
    {
        Type type = instance.GetType();
        if (type.IsInterface || type.IsAbstract)
            return;

        if (typeof(IOnPlayerModsSynced).IsAssignableFrom(type))
            PlayerModsSyncedListeners.Add((IOnPlayerModsSynced)instance);
    }

    public static event Action<SNet_Player, IEnumerable<pModInfo>> OnPlayerModsSynced { add => ModList.OnPlayerModsSynced += value; remove => ModList.OnPlayerModsSynced -= value; }
}
