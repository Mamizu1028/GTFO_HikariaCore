using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.Core;

[ArchiveDependency("dev.gtfomodding.gtfo-api", ArchiveDependency.DependencyFlags.HardDependency)]
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
        }

        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Hikaria Core");
        public static FeatureGroup Core => ModuleGroup.GetOrCreateSubGroup("Core");
    }
}
