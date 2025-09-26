using BepInEx.Unity.IL2CPP;
using Hikaria.Core.Interfaces;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.Bootstrap;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Utilities;
using static Hikaria.Core.CoreAPI;

namespace Hikaria.Core.Features.Core;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
internal class ModList : Feature, IOnSessionMemberChanged
{
    public override string Name => "插件列表";

    public override string Description => "获取所有安装了本插件的玩家的插件列表。\n" +
        "本功能属于核心功能，所有插件的正常运作离不开该功能。";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Core");

    public static event PlayerModsSynced OnPlayerModsSynced;

    [FeatureConfig]
    public static ModListSetting Settings { get; set; }

    public override void Init()
    {
        SNetExt.SetupCustomData<pModList>(typeof(pModList).FullName, ReceiveModListData);
        ArchiveModuleChainloader.Instance.Finished += OnChainloaderFinished;
        GameEventAPI.RegisterListener(this);
    }

    public class ModListSetting
    {
        [FSDisplayName("我的插件")]
        public List<ModInfoEntry> MyMods
        {
            get
            {
                return new(InstalledMods.Values.Select(modInfo => new ModInfoEntry(modInfo)));
            }
            set
            {
            }
        }

        [FSInline]
        [FSDisplayName("其他人的插件")]
        public List<PlayerModListEntry> OthersMods { get; set; } = new();
    }

    public class PlayerModListEntry
    {
        public PlayerModListEntry(SNet_Player player)
        {
            Lookup = player.Lookup;
        }

        [FSReadOnly]
        [FSIgnore]
        public ulong Lookup { get; set; }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("昵称")]
        public string Nickname
        {
            get
            {
                if (!SNet.Core.TryGetPlayer(Lookup, out var player, true))
                {
                    return Lookup.ToString();
                }
                return player.NickName;
            }
            set
            {
            }
        }

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("插件列表")]
        public List<ModInfoEntry> ModList
        {
            get
            {
                if (PlayerModsLookup.TryGetValue(Lookup, out var entry))
                {
                    return new(entry.Values.Select(modInfo => new ModInfoEntry(modInfo)));
                }
                return new();
            }
            set
            {
            }
        }
    }

    public class ModInfoEntry
    {
        public ModInfoEntry(pModInfo modInfo)
        {
            Name = modInfo.Name;
            GUID = modInfo.GUID;
            Version = modInfo.Version.ToString();
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("名称")]
        public string Name { get; set; }
        [FSReadOnly]
        [FSDisplayName("唯一识别符")]
        public string GUID { get; set; }
        [FSReadOnly]
        [FSDisplayName("版本")]
        public string Version { get; set; }
    }

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

        if (!PlayerModsLookup.ContainsKey(player.Lookup))
        {
            PlayerModsLookup[player.Lookup] = new();
        }
        else
        {
            PlayerModsLookup[player.Lookup].Clear();
        }
        for (int i = 0; i < data.ModCount; i++)
        {
            var mod = data.Mods[i];
            if (string.IsNullOrWhiteSpace(mod.GUID))
                continue;
            PlayerModsLookup[player.Lookup][mod.GUID] = mod;
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
        if (player.IsBot) return;

        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            if (player.IsLocal)
            {
                Settings.OthersMods.Clear();
            }
            else
            {
                Settings.OthersMods.RemoveAll(p => p.Lookup == player.Lookup);
            }
        }
        else if (playerEvent == SessionMemberEvent.JoinSessionHub)
        {
            if (player.IsLocal) return;
            if (!Settings.OthersMods.Any(p => p.Lookup == player.Lookup))
            {
                Settings.OthersMods.Add(new PlayerModListEntry(player));
            }
        }
    }

    public static Dictionary<string, pModInfo> InstalledMods = new();
    public static Dictionary<ulong, Dictionary<string, pModInfo>> PlayerModsLookup = new();
}