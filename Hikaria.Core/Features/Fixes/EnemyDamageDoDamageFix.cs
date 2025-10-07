using CharacterDestruction;
using GameEvent;
using Player;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Fixes;

[EnableFeatureByDefault]
[DoNotSaveToConfig]
[DisallowInGameToggle]
internal class EnemyDamageDoDamageFix : Feature
{
    public override string Name => "敌人受到伤害修复";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage), priority: -1000)]
    private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
    {
        private static bool Prefix(Dam_EnemyDamageBase __instance, pBulletDamageData data)
        {
            if (__instance.Owner == null || !__instance.Owner.Alive || __instance.IsImortal)
                return ArchivePatch.SKIP_OG;

            float damage = AgentModifierManager.ApplyModifier(__instance.Owner, AgentModifier.ProjectileResistance,
                data.damage.Get(__instance.HealthMax));
            var hitreactType = ES_HitreactType.Light;
            bool tryForceHitReact = false;
            bool willDamageKill = __instance.WillDamageKill(damage);
            CD_DestructionSeverity serverity;
            if (willDamageKill)
            {
                tryForceHitReact = true;
                hitreactType = ES_HitreactType.Heavy;
                serverity = CD_DestructionSeverity.Death;
            }
            else
            {
                serverity = CD_DestructionSeverity.Severe;
            }
            var direction = data.direction.Value;
            var localPosition = data.localPosition.Get(10f);
            var position = localPosition + __instance.Owner.Position;
            var limb = __instance.DamageLimbs[data.limbID];
            var willLimbKill = limb.DoDamage(damage);
            if (willDamageKill || willLimbKill)
            {
                __instance.CheckDestruction(limb, ref localPosition, ref direction,
                    data.limbID, ref serverity, ref tryForceHitReact, ref hitreactType);
            }
            data.source.TryGet(out var agent);
            __instance.ProcessReceivedDamage(damage, agent, position, direction, hitreactType, tryForceHitReact, 
                data.limbID, data.staggerMulti.Get(10f), DamageNoiseLevel.Normal, data.gearCategoryId);
            return ArchivePatch.SKIP_OG;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage), priority: -1000)]
    private class Dam_EnemyDamageBase__ReceiveMeleeDamage__Patch
    {
        private static bool Prefix(Dam_EnemyDamageBase __instance, pFullDamageData data)
        {
            if (__instance.Owner == null || !__instance.Owner.Alive || __instance.IsImortal)
                return ArchivePatch.SKIP_OG;

            float damage = AgentModifierManager.ApplyModifier(__instance.Owner, AgentModifier.MeleeResistance,
                Dam_EnemyDamageBase.RoundDamage(data.damage.Get(__instance.DamageMax)));
            bool willDamageKill = __instance.WillDamageKill(damage);
            var hitreactType = data.skipLimbDestruction ? ES_HitreactType.None : ES_HitreactType.Unspecified;
            bool tryForceHitReact = false;
            CD_DestructionSeverity serverity;
            if (willDamageKill)
            {
                tryForceHitReact = true;
                hitreactType = ES_HitreactType.Heavy;
                serverity = CD_DestructionSeverity.Death;
            }
            else
            {
                serverity = CD_DestructionSeverity.Severe;
            }
            var direction = data.direction.Value;
            var localPosition = data.localPosition.Get(10f);
            var position = localPosition + __instance.Owner.Position;
            var limb = __instance.DamageLimbs[data.limbID];
            var willLimbKill = limb.DoDamage(damage);
            if ((willDamageKill || willLimbKill) && !data.skipLimbDestruction)
            {
                __instance.CheckDestruction(limb, ref localPosition, ref direction,
                    data.limbID, ref serverity, ref tryForceHitReact, ref hitreactType);
            }
            data.source.TryGet(out var agent);
            if (__instance.ProcessReceivedDamage(damage, agent, position, direction, hitreactType, tryForceHitReact,
                data.limbID, data.staggerMulti.Get(10f), (DamageNoiseLevel)data.damageNoiseLevel, data.gearCategoryId))
            {
                GameEventManager.PostEvent(__instance.Owner.IsScout ? eGameEvent.enemy_scout_dead_from_melee : eGameEvent.enemy_dead_from_melee, null, 0f, string.Empty, null);
            }
            return ArchivePatch.SKIP_OG;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage), priority: -1000)]
    private class Dam_EnemyDamageBase__ReceiveExplosionDamage__Patch
    {
        private static bool Prefix(Dam_EnemyDamageBase __instance, pExplosionDamageData data)
        {
            if (__instance.Owner == null || !__instance.Owner.Alive || __instance.IsImortal)
                return ArchivePatch.SKIP_OG;

            var force = data.force.Get(10f);
            var forceNormalized = force.normalized;
            var hitreactType = ES_HitreactType.Light;
            bool tryForceHitreact = false;
            float damage = data.damage.Get(__instance.HealthMax);
            damage = AgentModifierManager.ApplyModifier(__instance.Owner, AgentModifier.ExplosionResistance, damage);
            bool willDamageKill = __instance.WillDamageKill(damage);
            CD_DestructionSeverity serverity;
            if (willDamageKill)
            {
                tryForceHitreact = true;
                hitreactType = ES_HitreactType.Heavy;
                serverity = CD_DestructionSeverity.Death;
            }
            else
            {
                serverity = CD_DestructionSeverity.Severe;
            }
            var localPosition = data.localPosition.Get(10f);
            int limbID = 0;
            if (__instance.DamageLimbsWithDestruction != null && __instance.DamageLimbsWithDestruction.Count > 0)
            {
                limbID = UnityEngine.Random.Range(0, __instance.DamageLimbsWithDestruction.Count);
                var limb = __instance.DamageLimbsWithDestruction[limbID];
                var willLimbKill = limb.DoDamage(damage);
                if (willDamageKill || willLimbKill)
                {
                    __instance.CheckDestruction(limb, ref localPosition, ref forceNormalized, limbID,
                        ref serverity, ref tryForceHitreact, ref hitreactType);
                }
            }
            else
            {
                var limb = __instance.DamageLimbs[limbID];
                limb.DoDamage(damage);
            }
            var position = localPosition + __instance.Owner.Position;
            __instance.ProcessReceivedDamage(damage, PlayerManager.Current.m_localPlayerAgentInLevel, position,
                force, hitreactType, tryForceHitreact, limbID, 0f, DamageNoiseLevel.Normal, data.gearCategoryId);
            return ArchivePatch.SKIP_OG;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceivePushDamage))]
    private class Dam_EnemyDamageBase__ReceivePushDamage__Patch
    {
        private static void Postfix(Dam_EnemyDamageBase __instance, pFullDamageData data)
        {
            if (__instance.Owner == null || !__instance.Owner.Alive)
                return;
            if (__instance.IsImortal || !__instance.Owner.EnemyBalancingData.CanBePushed)
            {
                data.source.TryGet(out var agent);
                __instance.Owner.RegisterDamageInflictor(agent);
            }
        }
    }
}
