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
        public override string Name => "敌人尸体穿透修复";

        public override string Description => "使攻击可以穿透敌人的尸体";

        public override FeatureGroup Group => EntryPoint.Groups.Fixes;

        private static Dictionary<uint, bool> EnemyDeathLookup = new();

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage), new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(int), typeof(float), typeof(float), typeof(uint) })]
        private class Dam_EnemyDamageBase__BulletDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance, float dam)
            {
                if (SNet.IsMaster)
                    return;

                var enemy = __instance.Owner;
                if (__instance.RegisterDamage(AgentModifierManager.ApplyModifier(enemy, AgentModifier.ProjectileResistance, dam)))
                {
                    EnemyDeathLookup[enemy.GlobalID] = true;
                    ChangeEnemyDamagableLayer(enemy, LayerManager.LAYER_ENEMY_DEAD);
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage), new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(DamageNoiseLevel), typeof(uint) })]
        private class Dam_EnemyDamageBase__MeleeDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance, float dam, float sleeperMulti)
            {
                if (SNet.IsMaster)
                    return;

                var enemy = __instance.Owner;
                float realDam = AgentModifierManager.ApplyModifier(enemy, AgentModifier.MeleeResistance, dam);
                if (enemy.Locomotion.CurrentStateEnum == ES_StateEnum.Hibernate)
                    realDam *= sleeperMulti;
                if (__instance.RegisterDamage(realDam))
                {
                    EnemyDeathLookup[enemy.GlobalID] = true;
                    ChangeEnemyDamagableLayer(enemy, LayerManager.LAYER_ENEMY_DEAD);
                }
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveSetHealth))]
        private class Dam_EnemyDamageBase__ReceiveSetHealth__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance, pSetHealthData data)
            {
                if (SNet.IsMaster)
                    return;

                var id = __instance.Owner.GlobalID;
                if (__instance.Health <= 0f)
                {
                    EnemyDeathLookup[id] = true;
                    ChangeEnemyDamagableLayer(__instance.Owner, LayerManager.LAYER_ENEMY_DEAD);
                }
                else
                {
                    EnemyDeathLookup[id] = false;
                    ChangeEnemyDamagableLayer(__instance.Owner, LayerManager.LAYER_ENEMY_DAMAGABLE);
                }

            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
        private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageBase __instance)
            {
                if (!SNet.IsMaster)
                    return;

                if (__instance.Health <= 0f)
                    ChangeEnemyDamagableLayer(__instance.Owner, LayerManager.LAYER_ENEMY_DEAD);
            }
        }

        [ArchivePatch(typeof(ES_HitreactBase), nameof(ES_HitreactBase.CurrentReactionType), null, ArchivePatch.PatchMethodType.Setter)]
        private class ES_HitreactBase__set_CurrentReactionType__Patch
        {
            private static void Postfix(ES_HitreactBase __instance, ES_HitreactType value)
            {
                if (SNet.IsMaster)
                    return;

                if (CurrentGameState < (int)eGameStateName.InLevel)
                    return;

                var enemy = __instance.m_enemyAgent;
                var id = enemy.GlobalID;
                if (value == ES_HitreactType.ToDeath)
                {
                    EnemyDeathLookup[id] = true;
                    ChangeEnemyDamagableLayer(enemy, LayerManager.LAYER_ENEMY_DEAD);
                }
                else if (EnemyDeathLookup.TryGetValue(id, out var isDead) && isDead)
                {
                    EnemyDeathLookup[id] = false;
                    ChangeEnemyDamagableLayer(enemy, LayerManager.LAYER_ENEMY_DAMAGABLE);
                }
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.UpdateEnemyAgent))]
        private class EnemyAgent__UpdateEmemyAgent__Patch
        {
            private static void Postfix(EnemyAgent __instance)
            {
                if (SNet.IsMaster)
                    return;

                if (__instance.Alive && __instance.Damage.Health <= 0f)
                {
                    if (__instance.Sync.m_enemyStateData.agentMode == AgentMode.Off)
                        return;

                    var id = __instance.GlobalID;
                    if (EnemyDeathLookup.TryGetValue(id, out var isDead) && isDead)
                    {
                        EnemyDeathLookup[id] = false;
                        ChangeEnemyDamagableLayer(__instance, LayerManager.LAYER_ENEMY_DAMAGABLE);
                    }
                }
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Enable))]
        private class EnemyAgent__Enable__Patch
        {
            private static void Prefix(EnemyAgent __instance)
            {
                var id = __instance.GlobalID;
                if (EnemyDeathLookup.TryGetValue(id, out var isDead) && isDead)
                {
                    EnemyDeathLookup[id] = false;
                    ChangeEnemyDamagableLayer(__instance, LayerManager.LAYER_ENEMY_DAMAGABLE);
                }
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDestroy))]
        private class EnemyAgent__OnDestroy__Patch
        {
            private static void Prefix(EnemyAgent __instance)
            {
                EnemyDeathLookup.Remove(__instance.GlobalID);
            }
        }

        private static void ChangeEnemyDamagableLayer(EnemyAgent enemy, int layer)
        {
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                limb.gameObject.layer = layer;
            }
        }

        [ArchivePatch(typeof(LayerManager), nameof(LayerManager.Setup))]
        private class LayerManager__Setup__Patch
        {
            private static void Postfix(LayerManager __instance)
            {
                LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = __instance.GetMask(new string[] { "EnemyDamagable", "PlayerSynced", "EnemyDead" });
                LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = __instance.GetMask(new string[] { "EnemyDamagable", "Dynamic", "Default", "Default_NoGraph", "Default_BlockGraph" });
            }
        }
    }
}
