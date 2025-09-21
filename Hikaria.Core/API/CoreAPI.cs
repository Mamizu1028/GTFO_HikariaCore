using Hikaria.Core.Features.Core;
using SNetwork;
using static Hikaria.Core.Features.Core.ModList;

namespace Hikaria.Core;

public static class CoreAPI
{
    public static bool IsPlayerInstalledCore(SNet_Player player, VersionRange range = default)
    {
        return IsPlayerInstalledMod(player, CoreGlobal.GUID, range);
    }

    public static bool IsPlayerInstalledMod(SNet_Player player, string guid, VersionRange range = default)
    {
        if (player == null || player.IsBot) return false;
        if (player.IsLocal)
        {
            return InstalledMods.TryGetValue(guid, out var info1) && range.Contains(info1.Version);
        }
        return PlayerModsLookup.TryGetValue(player.Lookup, out var lookup) && lookup.TryGetValue(guid, out var info2) && range.Contains(info2.Version);
    }

    public delegate void PlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods);

    public static event PlayerModsSynced OnPlayerModsSynced { add => ModList.OnPlayerModsSynced += value; remove => ModList.OnPlayerModsSynced -= value; }
}
