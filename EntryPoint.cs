using TheArchive.Core;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.Core;

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
}
