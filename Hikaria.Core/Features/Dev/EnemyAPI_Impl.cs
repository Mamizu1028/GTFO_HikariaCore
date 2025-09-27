using Enemies;
using SNetwork;
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
        private static SFloat24 s_SFloat24 = new();
        private static float GetFromLowResVector3(ref LowResVector3 vector, float maxValue)
        {
            s_SFloat24.internalValue1 = vector.vector.x.internalValue;
            s_SFloat24.internalValue2 = vector.vector.y.internalValue;
            s_SFloat24.internalValue3 = vector.vector.z.internalValue;
            return s_SFloat24.Get(maxValue);
        }

        private static bool Prefix(Dam_EnemyDamageBase __instance, ref Dam_EnemyDamageBase.pDestroyLimbData data)
        {
            if (data.limbID < __instance.DamageLimbs.Count)
                return ArchivePatch.RUN_OG;

            // 会有超过128个部位的怪吗?
            var limb = __instance.DamageLimbs[byte.MaxValue - data.limbID];
            limb.m_health = GetFromLowResVector3(ref data.destructionEventData.atPos_Local, limb.m_healthMax);

            Utils.SafeInvoke(OnEnemyLimbHealthReceived, limb);
            return ArchivePatch.SKIP_OG;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.DestroyLimb))]
    private class Dam_EnemyDamageLimb__DestroyLimb__Patch
    {
        private static void Postfix(Dam_EnemyDamageLimb __instance)
        {
            Utils.SafeInvoke(OnEnemyLimbDestroyed, __instance);
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.DestroyLimb))]
    private class Dam_EnemyDamageLimb_Custom__DestroyLimb__Patch
    {
        private static void Postfix(Dam_EnemyDamageLimb_Custom __instance)
        {
            Utils.SafeInvoke(OnEnemyLimbDestroyed, __instance);
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

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage))]
    private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
    {
        private static void Prefix()
        {
            _isReceivedDamage = true;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage))]
    private class Dam_EnemyDamageBase__ReceiveMeleeDamage__Patch
    {
        private static void Prefix()
        {
            _isReceivedDamage = true;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage))]
    private class Dam_EnemyDamageBase__ReceiveExplosionDamage__Patch
    {
        private static void Prefix()
        {
            _isReceivedDamage = true;
        }
    }

    private static bool _isReceivedDamage;
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage), priority: 100000)]
    private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        private static void Prefix(Dam_EnemyDamageBase __instance, ref float damage, int limbID)
        {
            // 修复部位血量不会被更新的问题
            if (_isReceivedDamage && (limbID == 0 || __instance.WillDamageKill(damage)))
            {
                _isReceivedDamage = false;
                __instance.DamageLimbs[limbID].DoDamage(damage);
            }
        }
    }
}
