using Hikaria.Core.Features.Dev;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Core;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
internal class ModList : Feature
{
    public override string Name => "插件列表";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Core");

    [FeatureConfig]
    public static ModListSetting Settings { get; set; }

    public class ModListSetting
    {
        [FSDisplayName("我的插件")]
        public List<ModInfoEntry> InstalledMods { get; set; } = new();

        [FSInline]
        [FSDisplayName("其他人的插件")]
        public List<PlayerModListEntry> OthersMods { get; set; } = new();
    }

    public class PlayerModListEntry
    {
        public PlayerModListEntry(SNet_Player player)
        {
            Lookup = player.Lookup;
            Nickname = player.NickName;
            if (CoreAPI_Impl.OthersMods.TryGetValue(Lookup, out var entry))
            {
                ModList = new List<ModInfoEntry>(entry.Values.Select(modInfo => new ModInfoEntry(modInfo)));
            }
        }

        [FSIgnore]
        public ulong Lookup { get; private set; }

        [FSSeparator]
        [FSReadOnly]
        [FSDisplayName("昵称")]
        public string Nickname { get; set; } = string.Empty;

        [FSReadOnly]
        [FSInline]
        [FSDisplayName("插件列表")]
        public List<ModInfoEntry> ModList { get; set; } = new();
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

    public override void OnEnable()
    {
        GameEventAPI.OnSessionMemberChanged += OnSessionMemberChanged;
    }

    public override void OnDisable()
    {
        GameEventAPI.OnSessionMemberChanged -= OnSessionMemberChanged;
    }

    [ArchivePatch(typeof(SNet_Core_STEAM), nameof(SNet_Core_STEAM.CreateLocalPlayer))]
    private class SNet_Core_STEAM__CreateLocalPlayer__Patch
    {
        private static void Postfix()
        {
            Settings.InstalledMods = new(CoreAPI_Impl.InstalledMods.Values.Select(modInfo => new ModInfoEntry(modInfo)));
        }
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (player.IsBot)
            return;

        if (playerEvent == SessionMemberEvent.LeftSessionHub)
        {
            if (player.IsLocal)
                Settings.OthersMods.Clear();
            else
                Settings.OthersMods.RemoveAll(p => p.Lookup == player.Lookup);
        }
        else if (playerEvent == SessionMemberEvent.JoinSessionHub)
        {
            if (player.IsLocal)
                return;

            if (!Settings.OthersMods.Any(p => p.Lookup == player.Lookup))
            {
                Settings.OthersMods.Add(new PlayerModListEntry(player));
            }
        }
    }
}