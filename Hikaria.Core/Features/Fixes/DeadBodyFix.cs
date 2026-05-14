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
        //[FSSlider(100, 500, FSSlider.SliderStyle.FloatNoDecimal)]
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
        SNetEventAPI.OnMasterChanged += OnMasterChanged;
        CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;

        UpdateMasterDamageSyncState();

        if (CurrentGameState == (int)eGameStateName.InLevel)
        {
            foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
            {
                s_predictionStore.Register(enemy);
            }
        }
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyReceivedDamage -= OnEnemyReceivedDamage;
        EnemyAPI.OnEnemyLimbDestroyed -= OnEnemyLimbDestroyed;
        EnemyAPI.OnEnemyDead -= OnEnemyDead;
        EnemyAPI.OnEnemySpawned -= OnEnemySpawned;
        SNetEventAPI.OnMasterChanged -= OnMasterChanged;
        CoreAPI.OnPlayerModsSynced -= OnPlayerModsSynced;

        s_predictionStore.RestoreAll();
        s_layerPolicy.RestoreAll();
    }
    #endregion

    #region 事件监听
    private void OnMasterChanged()
    {
        UpdateMasterDamageSyncState();
    }

    private void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods)
    {
        UpdateMasterDamageSyncState();
    }

    private static void UpdateMasterDamageSyncState()
    {
        s_predictionStore.MasterHasFullDamageSync = CoreAPI.IsPlayerInstalledCore(SNet.Master);
    }

    private void OnEnemyReceivedDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data)
    {
        s_predictionStore.ApplyAuthoritativeDamage(enemy, data);
    }

    private void OnEnemySpawned(EnemyAgent enemy)
    {
        s_predictionStore.Register(enemy);
    }

    private void OnEnemyLimbDestroyed(Dam_EnemyDamageLimb limb)
    {
        s_predictionStore.ApplyAuthoritativeLimbDestroyed(limb);
    }

    private void OnEnemyDead(EnemyAgent enemy)
    {
        s_predictionStore.ApplyAuthoritativeDeath(enemy);
        s_predictionStore.Forget(enemy);
        s_layerPolicy.Forget(enemy);
    }
    #endregion

    private static float s_expirationTime = 0.2f;
    private static bool s_blockCustomLimbDamageOverflow = true;
    private static readonly CorpsePierceLayerPolicy s_layerPolicy = new();
    private static readonly EnemyDamagePredictionStore s_predictionStore = new(s_layerPolicy, () => s_expirationTime);

    #region Helpers
    internal sealed class EnemyDamagePredictionStore
    {
        private sealed class EnemyPredictionState
        {
            public EnemyAgent Enemy { get; }
            public Dam_EnemyDamageBase Damage { get; }
            public LimbPredictionState[] Limbs { get; }
            public float AuthoritativeHealth { get; private set; }
            public bool IsAuthorityDead { get; private set; }
            public bool HasPrediction { get; private set; }
            public bool IsPredictedDead { get; private set; }
            public float PredictionExpiresAt { get; private set; }

            public EnemyPredictionState(EnemyAgent enemy)
            {
                Enemy = enemy;
                Damage = enemy.Damage;
                Limbs = Damage.DamageLimbs.Select(limb => new LimbPredictionState(limb)).ToArray();
                CaptureAuthoritativeFromGame();
            }

            public void CaptureAuthoritativeFromGame()
            {
                AuthoritativeHealth = Damage.Health;
                for (int i = 0; i < Limbs.Length; i++)
                {
                    Limbs[i].CaptureAuthoritativeFromGame();
                }
            }

            public void CaptureAuthoritativeFromGame(int limbID)
            {
                AuthoritativeHealth = Damage.Health;

                if (TryGetLimb(limbID, out var limbState))
                    limbState.CaptureAuthoritativeFromGame();
            }

            public void MarkPrediction(float expiresAt)
            {
                HasPrediction = true;
                PredictionExpiresAt = expiresAt;
            }

            public void MarkPredictedDead()
            {
                HasPrediction = true;
                IsPredictedDead = true;
            }

            public void MarkAuthorityDead()
            {
                IsAuthorityDead = true;
                AuthoritativeHealth = 0f;
                Damage.Health = 0f;
                ClearPredictionFlags();
            }

            public void ClearPredictionFlags()
            {
                HasPrediction = false;
                IsPredictedDead = false;
                PredictionExpiresAt = 0f;

                for (int i = 0; i < Limbs.Length; i++)
                {
                    Limbs[i].ClearPredictionFlags();
                }
            }

            public void RestoreAuthoritative(bool restoreAllLimbs)
            {
                Damage.Health = AuthoritativeHealth;

                for (int i = 0; i < Limbs.Length; i++)
                {
                    if (restoreAllLimbs || Limbs[i].HasPrediction)
                        Limbs[i].RestoreAuthoritative();
                }
            }

            public bool TryGetLimb(int limbID, out LimbPredictionState limbState)
            {
                limbState = null;

                if (limbID < 0 || limbID >= Limbs.Length)
                    return false;

                limbState = Limbs[limbID];
                return limbState.Limb != null;
            }
        }

        private sealed class LimbPredictionState
        {
            public Dam_EnemyDamageLimb Limb { get; }
            public float AuthoritativeHealth { get; private set; }
            public bool HasPrediction { get; private set; }
            public bool HasPredictedCustomDestroyed { get; private set; }

            public LimbPredictionState(Dam_EnemyDamageLimb limb)
            {
                Limb = limb;
                CaptureAuthoritativeFromGame();
            }

            public void CaptureAuthoritativeFromGame()
            {
                AuthoritativeHealth = Limb.m_health;
            }

            public void MarkPredicted()
            {
                HasPrediction = true;
            }

            public void MarkPredictedCustomDestroyed()
            {
                HasPrediction = true;
                HasPredictedCustomDestroyed = true;
            }

            public void ClearPredictionFlags()
            {
                HasPrediction = false;
                HasPredictedCustomDestroyed = false;
            }

            public void RestoreAuthoritative()
            {
                Limb.m_health = AuthoritativeHealth;
            }
        }

        private readonly Dictionary<int, EnemyPredictionState> _states = new();
        private readonly CorpsePierceLayerPolicy _layerPolicy;
        private readonly Func<float> _expirationTimeProvider;

        public bool MasterHasFullDamageSync { get; set; }

        public EnemyDamagePredictionStore(CorpsePierceLayerPolicy layerPolicy, Func<float> expirationTimeProvider)
        {
            _layerPolicy = layerPolicy;
            _expirationTimeProvider = expirationTimeProvider;
        }

        public void Register(EnemyAgent enemy)
        {
            if (!CanTrack(enemy))
                return;

            _states[enemy.GlobalID] = new EnemyPredictionState(enemy);
        }

        public void Forget(EnemyAgent enemy)
        {
            if (enemy == null)
                return;

            _states.Remove(enemy.GlobalID);
        }

        public void ApplyLocalDamagePrediction(EnemyAgent enemy, int limbID, float damageAmount)
        {
            if (damageAmount <= 0f)
                return;

            if (!TryGetOrRegister(enemy, out var state) || state.IsAuthorityDead)
                return;

            if (!state.HasPrediction)
                state.CaptureAuthoritativeFromGame();

            if (state.TryGetLimb(limbID, out var limbState))
            {
                var limb = limbState.Limb;
                bool wasCustomDestroyed = limb.DestructionType == eLimbDestructionType.Custom && limb.IsDestroyed;
                bool limbKilled = limb.DoDamage(damageAmount);

                limbState.MarkPredicted();

                bool isCustomDestroyed = limb.DestructionType == eLimbDestructionType.Custom
                    && (limb.IsDestroyed || limb.m_health < 0f || limbKilled);

                if (!wasCustomDestroyed && isCustomDestroyed)
                {
                    limbState.MarkPredictedCustomDestroyed();
                    _layerPolicy.MarkPredictedCustomLimbDestroyed(limb);
                }
            }

            state.Damage.RegisterDamage(damageAmount);
            state.MarkPrediction(Time.unscaledTime + _expirationTimeProvider());

            if (state.Damage.Health <= 0f)
            {
                state.MarkPredictedDead();
                _layerPolicy.MarkEnemyPredictedDead(enemy);
            }
        }

        public void ApplyAuthoritativeDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data)
        {
            if (!TryGetOrRegister(enemy, out var state))
                return;

            if (data.isKill)
            {
                ApplyAuthoritativeDeath(enemy);
                return;
            }

            state.CaptureAuthoritativeFromGame(data.limbID);

            if (MasterHasFullDamageSync)
                state.RestoreAuthoritative(restoreAllLimbs: false);

            _layerPolicy.ClearEnemyPredictedDead(enemy);
            _layerPolicy.ClearPredictedCustomLimbDestroyed(enemy);
            state.ClearPredictionFlags();
        }

        public void ApplyAuthoritativeDeath(EnemyAgent enemy)
        {
            if (!TryGetOrRegister(enemy, out var state))
                return;

            state.MarkAuthorityDead();
            _layerPolicy.ClearEnemyPredictedDead(enemy);
            _layerPolicy.MarkEnemyConfirmedDead(enemy);
        }

        public void ApplyAuthoritativeLimbDestroyed(Dam_EnemyDamageLimb limb)
        {
            if (limb == null || limb.m_base == null || limb.m_base.Owner == null)
                return;

            var enemy = limb.m_base.Owner;

            if (!TryGetOrRegister(enemy, out var state))
                return;

            if (!state.TryGetLimb(limb.m_limbID, out var limbState))
                return;

            limbState.CaptureAuthoritativeFromGame();
            limbState.ClearPredictionFlags();

            if (limb.DestructionType == eLimbDestructionType.Custom)
                _layerPolicy.ConfirmCustomLimbDestroyed(limb);
        }

        public void CheckExpiration(EnemyAgent enemy)
        {
            if (enemy == null)
                return;

            if (!_states.TryGetValue(enemy.GlobalID, out var state))
                return;

            if (!state.HasPrediction || state.IsAuthorityDead || Time.unscaledTime <= state.PredictionExpiresAt)
                return;

            if (MasterHasFullDamageSync || state.IsPredictedDead)
            {
                state.RestoreAuthoritative(restoreAllLimbs: MasterHasFullDamageSync);
                _layerPolicy.ClearEnemyPredictedDead(enemy);
            }

            _layerPolicy.ClearPredictedCustomLimbDestroyed(enemy);
            state.ClearPredictionFlags();
        }

        public void RestoreAll()
        {
            foreach (var state in _states.Values)
            {
                if (state.HasPrediction && !state.IsAuthorityDead)
                    state.RestoreAuthoritative(restoreAllLimbs: true);
            }

            _states.Clear();
        }

        private bool TryGetOrRegister(EnemyAgent enemy, out EnemyPredictionState state)
        {
            state = null;

            if (!CanTrack(enemy))
                return false;

            if (!_states.TryGetValue(enemy.GlobalID, out state))
            {
                state = new EnemyPredictionState(enemy);
                _states.Add(enemy.GlobalID, state);
            }

            return true;
        }

        private static bool CanTrack(EnemyAgent enemy)
        {
            return enemy != null && enemy.Damage != null && enemy.Damage.DamageLimbs != null;
        }
    }

    internal static class EnemyDamageEstimator
    {
        public static float EstimateBulletDamage(Dam_EnemyDamageBase damageBase, float rawDamage)
        {
            if (damageBase == null || damageBase.Owner == null)
                return 0f;

            var fullDamageData = new pFullDamageData();
            fullDamageData.damage.Set(rawDamage, damageBase.HealthMax);

            return AgentModifierManager.ApplyModifier(
                damageBase.Owner,
                AgentModifier.ProjectileResistance,
                fullDamageData.damage.Get(damageBase.HealthMax));
        }

        public static float EstimateMeleeDamage(Dam_EnemyDamageBase damageBase, float rawDamage)
        {
            if (damageBase == null || damageBase.Owner == null)
                return 0f;

            var fullDamageData = new pFullDamageData();
            fullDamageData.damage.Set(rawDamage, damageBase.DamageMax);

            var roundedDamage = Dam_EnemyDamageBase.RoundDamage(fullDamageData.damage.Get(damageBase.DamageMax));

            return AgentModifierManager.ApplyModifier(
                damageBase.Owner,
                AgentModifier.MeleeResistance,
                roundedDamage);
        }
    }

    internal sealed class CorpsePierceLayerPolicy
    {
        [Flags]
        private enum LimbLayerOverrideReason
        {
            None = 0,
            PredictedDead = 1,
            ConfirmedDead = 2,
            PredictedCustomLimbDestroyed = 4,
            CustomLimbDestroyed = 8
        }

        private sealed class LimbLayerOverride
        {
            public Dam_EnemyDamageLimb Limb { get; }
            public int LimbID { get; }
            public int OriginalLayer { get; }
            public LimbLayerOverrideReason Reasons { get; private set; }

            public LimbLayerOverride(Dam_EnemyDamageLimb limb, int limbID)
            {
                Limb = limb;
                LimbID = limbID;
                OriginalLayer = limb.gameObject.layer;
                Reasons = LimbLayerOverrideReason.None;
            }

            public void AddReason(LimbLayerOverrideReason reason)
            {
                Reasons |= reason;
            }

            public void RemoveReason(LimbLayerOverrideReason reason)
            {
                Reasons &= ~reason;
            }

            public void SetReasons(LimbLayerOverrideReason reasons)
            {
                Reasons = reasons;
            }
        }

        private readonly Dictionary<int, List<LimbLayerOverride>> _enemyOverrides = new();
        private int _originalPiercingMask;
        private bool _hasOriginalPiercingMask;

        public void ApplyPiercingMask(LayerManager layerManager)
        {
            if (layerManager == null)
                return;

            if (!_hasOriginalPiercingMask)
            {
                _originalPiercingMask = LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;
                _hasOriginalPiercingMask = true;
            }

            LayerManager.MASK_BULLETWEAPON_PIERCING_PASS =
                _originalPiercingMask | (1 << LayerManager.LAYER_ENEMY_DEAD);
        }

        public void MarkEnemyPredictedDead(EnemyAgent enemy)
        {
            AddEnemyReason(enemy, LimbLayerOverrideReason.PredictedDead);
        }

        public void ClearEnemyPredictedDead(EnemyAgent enemy)
        {
            RemoveEnemyReason(enemy, LimbLayerOverrideReason.PredictedDead);
        }

        public void MarkEnemyConfirmedDead(EnemyAgent enemy)
        {
            AddEnemyReason(enemy, LimbLayerOverrideReason.ConfirmedDead);
        }

        public void MarkPredictedCustomLimbDestroyed(Dam_EnemyDamageLimb limb)
        {
            AddLimbReason(limb, LimbLayerOverrideReason.PredictedCustomLimbDestroyed);
        }

        public void ConfirmCustomLimbDestroyed(Dam_EnemyDamageLimb limb)
        {
            RemoveLimbReason(limb, LimbLayerOverrideReason.PredictedCustomLimbDestroyed);
            AddLimbReason(limb, LimbLayerOverrideReason.CustomLimbDestroyed);
        }

        public void ClearPredictedCustomLimbDestroyed(EnemyAgent enemy)
        {
            RemoveEnemyReason(enemy, LimbLayerOverrideReason.PredictedCustomLimbDestroyed);
        }

        public void Forget(EnemyAgent enemy)
        {
            if (enemy == null)
                return;

            _enemyOverrides.Remove(enemy.GlobalID);
        }

        public void RestoreEnemy(EnemyAgent enemy)
        {
            if (enemy == null)
                return;

            if (!_enemyOverrides.TryGetValue(enemy.GlobalID, out var overrides))
                return;

            foreach (var layerOverride in overrides)
            {
                RestoreLayer(layerOverride);
            }

            _enemyOverrides.Remove(enemy.GlobalID);
        }

        public void RestoreAll()
        {
            foreach (var overrides in _enemyOverrides.Values)
            {
                foreach (var layerOverride in overrides)
                {
                    RestoreLayer(layerOverride);
                }
            }

            _enemyOverrides.Clear();

            if (_hasOriginalPiercingMask)
            {
                LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = _originalPiercingMask;
                _hasOriginalPiercingMask = false;
            }
        }

        private void AddEnemyReason(EnemyAgent enemy, LimbLayerOverrideReason reason)
        {
            if (!TryGetDamage(enemy, out var damage))
                return;

            foreach (var limb in damage.DamageLimbs)
            {
                AddLimbReason(limb, reason);
            }
        }

        private void RemoveEnemyReason(EnemyAgent enemy, LimbLayerOverrideReason reason)
        {
            if (enemy == null)
                return;

            if (!_enemyOverrides.TryGetValue(enemy.GlobalID, out var overrides))
                return;

            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                var layerOverride = overrides[i];
                layerOverride.RemoveReason(reason);
                ApplyOverrideState(layerOverride);

                if (layerOverride.Reasons == LimbLayerOverrideReason.None)
                    overrides.RemoveAt(i);
            }

            if (overrides.Count == 0)
                _enemyOverrides.Remove(enemy.GlobalID);
        }

        private void AddLimbReason(Dam_EnemyDamageLimb limb, LimbLayerOverrideReason reason)
        {
            if (!TryGetLimbIdentity(limb, out var enemyGlobalID, out var limbID))
                return;

            var layerOverride = GetOrCreateOverride(limb, enemyGlobalID, limbID);
            layerOverride.AddReason(reason);
            ApplyOverrideState(layerOverride);
        }

        private void RemoveLimbReason(Dam_EnemyDamageLimb limb, LimbLayerOverrideReason reason)
        {
            if (!TryGetLimbIdentity(limb, out var enemyGlobalID, out var limbID))
                return;

            if (!_enemyOverrides.TryGetValue(enemyGlobalID, out var overrides))
                return;

            for (int i = overrides.Count - 1; i >= 0; i--)
            {
                var layerOverride = overrides[i];
                if (layerOverride.LimbID != limbID)
                    continue;

                layerOverride.RemoveReason(reason);
                ApplyOverrideState(layerOverride);

                if (layerOverride.Reasons == LimbLayerOverrideReason.None)
                    overrides.RemoveAt(i);

                break;
            }

            if (overrides.Count == 0)
                _enemyOverrides.Remove(enemyGlobalID);
        }

        private LimbLayerOverride GetOrCreateOverride(Dam_EnemyDamageLimb limb, int enemyGlobalID, int limbID)
        {
            if (!_enemyOverrides.TryGetValue(enemyGlobalID, out var overrides))
            {
                overrides = new List<LimbLayerOverride>();
                _enemyOverrides.Add(enemyGlobalID, overrides);
            }

            for (int i = 0; i < overrides.Count; i++)
            {
                var layerOverride = overrides[i];
                if (layerOverride.LimbID != limbID)
                    continue;

                if (layerOverride.Limb == limb)
                    return layerOverride;

                var replacement = new LimbLayerOverride(limb, limbID);
                replacement.SetReasons(layerOverride.Reasons);
                RestoreLayer(layerOverride);
                overrides[i] = replacement;
                return replacement;
            }

            var created = new LimbLayerOverride(limb, limbID);
            overrides.Add(created);
            return created;
        }

        private static void ApplyOverrideState(LimbLayerOverride layerOverride)
        {
            if (layerOverride.Limb == null || layerOverride.Limb.gameObject == null)
                return;

            layerOverride.Limb.gameObject.layer = layerOverride.Reasons == LimbLayerOverrideReason.None
                ? layerOverride.OriginalLayer
                : LayerManager.LAYER_ENEMY_DEAD;
        }

        private static void RestoreLayer(LimbLayerOverride layerOverride)
        {
            if (layerOverride.Limb == null || layerOverride.Limb.gameObject == null)
                return;

            layerOverride.Limb.gameObject.layer = layerOverride.OriginalLayer;
        }

        private static bool TryGetDamage(EnemyAgent enemy, out Dam_EnemyDamageBase damage)
        {
            damage = null;

            if (enemy == null || enemy.Damage == null)
                return false;

            damage = enemy.Damage;
            return true;
        }

        private static bool TryGetLimbIdentity(Dam_EnemyDamageLimb limb, out int enemyGlobalID, out int limbID)
        {
            enemyGlobalID = 0;
            limbID = -1;

            if (limb == null || limb.m_base == null || limb.m_base.Owner == null)
                return false;

            enemyGlobalID = limb.m_base.Owner.GlobalID;
            limbID = limb.m_limbID;
            return true;
        }
    }


    #endregion

    #region 修改 LayerMask 使子弹可以穿透尸体
    [ArchivePatch(typeof(LayerManager), nameof(LayerManager.Setup))]
    private class LayerManager__Setup__Patch
    {
        private static void Postfix(LayerManager __instance)
        {
            s_layerPolicy.ApplyPiercingMask(__instance);
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
            s_predictionStore.CheckExpiration(__instance);
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

            if (__instance == null || __instance.Owner == null)
                return;

            var realDamage = EnemyDamageEstimator.EstimateBulletDamage(__instance, dam);
            s_predictionStore.ApplyLocalDamagePrediction(__instance.Owner, limbID, realDamage);
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

            if (__instance == null || __instance.Owner == null)
                return;

            var realDamage = EnemyDamageEstimator.EstimateMeleeDamage(__instance, dam);
            s_predictionStore.ApplyLocalDamagePrediction(__instance.Owner, limbID, realDamage);
        }
    }
    #endregion

    #region 屏蔽非法伤害
    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveBulletDamage), priority: int.MinValue)]
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

    [ArchivePatch(typeof(Dam_EnemyDamageBase), nameof(Dam_EnemyDamageBase.ReceiveMeleeDamage), priority: int.MinValue)]
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
