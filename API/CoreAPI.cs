using SNetwork;
using static Hikaria.Core.Features.Accessibility.ModList;
using Version = Hikaria.Core.Utilities.Version;

namespace Hikaria.Core;

public static class CoreAPI
{
    public static bool IsPlayerInstalledCore(SNet_Player player, Version version = default)
    {
        return IsPlayerInstalledMod(player, PluginInfo.GUID, version);
    }

    /*
    public static bool IsPlayerInstalledMod(SNet_Player player, string guid, Version version = default)
    {
        if (player == null) return false;
        var data = player.LoadCustomData<pModList>();
        if (data.ModCount == 0) return false;
        for (int i = 0; i < data.ModCount; i++)
        {
            var mod = data.Mods[i];
            if (mod.GUID == guid)
            {
                if (mod.Version >= version)
                    return true;
                return false;
            }
        }
        return false;
    }
    */

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
