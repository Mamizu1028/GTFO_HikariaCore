using Clonesoft.Json;
using Clonesoft.Json.Converters;
using Clonesoft.Json.Serialization;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Loader;

namespace Hikaria.Core;

[ArchiveDependency("dev.gtfomodding.gtfo-api")]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public bool ApplyHarmonyPatches => true;

    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public string ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Hikaria Core", new()
    {
        { Language.English, "Hikaria Core" },
        { Language.Chinese, "Hikaria 核心" },
    });

    public void Init()
    {
        JsonConvert.DefaultSettings = new(() =>
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver()
            };

            settings.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            settings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            settings.Converters.Add(new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" });
            settings.Converters.Add(new StringEnumConverter());
            settings.ContractResolver = new DefaultContractResolver();
            settings.NullValueHandling = NullValueHandling.Include;

            return settings;
        });

        LoaderWrapper.ClassInjector.RegisterTypeInIl2CppWithInterfaces<Components.Interact_Base>(false, typeof(IInteractable));
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Components.Interact_Timed>(false);
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Components.Interact_ManualTimedWithCallback>(false);

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
            Dev.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "开发者" }, { Language.English, "Developer" } });
            Fixes.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "修复" }, { Language.English, "Fixes" } });
            Security.SetLanguage(new Dictionary<Language, string>() { { Language.Chinese, "安全" }, { Language.English, "Security" } });
        }

        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Hikaria Core");
        public static FeatureGroup Accessibility => ModuleGroup.GetOrCreateSubGroup("Accessibility");
        public static FeatureGroup Core => ModuleGroup.GetOrCreateSubGroup("Core");
        public static FeatureGroup Dev => ModuleGroup.GetOrCreateSubGroup("Developer", true);
        public static FeatureGroup Fixes => ModuleGroup.GetOrCreateSubGroup("Fixes");
        public static FeatureGroup Security => ModuleGroup.GetOrCreateSubGroup("Security");

    }
}
