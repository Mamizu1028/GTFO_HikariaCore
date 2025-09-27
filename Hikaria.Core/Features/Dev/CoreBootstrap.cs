using Hikaria.Core.Features.Security;
using Hikaria.Core.Managers;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class CoreBootstrap : Feature
{
    public override string Name => "核心引导";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2CppWithInterfaces<Components.Interact_Base>(false, typeof(IInteractable));
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Components.Interact_Timed>(false);
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Components.Interact_ManualTimedWithCallback>(false);

        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ChatManager>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<GameEventLogManager>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Managers.PauseManager>();
    }

    public override void OnGameDataInitialized()
    {
        if (CompsObj == null)
        {
            CompsObj = new GameObject(CompsObjName);
            UnityEngine.Object.DontDestroyOnLoad(CompsObj);
        }
        if (CompsObj.GetComponent<Managers.PauseManager>() == null)
        {
            CompsObj.AddComponent<Managers.PauseManager>();
        }
        if (CompsObj.GetComponent<ChatManager>() == null)
        {
            CompsObj.AddComponent<ChatManager>();
        }
        if (CompsObj.GetComponent<GameEventLogManager>() == null)
        {
            CompsObj.AddComponent<GameEventLogManager>();
        }
        PopupMessageManager.Setup();

        FeatureManager.EnableAutomatedFeature(typeof(GlobalBan));
    }

    private const string CompsObjName = "Hikaria.Core.Comps.obj";

    private static GameObject CompsObj;
}
