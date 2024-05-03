using BepInEx.Unity.IL2CPP;
using Hikaria.Core.Interfaces;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using System.Runtime.InteropServices;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
internal class ModList : Feature, IOnSessionMemberChanged
{
    public override string Name => "Mod List";

    [FeatureConfig]
    public static ModListSetting Settings { get; set; }

    public override void Init()
    {
        SNetExt.SetupCustomData<pModList>(typeof(pModList).FullName, ReceiveModListData);
        IL2CPPChainloader.Instance.Finished += OnChainloaderFinished;
        GameEventListener.RegisterSelfInGameEventListener(this);
    }

    public class ModListSetting
    {
        [FSDisplayName("My Mods")]
        public List<ModInfoEntry> MyMods
        {
            get
            {
                var result = new List<ModInfoEntry>();
                foreach (var modInfo in InstalledMods.Values)
                {
                    result.Add(new(modInfo));
                }
                return result;
            }
            set
            {
            }
        }

        [FSInline]
        [FSDisplayName("Others Mods")]
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
        [FSDisplayName("Nickname")]
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
        [FSDisplayName("Mod List")]
        public List<ModInfoEntry> ModList
        {
            get
            {
                if (PlayerModsLookup.TryGetValue(Lookup, out var entry))
                {
                    var result = new List<ModInfoEntry>();
                    foreach (var modInfo in entry.Values)
                    {
                        result.Add(new(modInfo));
                    }
                    return result;
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
            Version = modInfo.Version.ToDetailString();
        }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("Name")]
        public string Name { get; set; }
        [FSReadOnly]
        [FSDisplayName("GUID")]
        public string GUID { get; set; }
        [FSReadOnly]
        [FSDisplayName("Version")]
        public string Version { get; set; }
    }

    private void ReceiveModListData(SNet_Player player, pModList data)
    {
        if (player.IsLocal) return;
        PlayerModsLookup[player.Lookup] = new();
        Logs.LogMessage($"ReceiveModListData from {player.Lookup}, Count:{data.ModCount}");
        for (int i = 0; i < data.ModCount; i++)
        {
            var mod = data.Mods[i];
            PlayerModsLookup[player.Lookup].Add(mod.GUID, mod);
        }
    }

    private void OnPluginLoaded(BepInEx.PluginInfo pluginInfo)
    {
        var metaData = pluginInfo.Metadata;
        var pVersion = pluginInfo.Metadata.Version;
        var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch, pVersion.Build);
        InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
    }

    private void OnChainloaderFinished()
    {
        InstalledMods.Clear();
        foreach (var kvp in IL2CPPChainloader.Instance.Plugins)
        {
            var metaData = kvp.Value.Metadata;
            var pVersion = metaData.Version;
            var version = new Version(pVersion.Major, pVersion.Minor, pVersion.Patch, pVersion.Build);
            InstalledMods[metaData.GUID] = new(metaData.Name, metaData.GUID, version);
        }
        IL2CPPChainloader.Instance.Finished -= OnChainloaderFinished;
        IL2CPPChainloader.Instance.PluginLoaded += OnPluginLoaded;
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (player.IsBot) return;

        if (player.IsLocal)
        {
            SNetExt.SetLocalCustomData<pModList>(new(SNet.LocalPlayer, InstalledMods.Values.ToList()));
            return;
        }

        SNetExt.SendCustomData<pModList>(player);

        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            var index = Settings.OthersMods.FindIndex(p => p.Lookup == player.Lookup);
            if (index != -1)
            {
                Settings.OthersMods.RemoveAt(index);
            }
        }
        else if (playerEvent == SessionMemberEvent.JoinSessionHub)
        {
            if (!Settings.OthersMods.Any(p => p.Lookup == player.Lookup))
            {
                Settings.OthersMods.Add(new PlayerModListEntry(player));
            }
        }
    }

    public static Dictionary<string, pModInfo> InstalledMods = new();
    public static Dictionary<ulong, Dictionary<string, pModInfo>> PlayerModsLookup = new();

    public struct pModList : SNetworkExt.IReplicatedPlayerData
    {
        public pModList(SNet_Player player, List<pModInfo> modList)
        {
            Array.Fill(Mods, new());
            PlayerData.SetPlayer(player);
            ModCount = Math.Clamp(modList.Count, 0, MOD_SYNC_COUNT);
            for (int i = 0; i < ModCount; i++)
            {
                Mods[i] = modList[i];
            }
        }

        public pModList()
        {
            Array.Fill(Mods, new());
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MOD_SYNC_COUNT)]
        public pModInfo[] Mods = new pModInfo[MOD_SYNC_COUNT];

        public int ModCount = 0;

        public const int MOD_SYNC_COUNT = 100;

        public SNetStructs.pPlayer PlayerData { get; set; } = new();
    }

    public struct pModInfo
    {
        public pModInfo(string name, string guid, Version version)
        {
            Name = name;
            GUID = guid;
            Version = version;
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string Name = string.Empty;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string GUID = string.Empty;
        public Version Version = default;
    }

    public struct Version
    {
        public Version(int major, int minor, int patch, string build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        public string ToDetailString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public int Major = 0;
        public int Minor = 0;
        public int Patch = 0;
        public string Build = string.Empty;
    }
}
