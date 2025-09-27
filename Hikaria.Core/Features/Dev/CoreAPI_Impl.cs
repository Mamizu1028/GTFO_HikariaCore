using Hikaria.Core.SNetworkExt;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[HideInModSettings]
[DoNotSaveToConfig]
internal class CoreAPI_Impl : Feature
{
    public override string Name => "CoreAPI Impl";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    [ArchivePatch(typeof(SNet_Player), nameof(SNet_Player.SendAllCustomData))]
    private class SNet_Player__SendAllCustomData__Patch
    {
        private static void Postfix(SNet_Player __instance, SNet_Player toPlayer)
        {
            SNetExt.SendAllCustomData(__instance, toPlayer);
        }
    }
}
