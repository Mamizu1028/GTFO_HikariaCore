using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using SNetwork;
using System.Collections;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using UnityEngine;

namespace Hikaria.Core.Features.Misc;

[EnableFeatureByDefault]
internal class EnemyRagdollTweaker : Feature
{
    public override string Name => "布娃娃调节器";

    public override TheArchive.Core.FeaturesAPI.Groups.GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Misc");

    [FeatureConfig]
    public static RagdollMultiplierSettings Settings { get; set; }

    public class RagdollMultiplierSettings
    {
        [FSDisplayName("增幅倍率")]
        public float ImpactForceMultiplie { get; set; } = 10f;
        [FSDisplayName("垂直增幅倍率")]
        public float UpwardMultiplier { get; set; } = 10f;
    }

    public override void OnEnable()
    {
        EnemyAPI.OnEnemyReceivedDamage += OnEnemyReceivedDamage;
        CoreAPI.OnPlayerModsSynced += OnPlayerModsSynced;
        SNetEventAPI.OnMasterChanged += OnMasterChanged;
    }

    public override void OnDisable()
    {
        EnemyAPI.OnEnemyReceivedDamage -= OnEnemyReceivedDamage;
        CoreAPI.OnPlayerModsSynced -= OnPlayerModsSynced;
        SNetEventAPI.OnMasterChanged -= OnMasterChanged;
        s_killedEnemies.Clear();
        s_enemyReceivedDamages.Clear();
    }

    private static void OnPlayerModsSynced(SNet_Player player, IEnumerable<pModInfo> mods)
    {
        if (player.IsMaster)
            s_masterHasCore = CoreAPI.IsPlayerInstalledCore(player);
    }

    private static void OnMasterChanged()
    {
        s_masterHasCore = CoreAPI.IsPlayerInstalledCore(SNet.Master);
    }

    private static void OnEnemyReceivedDamage(EnemyAgent enemy, pFullEnemyReceivedDamageData data)
    {
        if (!s_enemyReceivedDamages.TryGetValue(enemy.GlobalID, out var list))
        {
            list = new List<(float time, pFullEnemyReceivedDamageData data)>();
            s_enemyReceivedDamages.Add(enemy.GlobalID, list);
        }
        list.Add(new(Time.time, data));
        if (data.isKill)
            s_killedEnemies.Add(enemy.GlobalID);
    }

    private static bool s_masterHasCore;
    private static readonly HashSet<int> s_killedEnemies = new();
    private static readonly Dictionary<int, List<(float time, pFullEnemyReceivedDamageData data)>> s_enemyReceivedDamages = new();

    [ArchivePatch(typeof(EnemyRagdollBodyController), nameof(EnemyRagdollBodyController.SetRagdollEnabled))]
    private class EnemyRagdollBodyController__SetRagdollEnabled__Patch
    {
        private static bool Prefix(EnemyRagdollBodyController __instance, bool enabled)
        {
            if (enabled)
            {
                if (__instance.UseRagdollOptimization)
                {
                    __instance.m_owner.RagdollInstance = UnityEngine.Object.Instantiate(__instance.ragdollRefToInstantiate, __instance.transform.position, __instance.transform.rotation);
                    __instance.m_owner.RagdollInstance.SetActive(false);
                    __instance.m_owner.RagdollInstance.transform.localScale = __instance.m_owner.transform.localScale;
                    __instance.StartCoroutine(LinkToSpawnedRagdoll(__instance).WrapToIl2Cpp());
                    return false;
                }
            }
            return true;
        }

        private static IEnumerator LinkToSpawnedRagdoll(EnemyRagdollBodyController __instance)
        {
            yield return new WaitForFixedUpdate();
            var timeout = Time.time + 0.3f;
            var limbImpacts = new Dictionary<int, Vector3>();
            while (s_masterHasCore && Time.time < timeout)
            {
                if (s_killedEnemies.Contains(__instance.m_owner.GlobalID))
                {
                    foreach (var (time, data) in s_enemyReceivedDamages[__instance.m_owner.GlobalID])
                    {
                        if (Time.time - time <= 0.5f)
                        {
                            if (limbImpacts.ContainsKey(data.limbID))
                            {
                                limbImpacts[data.limbID] += data.direction * data.damage;
                            }
                            else
                            {
                                limbImpacts.Add(data.limbID, data.direction * data.damage);
                            }
                        }
                    }
                    break;
                }

                yield return null;
            }

            EnemyRagdollRef component = __instance.m_owner.RagdollInstance.GetComponent<EnemyRagdollRef>();
            __instance.transform.SetParent(__instance.m_owner.RagdollInstance.transform, true);
            __instance.m_owner.RagdollInstance.ChangeLayerRecursive(LayerManager.LAYER_ENEMY_DEAD, null);
            __instance.m_owner.RagdollInstance.SetActive(true);

            foreach (var kvp in component.m_rigidBodyLookup)
            {
                if (__instance.m_bodyDatas.ContainsKey(kvp.Key))
                {
                    Transform rbTransform = kvp.Value.transform;
                    Transform bdTransform = __instance.m_bodyDatas[kvp.Key].transform;
                    rbTransform.localPosition = bdTransform.localPosition;
                    rbTransform.localRotation = bdTransform.localRotation;
                    rbTransform.localScale = bdTransform.localScale;
                    bdTransform.SetParent(rbTransform, false);
                    bdTransform.localPosition = Vector3.zero;
                    bdTransform.localRotation = Quaternion.identity;
                    bdTransform.localScale = Vector3.one;
                    if (__instance.m_bodyDatas[kvp.Key].m_colliderInstance != null)
                    {
                        __instance.m_bodyDatas[kvp.Key].m_colliderInstance.enabled = false;
                    }
                    kvp.Value.isKinematic = false;
                    kvp.Value.useGravity = true;
                    kvp.Value.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    kvp.Value.interpolation = RigidbodyInterpolation.Interpolate;
                    kvp.Value.velocity = __instance.m_bodyDatas[kvp.Key].m_velocity;
                    if (s_masterHasCore)
                    {
                        if (limbImpacts.TryGetValue(__instance.m_bodyDatas.FindEntry(kvp.Key), out var impact))
                        {
                            var force = impact * Settings.ImpactForceMultiplie;
                            force.y = Mathf.Max(0f, force.y);
                            force += Vector3.up * Settings.UpwardMultiplier;
                            kvp.Value.AddForce(force, ForceMode.Impulse);
                            Vector3 torque = Vector3.Cross(impact, Vector3.up) * Settings.ImpactForceMultiplie;
                            kvp.Value.AddTorque(torque, ForceMode.Impulse);
                        }
                    }
                    else
                    {
                        kvp.Value.AddForce(Vector3.up * Settings.UpwardMultiplier, ForceMode.Impulse);
                    }
                }
            }
            if (s_masterHasCore)
            {
                s_killedEnemies.Remove(__instance.m_owner.GlobalID);
                s_enemyReceivedDamages.Remove(__instance.m_owner.GlobalID);
            }
        }
    }
}
