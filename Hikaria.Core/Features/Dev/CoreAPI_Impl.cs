using BepInEx.Unity.IL2CPP;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Bootstrap;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Utilities;
using static Hikaria.Core.CoreAPI;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class CoreAPI_Impl : Feature
{
    public override string Name => "CoreAPI Impl";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public override void Init()
    {
        SNetExt.SetupCustomData<pModList>(typeof(pModList).FullName, ReceiveModListData);
        ArchiveModuleChainloader.Instance.Finished += OnChainloaderFinished;
    }

    [ArchivePatch(typeof(SNet_Player), nameof(SNet_Player.SendAllCustomData))]
    private class SNet_Player__SendAllCustomData__Patch
    {
        private static void Postfix(SNet_Player __instance, SNet_Player toPlayer)
        {
            SNetExt.SendAllCustomData(__instance, toPlayer);
        }
    }

    #region Events
    public static event PlayerModsSynced OnPlayerModsSynced;
    #endregion

    #region ModListSync
    public static Dictionary<string, pModInfo> InstalledMods = new();
    public static Dictionary<ulong, Dictionary<string, pModInfo>> OthersMods = new();

    [ArchivePatch(typeof(SNet_Core_STEAM), nameof(SNet_Core_STEAM.CreateLocalPlayer))]
    private class SNet_Core_STEAM__CreateLocalPlayer__Patch
    {
        private static void Postfix()
        {
            SNetExt.SetLocalCustomData<pModList>(new(SNet.LocalPlayer, InstalledMods.Values.ToList()));
        }
    }

    private void ReceiveModListData(SNet_Player player, pModList data)
    {
        if (player.IsLocal || player.IsBot) return;

        if (!OthersMods.ContainsKey(player.Lookup))
        {
            OthersMods[player.Lookup] = new();
        }
        else
        {
            OthersMods[player.Lookup].Clear();
        }
        for (int i = 0; i < data.ModCount; i++)
        {
            var mod = data.Mods[i];
            if (string.IsNullOrWhiteSpace(mod.GUID))
                continue;
            OthersMods[player.Lookup][mod.GUID] = mod;
        }

        Utils.SafeInvoke(OnPlayerModsSynced, player, data.Mods);
    }

    private void OnPluginLoaded(BepInEx.PluginInfo pluginInfo)
    {
        var metaData = pluginInfo.Metadata;
        var pVersion = pluginInfo.Metadata.Version;
        var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch);
        InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
    }

    private void OnModuleLoaded(ModuleInfo moduleInfo)
    {
        var metaData = moduleInfo.Metadata;
        var pVersion = moduleInfo.Metadata.Version;
        var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch);
        InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
    }

    private void OnChainloaderFinished()
    {
        InstalledMods.Clear();
        foreach (var kvp in IL2CPPChainloader.Instance.Plugins)
        {
            var metaData = kvp.Value.Metadata;
            var pVersion = metaData.Version;
            var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch);
            InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
        }
        foreach (var kvp in ArchiveModuleChainloader.Instance.Modules)
        {
            var metaData = kvp.Value.Metadata;
            var pVersion = metaData.Version;
            var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch);
            InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
        }
        IL2CPPChainloader.Instance.PluginLoaded += OnPluginLoaded;
        ArchiveModuleChainloader.Instance.ModuleLoaded += OnModuleLoaded;
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (player.IsBot)
            return;

        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            if (player.IsLocal)
                OthersMods.Clear();
            else
                OthersMods.Remove(player.Lookup);
        }
    }
    #endregion
}
