using Hikaria.Core.Features.Dev;
using SNetwork;

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
            return CoreAPI_Impl.InstalledMods.TryGetValue(guid, out var info1) && range.Contains(info1.Version);
        }
        return CoreAPI_Impl.OthersMods.TryGetValue(player.Lookup, out var lookup) && lookup.TryGetValue(guid, out var info2) && range.Contains(info2.Version);
    }

    #region Delegates
    public delegate void PlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods);
    #endregion

    public static event PlayerModsSynced OnPlayerModsSynced { add => CoreAPI_Impl.OnPlayerModsSynced += value; remove => CoreAPI_Impl.OnPlayerModsSynced -= value; }
}
