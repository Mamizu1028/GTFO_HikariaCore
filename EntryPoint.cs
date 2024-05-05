using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.Core;

[ArchiveDependency("dev.gtfomodding.gtfo-api")]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public bool ApplyHarmonyPatches => false;

    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public string ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Hikaria Core");

    public Dictionary<Language, string> ModuleGroupLanguages => new()
    {
        { Language.English, "Hikaria Core" },
        { Language.Chinese, "Hikaria 核心" },
    };

    public void Init()
    {
        Logs.LogMessage("OK");
    }

    public void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
    }

    public void OnLateUpdate()
    {
    }

    public void OnExit()
    {
    }

    internal class Groups
    {
        static Groups()
        {
            Core.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "核心" }, { Language.English, "Core" } });
            Accessibility.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "辅助功能" }, { Language.English, "Accessibility" } });
            Dev.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "辅助功能" }, { Language.English, "Develop" } });
            Fixes.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "修复" }, { Language.English, "Fixes" } });
        }

        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Hikaria Core");
        public static FeatureGroup Accessibility => ModuleGroup.GetOrCreateSubGroup("Accessibility");
        public static FeatureGroup Core => ModuleGroup.GetOrCreateSubGroup("Core");
        public static FeatureGroup Dev => ModuleGroup.GetOrCreateSubGroup("Develop");
        public static FeatureGroup Fixes => ModuleGroup.GetOrCreateSubGroup("Fixes");

    }
}
