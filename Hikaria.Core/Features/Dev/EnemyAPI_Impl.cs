using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
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

    public new static IArchiveLogger FeatureLogger { get; set; }

    #region Events
    internal static event Action<Dam_EnemyDamageBase> OnEnemyHealthReceived;
    internal static event Action<Dam_EnemyDamageLimb> OnEnemyLimbHealthReceived;
    #endregion

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
    private static class Dam_EnemyDamageBase__ReceiveSetHealth__Patch
    {
        private static void Postfix(Dam_EnemyDamageBase __instance)
        {
            Utils.SafeInvoke(OnEnemyHealthReceived, __instance);
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveDestroyLimb), priority: 5000)]
    private static class Dam_EnemyDamageBase__ReceiveDestroyLimb__Patch
    {
        private static bool Prefix(Dam_EnemyDamageBase __instance, ref Dam_EnemyDamageBase.pDestroyLimbData data)
        {
            if (SNet.IsMaster || data.limbID >= 0)
                return true;

            var limbID = -data.limbID - 1;
            var limb = __instance.DamageLimbs[limbID];
            limb.m_health = data.destructionEventData.atPos_Local.GetFromLowResVector3(limb.m_healthMax);

            FeatureLogger.Debug($"ReceiveLimbHealth, limbID: {limb.m_limbID}, limbName: {limb.DebugName}, health: {limb.m_health}");

            Utils.SafeInvoke(OnEnemyLimbHealthReceived, __instance);

            return false;
        }
    }
}
