using Hikaria.Core.SNetworkExt;
using SNetwork;
using static Hikaria.Core.Features.ModList;

namespace Hikaria.Core;

public static class API
{
    public static bool IsPlayerInstalledCore(SNet_Player player)
    {
        if (player.LoadCustomData<pModList>().Mods.ToList().Any(p => p.GUID == PluginInfo.GUID))
        {
            return true;
        }
        return false;
    }

    public static bool IsPlayerInstalledMod(SNet_Player player, string guid)
    {
        if (player.LoadCustomData<pModList>().Mods.ToList().Any(p => p.GUID == guid))
        {
            return true;
        }
        return false;
    }
}
