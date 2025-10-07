using Enemies;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features.Fixes;

[DoNotSaveToConfig]
[DisallowInGameToggle]
[EnableFeatureByDefault]
internal class KillIndicatorFix : Feature
{
    public override string Name => "击杀标记修复";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    public override void OnEnable()
    {
        EnemyAPI.OnEnemyReceivedDamage += OnEnemyReceivedDamage;
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyReceivedDamage -= OnEnemyReceivedDamage;
    }

    private static void OnEnemyReceivedDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data)
    {
        if (SNet.IsMaster)
            return;

        if (data.damageSource.TryGet(out var agent) && agent.IsLocallyOwned)
        {
            if (data.isKill && !enemy.Damage.DeathIndicatorShown)
            {
                GuiManager.CrosshairLayer.ShowDeathIndicator(data.position);
                enemy.Damage.DeathIndicatorShown = true;
            }
            else if (data.damageTraceFlags.HasFlag(DamageTraceFlags.SentryGun))
            {
                var limb = enemy.Damage.DamageLimbs[data.limbID];
                limb.ShowHitIndicator(limb.m_type == eLimbDamageType.Weakspot, data.isKill, data.position, limb.m_armorDamageMulti < 1f);
            }
        }
    }
}
