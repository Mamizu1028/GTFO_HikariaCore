using Agents;
using Enemies;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes;

[EnableFeatureByDefault]
public class DeadBodyFix : Feature
{
    public override string Name => "敌人尸体穿透修复";

    public override string Description => "使攻击可以穿透敌人的尸体";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    [FeatureConfig]
    public static DeadBodyFixSettings Settings { get; set; }

    public class DeadBodyFixSettings
    {
        [FSSlider(0, 500, rounding: FSSlider.RoundTo.NoDecimal)]
        [FSDisplayName("预测过期时间")]
        [FSDescription("本地推算敌人死亡状态保持时间。默认值: 200，单位: 毫秒ms")]
        public float ExpirationTime { get => EnemyDamageableHelper.ExpirationTime; set => EnemyDamageableHelper.ExpirationTime = value; }

        [FSHide]
        [FSDisplayName("阻止特殊部位伤害溢出")]
        [FSDescription("启用该选项以消除客户端在使用霰弹类与高射速枪械攻击特殊部位的优势\n" +
            "该选项只影响未启用尸体穿透修复的玩家\n" +
            "注意这将会导致溢出的伤害被直接丢弃!!!")]
        public bool BlockCustomLimbDamageOverflow { get => _blockCustomLimbDamageOverflow; set => _blockCustomLimbDamageOverflow = value; }
    }

    public override void OnEnable()
    {
        EnemyAPI.OnEnemyHealthReceived += OnEnemyHealthReceived;
        EnemyAPI.OnEnemyLimbHealthReceived += OnEnemyLimbHealthReceived;
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyHealthReceived -= OnEnemyHealthReceived;
        EnemyAPI.OnEnemyLimbHealthReceived -= OnEnemyLimbHealthReceived;
    }

    private static bool _blockCustomLimbDamageOverflow = true;
    private static pFullDamageData s_fullDamageData = new();
    private static Dictionary<int, EnemyDamageableHelper> s_EnemyDamageableHelpers = new();

    // WIP 应当改为自下而上驱动，上层为 DamageBase, 下层为 DamageLimb
    public class EnemyDamageableHelper
    {
        public static float ExpirationTime = 200;

        private readonly List<EnemyDamageableLimbHelper> _limbHelpers = new();
        private readonly Dam_EnemyDamageBase _damage;
        private bool _alive = true;
        private float _health = 0f;
        private bool _isAuthoritativlyDead = false;
        private bool _isDirty = false;
        private float _lastAuthoritativeHealth = 0f;
        private float _lastNonAuthoritativeUpdateTime = 0f;

        public bool IsAlive
        {
            get => _alive;
            private set
            {
                if (_alive == value)
                    return;

                _alive = value;

                if (_alive)
                    _damage.DeathIndicatorShown = false;
            }
        }

        public float Health
        {
            get => _health;
            private set
            {
                _health = value;
                IsAlive = _health > 0f;
            }
        }

        public EnemyDamageableLimbHelper this[int limbID] => _limbHelpers[limbID];

        public bool IsAuthoritativlyDead => _isAuthoritativlyDead;

        public EnemyDamageableHelper(Dam_EnemyDamageBase damage)
        {
            _damage = damage;
            foreach (var limb in _damage.DamageLimbs)
            {
                _limbHelpers.Add(new EnemyDamageableLimbHelper(_damage, limb));
            }
            _lastAuthoritativeHealth = damage.Health;
            _lastNonAuthoritativeUpdateTime = 0f;
        }

        public void UpdateHealth(float health, bool isAuthoritative)
        {
            Health = health;
            if (isAuthoritative)
            {
                _lastAuthoritativeHealth = health;
                return;
            }
            _lastNonAuthoritativeUpdateTime = Time.time;
        }

        public void UpdateLimbHealth(int limbID, float health, bool isAuthoritative)
        {
            _limbHelpers[limbID].UpdateLimbHealth(health, isAuthoritative);
        }

        public void ForceUpdateAliveStatus(bool alive, bool isAuthoritative)
        {
            if (isAuthoritative)
            {
                _isAuthoritativlyDead = !alive;
                _isDirty = false;
            }
            IsAlive = alive;
        }

        public void UpdateExpriation()
        {
            if (!_isDirty || _isAuthoritativlyDead || IsAlive) return;

            if ((Time.time - _lastNonAuthoritativeUpdateTime) * 1000 > ExpirationTime)
            {
                Health = _lastAuthoritativeHealth;
                foreach (var limbHelper in _limbHelpers)
                {
                    limbHelper.UpdateExpiration();
                }
                _isDirty = false;
            }
        }

        public class EnemyDamageableLimbHelper
        {
            private Dam_EnemyDamageBase _base;
            private Dam_EnemyDamageLimb _limb;
            private float _limbHealth = 0f;
            private float _lastAuthoritativeLimbHealth = 0f;
            private float _lastNonAuthoritativeUpdateTime = 0f;
            private bool _isDestroyed = false;
            private bool _isReallyDestroyed = false;
            private bool _isDirty = false;

            public EnemyDamageableLimbHelper(Dam_EnemyDamageBase damageBase, Dam_EnemyDamageLimb limb)
            {
                _base = damageBase;
                _limb = limb;
                _limbHealth = limb.m_health;
                _lastAuthoritativeLimbHealth = limb.m_health;
                _lastNonAuthoritativeUpdateTime = 0f;
            }

            public bool IsReallyDestroyed => _isReallyDestroyed;

            public bool IsDestroyed
            {
                get => _isDestroyed;
                private set
                {
                    if (_isDestroyed == value)
                        return;

                    _isDestroyed = value || _isReallyDestroyed;

                    _limb.gameObject.layer = _isDestroyed ? LayerManager.LAYER_ENEMY_DEAD : LayerManager.LAYER_ENEMY_DAMAGABLE;
                }
            }

            public float LimbHealth
            {
                get => _limbHealth;
                private set
                {
                    _limbHealth = value;

                    IsDestroyed = _limbHealth < 0f;
                }
            }

            public void UpdateLimbDestroyStatus(bool isDestroyed, bool isAuthoritative)
            {
                IsDestroyed = isDestroyed;
                if (isAuthoritative)
                {
                    _isReallyDestroyed = isDestroyed;
                    _isDirty = false;
                    return;
                }
                _isDirty = true;
            }

            public void UpdateLimbHealth(float health, bool isAuthoritative)
            {
                LimbHealth = health;
                if (isAuthoritative)
                {
                    _lastAuthoritativeLimbHealth = health;
                    _isDirty = false;
                    return;
                }
                _lastNonAuthoritativeUpdateTime = Time.time;
            }

            public void UpdateExpiration()
            {
                if (!_isDirty || _isReallyDestroyed || !IsDestroyed) return;

                if ((Time.time - _lastNonAuthoritativeUpdateTime) * 1000 > ExpirationTime)
                {
                    LimbHealth = _lastAuthoritativeLimbHealth;
                    _isDirty = false;
                }
            }
        }
    }

    #region 通过修改 LayerMask 使子弹可以穿透尸体
    [ArchivePatch(typeof(LayerManager), nameof(LayerManager.Setup))]
    private class LayerManager__Setup__Patch
    {
        private static void Postfix(LayerManager __instance)
        {
            LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = __instance.GetMask(new string[] { "EnemyDamagable", "PlayerSynced", "EnemyDead" });
            LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = __instance.GetMask(new string[] { "EnemyDamagable", "Dynamic", "Default", "Default_NoGraph", "Default_BlockGraph" });
        }
    }
    #endregion

    #region 初始化和移除 EnemyDamageableHelper

    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Alive), patchMethodType: ArchivePatch.PatchMethodType.Setter)]
    private class EnemyAgent__set_Alive__Patch
    {
        private static void Postfix(EnemyAgent __instance, bool value)
        {
            if (!value)
                return;

            if (!s_EnemyDamageableHelpers.TryGetValue(__instance.GlobalID, out var data))
            {
                data = new EnemyDamageableHelper(__instance.Damage);
                s_EnemyDamageableHelpers.Add(__instance.GlobalID, data);
            }

            data.UpdateHealth(__instance.Damage.Health, true);
        }
    }

    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDead))]
    private class EnemyAgent__OnDead__Patch
    {
        private static void Prefix(EnemyAgent __instance)
        {
            s_EnemyDamageableHelpers.Remove(__instance.GlobalID);
        }
    }
    #endregion

    #region 非权威血量更新
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage))]
    private class Dam_EnemyDamageBase__BulletDamage__Patch
    {
        private static Type[] ParameterTypes()
        {
            return new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(int), typeof(float), typeof(float), typeof(uint) };
        }

        // 客户端计算推测敌人血量并写入 Health, 最后更新敌人血量 (非权威), 枪械武器造成的伤害不使用 RoundDamage
        private static void Postfix(Dam_EnemyDamageBase __instance, float dam, int limbID)
        {
            if (SNet.IsMaster)
                return;

            var enemy = __instance.Owner;
            s_fullDamageData.damage.Set(dam, __instance.HealthMax);
            var realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.ProjectileResistance, s_fullDamageData.damage.Get(__instance.HealthMax));
            __instance.RegisterDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID].UpdateHealth(enemy.Damage.Health, false);
            if (limbID >= 0)
            {
                var limb = __instance.DamageLimbs[limbID];
                limb.DoDamage(realDamage);
                s_EnemyDamageableHelpers[enemy.GlobalID].UpdateLimbHealth(limbID, limb.m_health, false);
            }
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage))]
    private class Dam_EnemyDamageBase__MeleeDamage__Patch
    {
        private static Type[] ParameterTypes()
        {
            return new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(DamageNoiseLevel), typeof(uint) };
        }

        // 客户端计算推测敌人血量并写入 Health, 最后更新敌人血量 (非权威), 近战武器造成的伤害使用 RoundDamage
        private static void Postfix(Dam_EnemyDamageBase __instance, float dam, int limbID)
        {
            if (SNet.IsMaster)
                return;

            var enemy = __instance.Owner;
            s_fullDamageData.damage.Set(dam, __instance.DamageMax);
            var realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.MeleeResistance, Dam_EnemyDamageBase.RoundDamage(s_fullDamageData.damage.Get(__instance.DamageMax)));
            __instance.RegisterDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID].UpdateHealth(enemy.Damage.Health, false);
            if (limbID >= 0)
            {
                var limb = __instance.DamageLimbs[limbID];
                limb.DoDamage(realDamage);
                s_EnemyDamageableHelpers[enemy.GlobalID].UpdateLimbHealth(limbID, limb.m_health, false);
            }
        }
    }
    #endregion

    #region 权威更新
    [ArchivePatch(typeof(ES_HitreactBase), nameof(ES_HitreactBase.CurrentReactionType), patchMethodType: ArchivePatch.PatchMethodType.Setter)]
    private class ES_HitreactBase__set_CurrentReactionType__Patch
    {
        // 客户端更新敌人状态 (权威)
        private static void Postfix(ES_Hitreact __instance, ES_HitreactType value)
        {
            if (SNet.IsMaster)
                return;

            if (CurrentGameState != (int)eGameStateName.InLevel)
                return;

            if (value == ES_HitreactType.ToDeath)
            {
                s_EnemyDamageableHelpers[__instance.m_enemyAgent.GlobalID].ForceUpdateAliveStatus(false, true);
            }
            else
            {
                s_EnemyDamageableHelpers[__instance.m_enemyAgent.GlobalID].ForceUpdateAliveStatus(true, true);
            }
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        // 屏蔽特殊部位因网络延迟引起销毁不及时从而出现的多次伤害
        private static bool Prefix(Dam_EnemyDamageBase __instance, ref float damage, int limbID)
        {
            if (!SNet.IsMaster)
                return true;

            if (!_blockCustomLimbDamageOverflow || limbID < 0)
                return true;

            var limb = __instance.DamageLimbs[limbID];
            if (!limb.IsDestroyed || limb.DestructionType != eLimbDestructionType.Custom)
                return true;

            damage = 0;
            return false;
        }

        // 主机更新敌人整体血量
        private static void Postfix(Dam_EnemyDamageBase __instance)
        {
            if (!SNet.IsMaster)
                return;

            s_EnemyDamageableHelpers[__instance.Owner.GlobalID].UpdateHealth(__instance.Health, true);
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveDestroyLimb))]
    private class Dam_EnemyDamageBase__ReceiveDestroyLimb__Patch
    {
        // 主机更新敌人部位状态
        private static void Postfix(Dam_EnemyDamageBase __instance, Dam_EnemyDamageBase.pDestroyLimbData data)
        {
            if (!SNet.IsMaster)
                return;

            s_EnemyDamageableHelpers[__instance.Owner.GlobalID][data.limbID].UpdateLimbDestroyStatus(true, true);
        }
    }

    private static void OnEnemyHealthReceived(Dam_EnemyDamageBase damage)
    {
        s_EnemyDamageableHelpers[damage.Owner.GlobalID].UpdateHealth(damage.Health, true);
    }

    private static void OnEnemyLimbHealthReceived(Dam_EnemyDamageLimb limb)
    {
        s_EnemyDamageableHelpers[limb.m_base.Owner.GlobalID].UpdateLimbHealth(limb.m_limbID, limb.m_health, true);
    }
    #endregion

    #region 非权威状态过期检测
    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.UpdateEnemyAgent))]
    private class EnemyAgent__UpdateEmemyAgent__Patch
    {
        // 客户端检查敌人状态
        private static void Postfix(EnemyAgent __instance)
        {
            if (SNet.IsMaster)
                return;

            if (__instance.Alive && __instance.Damage.Health <= 0f)
            {
                if (__instance.Sync.m_enemyStateData.agentMode == AgentMode.Off)
                    return;

                s_EnemyDamageableHelpers[__instance.GlobalID].UpdateExpriation();
            }
        }
    }
    #endregion
}
