using Hikaria.Core.Interfaces;
using Hikaria.Core.Managers;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.Features;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
internal class Bootstrap : Feature, IOnGameDataInited
{
    public override string Name => "Bootstrap";
    public override void Init()
    {
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<ChatManager>();
        LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<Managers.PauseManager>();

        GameEventListener.RegisterSelfInGameEventListener(this);
    }

    [ArchivePatch(typeof(SNet_Player), nameof(SNet_Player.SendAllCustomData))]
    private class SNet_Player__SendAllCustomData__Patch
    {
        private static void Postfix(SNet_Player __instance, SNet_Player toPlayer)
        {
            SNetExt.SendAllCustomData(__instance, toPlayer);
        }
    }

    [ArchivePatch(typeof(global::PauseManager), nameof(global::PauseManager.SetPaused))]
    private class PauseManager__SetPaused__Patch
    {
        private static void Postfix()
        {
            PauseManager.IsPaused = true;
        }
    }

    [ArchivePatch(typeof(global::PauseManager), nameof(global::PauseManager.SetUnpaused))]
    private class PauseManager__SetUnpaused__Patch
    {
        private static void Postfix()
        {
            PauseManager.IsPaused = false;
        }
    }

    public void OnGameDataInited()
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
    }

    private const string CompsObjName = "Hikaria.Core.Comps.obj";

    private static GameObject CompsObj;
}
