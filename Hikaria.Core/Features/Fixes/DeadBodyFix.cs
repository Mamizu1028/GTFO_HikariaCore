using Agents;
using Enemies;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Interfaces;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes;

[EnableFeatureByDefault]
public class DeadBodyFix : Feature
{
    public override string Name => "敌人尸体穿透修复";

    public override string Description => "使攻击可以穿透敌人的尸体";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    public static new IArchiveLogger FeatureLogger {get;set;}

    [FeatureConfig]
    public static DeadBodyFixSettings Settings { get; set; }

    public class DeadBodyFixSettings
    {
        //[FSSlider(100, 500, FSSlider.SliderStyle.FloatPercent, FSSlider.RoundTo.NoDecimal)]
        [FSDisplayName("预测过期时间")]
        [FSDescription("本地预测敌人死亡状态保持时间")]
        [FSTooltip("默认值: 200, 单位: 毫秒ms")]
        public float ExpirationTime { get => s_expirationTime * 1000f; set => s_expirationTime = value / 1000f; }

        [FSHide]
        [FSDisplayName("阻止特殊部位伤害溢出")]
        [FSDescription("启用该选项以消除客户端在使用霰弹类与高射速枪械攻击特殊部位的优势\n" +
            "该选项只影响未启用此功能的玩家\n" +
            "注意这将会导致溢出的伤害被直接丢弃!!!")]
        public bool BlockCustomLimbDamageOverflow { get => s_blockCustomLimbDamageOverflow; set => s_blockCustomLimbDamageOverflow = value; }
    }

    #region Feature Functions
    public override void OnEnable()
    {
        EnemyAPI.OnEnemyReceivedDamage += OnEnemyReceivedDamage;
        EnemyAPI.OnEnemyLimbDestroyed += OnEnemyLimbDestroyed;
        EnemyAPI.OnEnemyDead += OnEnemyDead;
        EnemyAPI.OnEnemySpawned += OnEnemySpawned;
        EnemyAPI.OnEnemyDespawn += OnEnemyDespawn;
        SNetEventAPI.OnMasterChanged += OnMasterChanged;
        CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;

        if (CurrentGameState == (int)eGameStateName.InLevel)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
            {
                if (!s_enemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var data))
                {
                    data = new EnemyDamageableHelper(enemy.Damage);
                    s_enemyDamageableHelpers.Add(enemy.GlobalID, data);
                }
            }
        }
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyReceivedDamage -= OnEnemyReceivedDamage;
        EnemyAPI.OnEnemyLimbDestroyed -= OnEnemyLimbDestroyed;
        EnemyAPI.OnEnemyDead -= OnEnemyDead;
        EnemyAPI.OnEnemySpawned -= OnEnemySpawned;
        EnemyAPI.OnEnemyDespawn -= OnEnemyDespawn;
        SNetEventAPI.OnMasterChanged -= OnMasterChanged;
        CoreAPI.OnPlayerModsSynced -= OnPlayerModsSynced;
        s_enemyDamageableHelpers.Clear();
    }
    #endregion

    #region 事件监听
    private void OnMasterChanged()
    {
        s_masterHasFullDamageSync = CoreAPI.IsPlayerInstalledCore(SNet.Master);
    }

    private void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods)
    {
        s_masterHasFullDamageSync = CoreAPI.IsPlayerInstalledCore(SNet.Master);
    }

    private void OnEnemyReceivedDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data)
    {
        if (!s_enemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var helper) || helper.IsDead)
            return;

        var limbID = data.limbID;
        if (limbID >= 0)
        {
            helper.ReceivedLimbHealth(limbID);
            helper.UpdateLimbHealth(limbID);
        }

        helper.ReceivedHealth();
        helper.UpdateHealth();

        if (data.isKill)
        {
            helper.ReceivedDead();
        }
    }

    private void OnEnemySpawned(EnemyAgent enemy)
    {
        if (!s_enemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var data))
        {
            data = new EnemyDamageableHelper(enemy.Damage);
            s_enemyDamageableHelpers.Add(enemy.GlobalID, data);
        }
        data.ResetState();
    }

    private void OnEnemyDespawn(EnemyAgent enemy)
    {
        s_enemyDamageableHelpers.Remove(enemy.GlobalID);
    }

    private static void OnEnemyLimbDestroyed(Dam_EnemyDamageLimb limb)
    {
        s_enemyDamageableHelpers[limb.m_base.Owner.GlobalID].ReceiveLimbDestroyed(limb.m_limbID);
    }

    private static void OnEnemyDead(EnemyAgent enemy)
    {
        if (s_enemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var data))
        {
            data.ReceivedDead();
        }
    }
    #endregion

    private static bool s_masterHasFullDamageSync = false;
    private static bool s_blockCustomLimbDamageOverflow = true;
    private static pFullDamageData s_fullDamageData = new();
    private static readonly Dictionary<int, EnemyDamageableHelper> s_enemyDamageableHelpers = new();
    private static float s_expirationTime = 0.2f;

    public class EnemyDamageableHelper
    {
        private readonly EnemyDamageableLimbHelper[] _limbHelpers;
        private readonly Dam_EnemyDamageBase _damage;

        private bool _alive = true;
        private bool _isDirty = false;
        private float _nextCheckTime = 0f;

        public bool IsAlive
        {
            get => _alive;
            private set
            {
                if (_alive == value)
                    return;

                _alive = value;

                foreach (var limbHelper in _limbHelpers)
                    limbHelper.UpdatePredictiveAlive(_alive);
            }
        }

        public float Health { get; private set; }

        public bool IsDirty
        {
            get => _isDirty && !IsDead;
            private set
            {
                _isDirty = value;
                if (value)
                    _nextCheckTime = Time.unscaledTime + s_expirationTime;
            }
        }

        public bool IsDead { get; private set; } = false;

        public float LastReceivedHealth { get; private set; } = 0f;

        public EnemyDamageableHelper(Dam_EnemyDamageBase damage)
        {
            _damage = damage;
            _limbHelpers = _damage.DamageLimbs.Select(limb => new EnemyDamageableLimbHelper(limb)).ToArray();
            LastReceivedHealth = damage.Health;
            Health = damage.Health;
            _nextCheckTime = 0f;
            IsDead = false;
            _alive = true;
            _isDirty = false;
        }

        public void ResetState()
        {
            LastReceivedHealth = _damage.Health;
            Health = _damage.Health;
            IsAlive = Health > 0f;
            IsDead = !IsAlive;
            _nextCheckTime = 0f;
            IsDirty = false;

            foreach (var limbHelper in _limbHelpers)
            {
                limbHelper.ResetState();
            }
        }

        public void UpdateLimbHealth(int limbID)
        {
            _limbHelpers[limbID].UpdateLimbHealth();
            IsDirty = true;
        }

        public void UpdateHealth()
        {
            Health = _damage.Health;
            IsAlive = Health > 0f;
            IsDirty = true;
        }

        public void ReceivedLimbHealth(int limbID)
        {
            _limbHelpers[limbID].ReceivedLimbHealth();
        }

        public void ReceivedHealth()
        {
            LastReceivedHealth = _damage.Health;
        }

        public void ReceiveLimbDestroyed(int limbID)
        {
            _limbHelpers[limbID].ReceiveDestroyed();
            IsDirty = true;
        }

        public void ReceivedDead()
        {
            if (!IsDead)
            {
                LastReceivedHealth = 0f;
                Health = 0f;
                _damage.Health = 0f;
                IsDead = true;
                foreach (var limbHelper in _limbHelpers)
                {
                    limbHelper.ReceivedDead();
                }
            }
            _isDirty = false;
        }

        public void CheckExpriation()
        {
            if (Time.unscaledTime > _nextCheckTime)
            {
                if (s_masterHasFullDamageSync)
                {
                    Health = LastReceivedHealth;
                    _damage.Health = LastReceivedHealth;
                    foreach (var limbHelper in _limbHelpers)
                    {
                        if (limbHelper.IsDirty)
                            limbHelper.CheckExpriation();
                    }
                }
                else 
                {
                    if (!IsAlive && !IsDead)
                    {
                        Health = LastReceivedHealth;
                        _damage.Health = LastReceivedHealth;
                        foreach (var limbHelper in _limbHelpers)
                        {
                            limbHelper.CheckExpriation();
                        }
                    }
                }
                _isDirty = false;
            }
        }

        private class EnemyDamageableLimbHelper
        {
            private readonly Dam_EnemyDamageLimb _limb;

            private bool _isDestroyed = false;
            private bool _isDirty = false;

            public EnemyDamageableLimbHelper(Dam_EnemyDamageLimb limb)
            {
                _limb = limb;
                LimbHealth = limb.m_health;
                LastReceivedLimbHealth = limb.m_health;
                _isDirty = false;
            }

            public bool IsDirty => !_isDirty && (_limb.DestructionType != eLimbDestructionType.Custom || !_isDestroyed);

            public bool IsHidden
            {
                get => _limb.gameObject.layer == LayerManager.LAYER_ENEMY_DEAD;
                private set
                {
                    _limb.gameObject.layer = value ? LayerManager.LAYER_ENEMY_DEAD : LayerManager.LAYER_ENEMY_DAMAGABLE;
                }
            }

            public float LimbHealth { get; private set; } = 0f;

            public float LastReceivedLimbHealth { get; private set; } = 0f;

            public void ReceivedDead()
            {
                IsHidden = true;
                _isDirty = false;
            }

            public void ReceiveDestroyed()
            {
                _isDestroyed = true;
                IsHidden = _limb.DestructionType == eLimbDestructionType.Custom;
                _isDirty = true;
            }

            public void ReceivedLimbHealth()
            {
                LastReceivedLimbHealth = _limb.m_health;
                _isDirty = true;
            }

            public void UpdateLimbHealth()
            {
                LimbHealth = _limb.m_health;
                IsHidden = _limb.DestructionType == eLimbDestructionType.Custom && (LimbHealth < 0 || _isDestroyed);
                _isDirty = true;
            }

            public void UpdatePredictiveAlive(bool alive)
            {
                if (!alive)
                    IsHidden = true;
            }

            public void CheckExpriation()
            {
                if (s_masterHasFullDamageSync || IsHidden)
                {
                    LimbHealth = LastReceivedLimbHealth;
                    _limb.m_health = LastReceivedLimbHealth;
                    IsHidden = _limb.DestructionType == eLimbDestructionType.Custom && (LimbHealth < 0 || _isDestroyed);
                }
                _isDirty = false;
            }

            public void ResetState()
            {
                LastReceivedLimbHealth = _limb.m_health;
                _isDestroyed = _limb.IsDestroyed;
                LimbHealth = _limb.m_health;
                IsHidden = _limb.DestructionType == eLimbDestructionType.Custom && (LimbHealth < 0 || _isDestroyed);
                _isDirty = false;
            }
        }
    }

    #region 修改 LayerMask 使子弹可以穿透尸体
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

    #region 过期检测
    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.UpdateEnemyAgent))]
    private class EnemyAgent__UpdateEmemyAgent__Patch
    {
        private static void Postfix(EnemyAgent __instance)
        {
            if (s_enemyDamageableHelpers.TryGetValue(__instance.GlobalID, out var data) && data.IsDirty)
            {
                data.CheckExpriation();
            }
        }
    }
    #endregion

    #region 本地血量更新
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage))]
    private class Dam_EnemyDamageBase__BulletDamage__Patch
    {
        private static Type[] ParameterTypes()
        {
            return new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(int), typeof(float), typeof(float), typeof(uint) };
        }

        // 客户端推测敌人血量并更新 Health, 枪械武器造成的伤害不使用 RoundDamage
        private static void Postfix(Dam_EnemyDamageBase __instance, float dam, int limbID)
        {
            if (SNet.IsMaster)
                return;

            var enemy = __instance.Owner;
            s_fullDamageData.damage.Set(dam, __instance.HealthMax);
            var realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.ProjectileResistance, s_fullDamageData.damage.Get(__instance.HealthMax));
            __instance.DamageLimbs[limbID].DoDamage(realDamage);
            s_enemyDamageableHelpers[enemy.GlobalID].UpdateLimbHealth(limbID);
            __instance.RegisterDamage(realDamage);
            s_enemyDamageableHelpers[enemy.GlobalID].UpdateHealth();
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.MeleeDamage))]
    private class Dam_EnemyDamageBase__MeleeDamage__Patch
    {
        private static Type[] ParameterTypes()
        {
            return new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(DamageNoiseLevel), typeof(uint) };
        }

        // 客户端计算推测敌人血量并更新 Health, 近战武器造成的伤害使用 RoundDamage
        private static void Postfix(Dam_EnemyDamageBase __instance, float dam, int limbID)
        {
            if (SNet.IsMaster)
                return;

            var enemy = __instance.Owner;
            s_fullDamageData.damage.Set(dam, __instance.DamageMax);
            var realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.MeleeResistance, Dam_EnemyDamageBase.RoundDamage(s_fullDamageData.damage.Get(__instance.DamageMax)));
            __instance.DamageLimbs[limbID].DoDamage(realDamage);
            s_enemyDamageableHelpers[enemy.GlobalID].UpdateLimbHealth(limbID);
            __instance.RegisterDamage(realDamage);
            s_enemyDamageableHelpers[enemy.GlobalID].UpdateHealth();
        }
    }
    #endregion

    #region 屏蔽非法伤害
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage), priority: 20000)]
    private class Dam_EnemyDamageBase__ReceiveBulletDamage__Patch
    {
        // 屏蔽特殊部位因网络延迟引起销毁不及时从而出现的多次伤害
        private static void Prefix(Dam_EnemyDamageBase __instance, ref pBulletDamageData data)
        {
            if (!SNet.IsMaster)
                return;

            if (!s_blockCustomLimbDamageOverflow)
                return;

            var limb = __instance.DamageLimbs[data.limbID];
            if (limb.DestructionType != eLimbDestructionType.Custom || !limb.IsDestroyed)
                return;

            data.damage.Set(0f, __instance.HealthMax);
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage), priority: 20000)]
    private class Dam_EnemyDamageBase__ReceiveMeleeDamage__Patch
    {
        // 屏蔽特殊部位因网络延迟引起销毁不及时从而出现的多次伤害
        private static void Prefix(Dam_EnemyDamageBase __instance, ref pFullDamageData data)
        {
            if (!SNet.IsMaster)
                return;

            if (!s_blockCustomLimbDamageOverflow)
                return;

            var limb = __instance.DamageLimbs[data.limbID];
            if (limb.DestructionType != eLimbDestructionType.Custom || !limb.IsDestroyed)
                return;

            data.damage.Set(0f, __instance.DamageMax);
        }
    }
    #endregion
}
