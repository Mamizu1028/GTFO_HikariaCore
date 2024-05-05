using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features.Fixes;

[HideInModSettings]
[AutomatedFeature]
internal class EnemyDamageSync : Feature
{
    public override string Name => "Enemy Damage Sync";

    public override FeatureGroup Group => EntryPoint.Groups.Fixes;

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        private static void Postfix(Dam_EnemyDamageBase __instance)
        {
            if (!SNet.IsMaster) return;

            pSetHealthData data = new();
            data.health.Set(__instance.Health, __instance.HealthMax);
            __instance.m_setHealthPacket.Send(data, SNet_ChannelType.GameNonCritical);
        }
    }
}
