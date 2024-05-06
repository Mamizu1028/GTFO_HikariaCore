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
        if (player == null) return false;
        if (player.IsLocal)
        {
            return InstalledMods.TryGetValue(guid, out var info1) && info1.Version >= version;
        }
        return PlayerModsLookup.TryGetValue(player.Lookup, out var lookup) && lookup.TryGetValue(guid, out var info2) && info2.Version >= version;
    }
}
