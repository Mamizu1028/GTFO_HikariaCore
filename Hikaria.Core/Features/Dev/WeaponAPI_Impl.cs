using Gear;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;
using TheArchive.Utilities;

using static Hikaria.Core.WeaponAPI;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
[HideInModSettings]
internal class WeaponAPI_Impl : Feature
{
    public override string Name => "WeaponAPI Impl";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public static new IArchiveLogger FeatureLogger { get; set; }

    #region Events
    public static event PreBulletWeaponFire OnPreBulletWeaponFire;
    public static event PostBulletWeaponFire OnPostBulletWeaponFire;

    public static event PreShotgunFire OnPreShotgunFire;
    public static event PostShotgunFire OnPostShotgunFire;
    #endregion

    [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
    private class BulletWeapon__Fire__Patch
    {
        private static void Prefix(BulletWeapon __instance, bool resetRecoilSimilarity)
        {
            Utils.SafeInvoke(OnPreBulletWeaponFire, __instance, resetRecoilSimilarity);
        }

        private static void Postfix(BulletWeapon __instance, bool resetRecoilSimilarity)
        {
            Utils.SafeInvoke(OnPostBulletWeaponFire, __instance, resetRecoilSimilarity);
        }
    }

    [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
    private class Shotgun__Fire__Patch
    {
        private static void Prefix(Shotgun __instance, bool resetRecoilSimilarity)
        {
            Utils.SafeInvoke(OnPreShotgunFire, __instance, resetRecoilSimilarity);
        }

        private static void Postfix(Shotgun __instance, bool resetRecoilSimilarity)
        {

            Utils.SafeInvoke(OnPostShotgunFire, __instance, resetRecoilSimilarity);
        }
    }
}
