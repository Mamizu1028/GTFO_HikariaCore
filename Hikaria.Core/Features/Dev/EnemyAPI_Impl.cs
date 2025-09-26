using Enemies;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;
using TheArchive.Utilities;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
[HideInModSettings]
internal class EnemyAPI_Impl : Feature
{
    public override string Name => "EnemyAPI Impl";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public new static IArchiveLogger FeatureLogger { get; set; }

    #region Events
    public static event Action<Dam_EnemyDamageBase> OnEnemyHealthReceived;
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbHealthReceived;
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbDestroyed;
    public static event Action<EnemyAgent> OnEnemyDead;
    #endregion

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
    private static class Dam_EnemyDamageBase__ReceiveSetHealth__Patch
    {
        private static void Postfix(Dam_EnemyDamageBase __instance)
        {
            Utils.SafeInvoke(OnEnemyHealthReceived, __instance);
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveDestroyLimb))]
    private static class Dam_EnemyDamageBase__ReceiveDestroyLimb__Patch
    {
        private static bool Prefix(Dam_EnemyDamageBase __instance, ref Dam_EnemyDamageBase.pDestroyLimbData data)
        {
            if (data.limbID <= __instance.DamageLimbs.Count)
                return ArchivePatch.RUN_OG;

            // 会有超过128个部位的怪吗?
            var limb = __instance.DamageLimbs[byte.MaxValue - data.limbID];
            limb.m_health = data.destructionEventData.atPos_Local.GetFromLowResVector3(limb.m_healthMax);

            Utils.SafeInvoke(OnEnemyLimbHealthReceived, __instance);
            return ArchivePatch.SKIP_OG;
        }

        private static void Postfix(Dam_EnemyDamageBase __instance, ref Dam_EnemyDamageBase.pDestroyLimbData data)
        {
            if (data.limbID > __instance.DamageLimbs.Count)
                return;

            var limb = __instance.DamageLimbs[byte.MaxValue - data.limbID];
            Utils.SafeInvoke(OnEnemyLimbDestroyed, limb);
        }
    }

    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDead))]
    private class EnemyAgent__OnDead__Patch
    {
        private static void Prefix(EnemyAgent __instance)
        {
            Utils.SafeInvoke(OnEnemyDead, __instance);
        }
    }
}
