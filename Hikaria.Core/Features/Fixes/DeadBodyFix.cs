using Agents;
using Enemies;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes
{
    [EnableFeatureByDefault]
    public class DeadBodyFix : Feature
    {
        public override string Name => "尸体穿透修复";

        public override string Description => "使子弹可以穿透敌人的尸体";

        public override FeatureGroup Group => EntryPoint.Groups.Fixes;

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage), new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(int), typeof(float), typeof(float), typeof(uint) })]
        private class Dam_EnemyDamageBase__BulletDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance, float dam)
            {
                if (SNet.IsMaster)
                    return;

                if (__instance.RegisterDamage(AgentModifierManager.ApplyModifier(__instance.Owner, AgentModifier.ProjectileResistance, dam)))
                    ChangeEnemyDamagableLayerToDead(__instance.Owner);
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage))]
        private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance)
            {
                if (!SNet.IsMaster)
                    return;

                if (__instance.Health <= 0f)
                    ChangeEnemyDamagableLayerToDead(__instance.Owner);
            }
        }

        [ArchivePatch(typeof(ES_HitreactBase), nameof(ES_HitreactBase.CurrentReactionType), null, ArchivePatch.PatchMethodType.Setter)]
        private class ES_HitreactBase__set_CurrentReactionType__Patch
        {
            private static void Postfix(ES_HitreactBase __instance, ES_HitreactType value)
            {
                if (value != ES_HitreactType.ToDeath || SNet.IsMaster)
                    return;

                ChangeEnemyDamagableLayerToDead(__instance.m_enemyAgent);
            }
        }

        private static void ChangeEnemyDamagableLayerToDead(EnemyAgent enemy)
        {
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                limb.gameObject.layer = LayerManager.LAYER_ENEMY_DEAD;
            }
        }

        [ArchivePatch(typeof(LayerManager), nameof(LayerManager.Setup))]
        private class LayerManager__Setup__Patch
        {
            private static void Postfix(LayerManager __instance)
            {
                LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = __instance.GetMask(new string[] { "EnemyDamagable", "PlayerSynced", "EnemyDead" });
            }
        }
    }
}
