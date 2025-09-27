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

    public new static IArchiveLogger FeatureLogger {get;set;}

    [FeatureConfig]
    public static DeadBodyFixSettings Settings { get; set; }

    public class DeadBodyFixSettings
    {
        [FSSlider(0, 500, FSSlider.SliderStyle.FloatNoDecimal, FSSlider.RoundTo.NoDecimal)]
        [FSDisplayName("预测过期时间")]
        [FSDescription("本地预测敌人死亡状态保持时间")]
        [FSTooltip("默认值: 200, 单位: 毫秒ms")]
        public float ExpirationTime { get => EnemyDamageableHelper.ExpirationTime; set => EnemyDamageableHelper.ExpirationTime = value; }

        [FSIgnore]
        [FSDisplayName("允许本地部位销毁")]
        [FSDescription("允许本地预测进行部位销毁\n\n注意：部位销毁不可逆!!!")]
        public bool EnableLimbDestruction { get => EnemyDamageableHelper.EnableLimbDestruction; set => EnemyDamageableHelper.EnableLimbDestruction = value; }

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
        EnemyAPI.OnEnemyLimbDestroyed += OnEnemyLimbDestroyed;
        EnemyAPI.OnEnemyDead += OnEnemyDead;

        if (CurrentGameState == (int)eGameStateName.InLevel)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
            {
                if (!s_EnemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var data))
                {
                    data = new EnemyDamageableHelper(enemy.Damage);
                    s_EnemyDamageableHelpers.Add(enemy.GlobalID, data);
                }
            }
        }
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyHealthReceived -= OnEnemyHealthReceived;
        EnemyAPI.OnEnemyLimbHealthReceived -= OnEnemyLimbHealthReceived;
        EnemyAPI.OnEnemyLimbDestroyed -= OnEnemyLimbDestroyed;
        EnemyAPI.OnEnemyDead -= OnEnemyDead;

        s_EnemyDamageableHelpers.Clear();
    }

    private static bool _blockCustomLimbDamageOverflow = true;
    private static pFullDamageData s_fullDamageData = new();
    private static Dictionary<int, EnemyDamageableHelper> s_EnemyDamageableHelpers = new();

    public class EnemyDamageableHelper
    {
        public static float ExpirationTime = 200;
        public static bool EnableLimbDestruction = false;

        private readonly EnemyDamageableLimbHelper[] _limbHelpers;
        private readonly Dam_EnemyDamageBase _damage;

        private float _health = 0f;
        private bool _alive = true;
        private bool _isDead = false;
        private bool _isDirty = false;
        private float _lastReceivedHealth = 0f;
        private float _lastLocalUpdateTime = 0f;

        public bool IsAlive
        {
            get => _alive;
            private set
            {
                _alive = value;

                if (_alive)
                {
                    _damage.DeathIndicatorShown = false;
                }
                else
                {
                    for (int i = 0; i < _limbHelpers.Length; i++)
                    {
                        _limbHelpers[i].UpdateEnemyAlive();
                    }
                }
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

        public bool IsHidden
        {
            get => _limbHelpers.All(limb => limb.IsHidden);
        }

        public bool IsDirty => _isDirty && !_isDead;

        public EnemyDamageableLimbHelper this[int limbID] => _limbHelpers[limbID];

        public bool IsDead => _isDead;

        public EnemyDamageableHelper(Dam_EnemyDamageBase damage)
        {
            _damage = damage;
            _limbHelpers = new EnemyDamageableLimbHelper[_damage.DamageLimbs.Count];
            for (int i = 0; i < _damage.DamageLimbs.Count; i++)
            {
                _limbHelpers[i] = new EnemyDamageableLimbHelper(this, _damage.DamageLimbs[i]);
            }
            _lastReceivedHealth = damage.Health;
            _lastLocalUpdateTime = 0f;
        }

        public void UpdateHealth()
        {
            Health = _damage.Health;
            _isDirty = true;
            _lastLocalUpdateTime = Time.unscaledTime;
        }

        public void ReceivedHealth()
        {
            _lastReceivedHealth = _damage.Health;
            if (SNet.IsMaster)
            {
                Health = _damage.Health;
            }
        }

        public void ReceivedDead()
        {
            _isDead = true;
            _isDirty = false;

            for (int i = 0; i < _limbHelpers.Length; i++)
            {
                _limbHelpers[i].ReceivedDead();
            }
        }

        public void UpdateExpriation()
        {
            if ((Time.unscaledTime - _lastLocalUpdateTime) * 1000 > ExpirationTime)
            {
                Health = _lastReceivedHealth;
                _damage.Health = _lastReceivedHealth;
                foreach (var limbHelper in _limbHelpers)
                {
                    if (limbHelper.IsDirty)
                        limbHelper.UpdateExpiration();
                }
                _isDirty = false;
            }
        }

        public class EnemyDamageableLimbHelper
        {
            private readonly EnemyDamageableHelper _baseHelper;
            private readonly Dam_EnemyDamageLimb _limb;
            private readonly eLimbDestructionType _limbDestructionType;

            private float _limbHealth = 0f;
            private float _lastReceivedLimbHealth = 0f;
            private float _lastLocalUpdateTime = 0f;
            private bool _isDestroyed = false;
            private bool _isDirty = false;

            public EnemyDamageableLimbHelper(EnemyDamageableHelper baseHelper, Dam_EnemyDamageLimb limb)
            {
                _baseHelper = baseHelper;
                _limb = limb;
                _limbDestructionType = limb.DestructionType;
                _lastReceivedLimbHealth = limb.m_health;
                _lastLocalUpdateTime = 0f;
            }

            public bool IsDirty => _isDirty && !_isDestroyed;

            public bool IsHidden
            {
                get => _limb.gameObject.layer == LayerManager.LAYER_ENEMY_DEAD;
                private set
                {
                    _limb.gameObject.layer = value ? LayerManager.LAYER_ENEMY_DEAD : LayerManager.LAYER_ENEMY_DAMAGABLE;
                }
            }

            public float LimbHealth
            {
                get => _limbHealth;
                private set
                {
                    _limbHealth = value;
                    if (_limbDestructionType != eLimbDestructionType.Custom)
                        return;

                    IsHidden = _limbHealth < 0 || _limb.IsDestroyed;
                }
            }

            public void ReceivedDead()
            {
                IsHidden = true;
                _isDirty = false;
            }

            public void ReceiveDestroyed()
            {
                IsHidden = true;
                _isDirty = false;
                _isDestroyed = true;
            }

            public void ReceivedHealth()
            {
                _lastReceivedLimbHealth = _limb.m_health;
                if (SNet.IsMaster)
                {
                    LimbHealth = _limb.m_health;
                }
            }

            public void UpdateLimbHealth()
            {
                LimbHealth = _limb.m_health;
                _isDirty = true;
                _lastLocalUpdateTime = Time.unscaledTime;
            }

            public void UpdateEnemyAlive()
            {
                if (_isDestroyed)
                    return;

                if (_baseHelper.IsAlive)
                {
                    IsHidden = true;
                }
                else
                {
                    IsHidden = _limbHealth < 0;
                }
                _isDirty = true;
            }

            public void UpdateExpiration()
            {
                if ((Time.unscaledTime - _lastLocalUpdateTime) * 1000 > ExpirationTime)
                {
                    LimbHealth = _lastReceivedLimbHealth;
                    _limb.m_health = _lastReceivedLimbHealth;
                    _isDirty = false;
                }
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
            data.ReceivedHealth();
        }
    }

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.BulletDamage))]
    private class Dam_EnemyDamageBase__BulletDamage__Patch
    {
        private static Type[] ParameterTypes()
        {
            return new Type[] { typeof(float), typeof(Agent), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(int), typeof(float), typeof(float), typeof(uint) };
        }

        // 客户端推测敌人血量并写入 Health, 最后更新敌人血量 (非权威), 枪械武器造成的伤害不使用 RoundDamage
        private static void Postfix(Dam_EnemyDamageBase __instance, float dam, int limbID)
        {
            if (SNet.IsMaster)
                return;

            var enemy = __instance.Owner;
            s_fullDamageData.damage.Set(dam, __instance.HealthMax);
            var realDamage = AgentModifierManager.ApplyModifier(enemy, AgentModifier.ProjectileResistance, s_fullDamageData.damage.Get(__instance.HealthMax));
            var limb = __instance.DamageLimbs[limbID];
            limb.DoDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID][limbID].UpdateLimbHealth();
            __instance.RegisterDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID].UpdateHealth();
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
            var limb = __instance.DamageLimbs[limbID];
            limb.DoDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID][limbID].UpdateLimbHealth();
            __instance.RegisterDamage(realDamage);
            s_EnemyDamageableHelpers[enemy.GlobalID].UpdateHealth();
        }
    }

    // 此方法在服务端没有任何 DamageSync 时有用
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
                s_EnemyDamageableHelpers[__instance.m_enemyAgent.GlobalID].ReceivedDead();
        }
    }

    private static int _lastDestroyedLimbObjectID;
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ProcessReceivedDamage))]
    private class Dam_EnemyDamageBase__ProcessReceivedDamage__Patch
    {
        // 屏蔽特殊部位因网络延迟引起销毁不及时从而出现的多次伤害
        private static bool Prefix(Dam_EnemyDamageBase __instance, ref float damage, int limbID)
        {
            if (!SNet.IsMaster)
                return ArchivePatch.RUN_OG;

            if (!_blockCustomLimbDamageOverflow)
                return ArchivePatch.RUN_OG;

            var limb = __instance.DamageLimbs[limbID];
            if (limb.DestructionType != eLimbDestructionType.Custom || limb.m_health >= 0)
                return ArchivePatch.RUN_OG;

            if (_lastDestroyedLimbObjectID == limb.gameObject.GetInstanceID())
            {
                _lastDestroyedLimbObjectID = 0;
                return ArchivePatch.RUN_OG;
            }

            damage = 0;
            return ArchivePatch.SKIP_OG;
        }

        // 主机更新敌人整体血量
        private static void Postfix(Dam_EnemyDamageBase __instance, float damage, int limbID)
        {
            if (!SNet.IsMaster)
                return;

            s_EnemyDamageableHelpers[__instance.Owner.GlobalID][limbID].ReceivedHealth();
            s_EnemyDamageableHelpers[__instance.Owner.GlobalID].ReceivedHealth();
        }
    }

    [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.UpdateEnemyAgent))]
    private class EnemyAgent__UpdateEmemyAgent__Patch
    {
        // 客户端检查敌人状态
        private static void Postfix(EnemyAgent __instance)
        {
            if (SNet.IsMaster)
                return;

            if (s_EnemyDamageableHelpers.TryGetValue(__instance.GlobalID, out var data) && data.IsDirty)
            {
                data.UpdateExpriation();
            }
        }
    }

    private static void OnEnemyHealthReceived(Dam_EnemyDamageBase damage)
    {
        if (SNet.IsMaster)
            return;
        s_EnemyDamageableHelpers[damage.Owner.GlobalID].ReceivedHealth();
    }

    private static void OnEnemyLimbHealthReceived(Dam_EnemyDamageLimb limb)
    {
        if (SNet.IsMaster)
            return;
        s_EnemyDamageableHelpers[limb.m_base.Owner.GlobalID][limb.m_limbID].ReceivedHealth();
    }

    private static void OnEnemyLimbDestroyed(Dam_EnemyDamageLimb limb)
    {
        if (SNet.IsMaster)
            _lastDestroyedLimbObjectID = limb.gameObject.GetInstanceID();
        s_EnemyDamageableHelpers[limb.m_base.Owner.GlobalID][limb.m_limbID].ReceiveDestroyed();
    }

    private static void OnEnemyDead(EnemyAgent enemy)
    {
        if (s_EnemyDamageableHelpers.TryGetValue(enemy.GlobalID, out var data))
        {
            data.ReceivedDead();
            s_EnemyDamageableHelpers.Remove(enemy.GlobalID);
        }
    }
}
