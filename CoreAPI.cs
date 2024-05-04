using Hikaria.Core.SNetworkExt;
using SNetwork;
using static Hikaria.Core.Features.ModList;
using Version = Hikaria.Core.Utilities.Version;

namespace Hikaria.Core;

public static class CoreAPI
{
    public static bool IsPlayerInstalledCore(SNet_Player player, Version version = default)
    {
        if (player.LoadCustomData<pModList>().Mods.ToList().Any(p => p.GUID == PluginInfo.GUID && p.Version >= version))
        {
            return true;
        }
        return false;
    }

    public static bool IsPlayerInstalledMod(SNet_Player player, string guid, Version version = default)
    {
        if (player.LoadCustomData<pModList>().Mods.ToList().Any(p => p.GUID == guid && p.Version >= version))
        {
            return true;
        }
        return false;
    }
}
