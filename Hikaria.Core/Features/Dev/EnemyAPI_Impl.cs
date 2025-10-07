using Agents;
using Enemies;
using Gear;
using Hikaria.Core.SNetworkExt;
using Player;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using TheArchive.Interfaces;
using TheArchive.Utilities;
using UnityEngine;
using static Hikaria.Core.EnemyAPI;

namespace Hikaria.Core.Features.Dev;

[EnableFeatureByDefault]
[DisallowInGameToggle]
[DoNotSaveToConfig]
[HideInModSettings]
internal class EnemyAPI_Impl : Feature
{
    public override string Name => "EnemyAPI Impl";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Developer", true);

    public static new IArchiveLogger FeatureLogger { get; set; }

    public override void Init()
    {
        s_enemyReceivedDamageAction = SNetExt_BroadcastAction<pFullEnemyReceivedDamageData>.Create(typeof(pFullEnemyReceivedDamageData).FullName, 
            OnReceivedFullEnemyReceivedDamage, (p) => CoreAPI.IsPlayerInstalledCore(p), SNet_ChannelType.GameReceiveCritical);
        s_enemySetHealthAction = SNetExt_BroadcastAction<pEnemySetHealthData>.Create(typeof(pEnemySetHealthData).FullName,
            OnReceiveEnemySetHealthData, (p) => CoreAPI.IsPlayerInstalledCore(p), SNet_ChannelType.GameReceiveCritical);
    }

    #region Events
    public static event Action<Dam_EnemyDamageLimb> OnEnemyLimbDestroyed;
    public static event Action<EnemyAgent> OnEnemyDead;
    public static event EnemyReceivedDamage OnEnemyReceivedDamage;
    public static event Action<EnemyAgent> OnEnemySpawned;
    public static event Action<EnemyAgent> OnEnemyDespawn;
    #endregion

    private static void OnReceivedFullEnemyReceivedDamage(SNet_Player sender, pFullEnemyReceivedDamageData data)
    {
        if (!sender.IsMaster)
            return;

        if (!data.enemy.TryGet(out var enemy))
            return;

        Utils.SafeInvoke(OnEnemyReceivedDamage, enemy, data);
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

    [ArchivePatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
    private class EnemySync__OnSpawn__Patch
    {
        private static void Postfix(EnemySync __instance)
        {
            Utils.SafeInvoke(OnEnemySpawned, __instance.m_agent);
        }
    }

    [ArchivePatch(typeof(EnemySync), nameof(EnemySync.OnDespawn))]
    private class EnemySync__OnDespawn__Patch
    {
        private static void Prefix(EnemySync __instance)
        {
            Utils.SafeInvoke(OnEnemyDespawn, __instance.m_agent);
        }
    }

    [ArchivePatch(typeof(ES_HitreactBase), nameof(ES_HitreactBase.CurrentReactionType), patchMethodType: ArchivePatch.PatchMethodType.Setter)]
    private class ES_HitreactBase__set_CurrentReactionType__Patch
    {
        private static void Postfix(ES_Hitreact __instance, ES_HitreactType value)
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
                return;

            if (value == ES_HitreactType.ToDeath)
                Utils.SafeInvoke(OnEnemyDead, __instance);
        }
    }

    [ArchivePatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.FireBullet))]
    private class SentryGunInstance_Firing_Bullets__FireBullet__Patch
    {
        private static void Prefix(SentryGunInstance_Firing_Bullets __instance)
        {
            s_tempGearIDRange = __instance.m_core?.GearIDRange;
            s_damageSourceFlags |= DamageTraceFlags.SentryGun;
        }

        private static void Postfix()
        {
            s_damageSourceFlags &= ~DamageTraceFlags.SentryGun;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage), priority: 100000)]
    private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
    {
        private static void Prefix(Dam_EnemyDamageBase __instance)
        {
            s_damageSourceFlags |= DamageTraceFlags.Bullet;
        }

        private static void Postfix()
        {
            s_damageSourceFlags &= ~DamageTraceFlags.Bullet;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage), priority: 100000)]
    private class Dam_EnemyDamageBase__ReceiveMeleeDamage__Patch
    {
        private static void Prefix()
        {
            s_damageSourceFlags |= DamageTraceFlags.Melee;
        }

        private static void Postfix()
        {
            s_damageSourceFlags &= ~DamageTraceFlags.Melee;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveExplosionDamage), priority: 100000)]
    private class Dam_EnemyDamageBase__ReceiveExplosionDamage__Patch
    {
        private static void Prefix()
        {
            s_damageSourceFlags |= DamageTraceFlags.Explosion;
        }

        private static void Postfix()
        {
            s_damageSourceFlags &= ~DamageTraceFlags.Explosion;
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage), priority: -10000)]
    private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        private static void Postfix(Dam_EnemyDamageBase __instance, bool __result, float damage, Agent damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact = false, int limbID = -1, float staggerDamageMulti = 1f, DamageNoiseLevel damageNoiseLevel = DamageNoiseLevel.Normal, uint gearCategoryId = 0u)
        {
            if (!SNet.IsMaster)
                return;
            
            if (!__result) // 如果敌人依然存活则同步血量
            {
                if (limbID >= 0)
                {
                    var limb = __instance.DamageLimbs[limbID];
                    SendEnemyHealth(__instance.Owner, limb);
                }
                SendEnemyHealth(__instance.Owner);
            }

            if (damageSource != null)
            {
                s_damageSourceFlags |= damageSource.Type switch
                {
                    AgentType.Player => DamageTraceFlags.Player,
                    AgentType.SentryGun => DamageTraceFlags.SentryGun,
                    AgentType.Decoy => DamageTraceFlags.Decoy,
                    AgentType.Enemy => DamageTraceFlags.Enemy,
                    _ => DamageTraceFlags.Unknown
                };
            }

            if (s_tempGearIDRange == null)
            {
                if (s_damageSourceFlags.HasFlag(DamageTraceFlags.Player))
                {
                    if (!s_damageSourceFlags.HasFlag(DamageTraceFlags.SentryGun))
                    {
                        var playerAgent = damageSource.Cast<PlayerAgent>();
                        if (s_damageSourceFlags.HasFlag(DamageTraceFlags.Melee))
                        {
                            if (PlayerBackpackManager.TryGetItem(playerAgent.Owner, InventorySlot.GearMelee, out var item))
                                s_tempGearIDRange = item.GearIDRange;
                        }
                        else if (s_damageSourceFlags.HasFlag(DamageTraceFlags.Bullet))
                        {
                            var wieldSlot = playerAgent.Inventory.WieldedSlot;
                            if (wieldSlot != InventorySlot.GearStandard && wieldSlot != InventorySlot.GearSpecial)
                                wieldSlot = InventorySlot.GearStandard;
                            if (PlayerBackpackManager.TryGetItem(playerAgent.Owner, wieldSlot, out var item))
                                s_tempGearIDRange = item.GearIDRange;
                        }
                    }
                }
            }

            var gearChecksum = s_tempGearIDRange?.GetChecksum() ?? 0u;

            SendEnemyReceivedDamageData(__instance.Owner, __result, damage, damageSource, position, direction, hitreact, 
                tryForceHitreact, limbID, 
                staggerDamageMulti, damageNoiseLevel, gearCategoryId, gearChecksum, s_damageSourceFlags);
        }

        private static void Finalizer()
        {
            s_damageSourceFlags = DamageTraceFlags.None;
            s_tempGearIDRange = null;
        }
    }

    private static void OnReceiveEnemySetHealthData(SNet_Player sender, pEnemySetHealthData data)
    {
        if (SNet.IsMaster || !sender.IsMaster)
            return;

        if (!data.enemy.TryGet(out var enemy))
            return;

        if (data.limbID == -1)
        {
            enemy.Damage.Health = data.health;
            return;
        }

        var limb = enemy.Damage.DamageLimbs[data.limbID];
        limb.m_health = data.health;
    }

    private static void SendEnemyReceivedDamageData(EnemyAgent enemy, bool isKill, float damage, Agent damageSource, Vector3 position, Vector3 direction, ES_HitreactType hitreact, bool tryForceHitreact = false, int limbID = -1, float staggerDamageMulti = 1f, DamageNoiseLevel damageNoiseLevel = DamageNoiseLevel.Normal, uint gearCategoryId = 0u, uint gearChecksum = 0u, DamageTraceFlags damageTraceFlags = DamageTraceFlags.Unknown)
    {
        s_fullEnemyReceivedDamageData.enemy.Set(enemy);
        s_fullEnemyReceivedDamageData.damage = damage;
        s_fullEnemyReceivedDamageData.isKill = isKill;
        s_fullEnemyReceivedDamageData.damageSource.Set(damageSource);
        s_fullEnemyReceivedDamageData.position = position;
        s_fullEnemyReceivedDamageData.direction = direction;
        s_fullEnemyReceivedDamageData.hitreact = hitreact;
        s_fullEnemyReceivedDamageData.limbID = limbID;
        s_fullEnemyReceivedDamageData.staggerDamageMulti = staggerDamageMulti;
        s_fullEnemyReceivedDamageData.damageNoiseLevel = damageNoiseLevel;
        s_fullEnemyReceivedDamageData.gearCategoryId = gearCategoryId;
        s_fullEnemyReceivedDamageData.gearChecksum = gearChecksum;
        s_fullEnemyReceivedDamageData.damageTraceFlags = damageTraceFlags;
        s_enemyReceivedDamageAction.Do(s_fullEnemyReceivedDamageData);
    }

    private static void SendEnemyHealth(EnemyAgent enemy, Dam_EnemyDamageLimb limb = null)
    {
        s_enemySetHealthData.enemy.Set(enemy);
        if (limb == null)
        {
            s_enemySetHealthData.health = enemy.Damage.Health;
            s_enemySetHealthData.limbID = -1;
        }
        else
        {
            s_enemySetHealthData.health = limb.m_health;
            s_enemySetHealthData.limbID = limb.m_limbID;
        }

        s_enemySetHealthAction.Do(s_enemySetHealthData);
    }

    private static DamageTraceFlags s_damageSourceFlags = DamageTraceFlags.None;
    private static GearIDRange s_tempGearIDRange = null;
    private static pEnemySetHealthData s_enemySetHealthData = new();
    private static SNetExt_BroadcastAction<pEnemySetHealthData> s_enemySetHealthAction;
    private static pFullEnemyReceivedDamageData s_fullEnemyReceivedDamageData = new();
    private static SNetExt_BroadcastAction<pFullEnemyReceivedDamageData> s_enemyReceivedDamageAction;

    private struct pEnemySetHealthData
    {
        public pEnemyAgent enemy = new();
        public float health = 0f;
        public int limbID = -1;

        public pEnemySetHealthData(EnemyAgent enemy, float health, int limbID)
        {
            this.enemy.Set(enemy);
            this.health = health;
            this.limbID = limbID;
        }
    }
}
