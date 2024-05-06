using Hikaria.Core.Managers;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.Features.Core;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class HikariaCoreBootstrap : Feature
{
    public override string Name => "Hikaria Core Bootstrap";

    public override FeatureGroup Group => EntryPoint.Groups.Core;

    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ChatManager>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<GameEventLogManager>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Managers.PauseManager>();
    }

    [ArchivePatch(typeof(SNet_Player), nameof(SNet_Player.SendAllCustomData))]
    private class SNet_Player__SendAllCustomData__Patch
    {
        private static void Postfix(SNet_Player __instance, SNet_Player toPlayer)
        {
            SNetExt.SendAllCustomData(__instance, toPlayer);
        }
    }

    public override void OnGameDataInitialized()
    {
        if (CompsObj == null)
        {
            CompsObj = new(CompsObjName);
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
        Managers.PauseManager.Setup();
    }

    private const string CompsObjName = "Hikaria.Core.Comps.obj";

    private static GameObject CompsObj;
}
