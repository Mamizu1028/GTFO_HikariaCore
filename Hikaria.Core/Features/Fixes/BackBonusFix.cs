using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes
{
    [EnableFeatureByDefault]
    public class BackBonusFix : Feature
    {
        public override string Name => "背伤加成修复";

        public override string Description => "使得背伤加成通过敌人的背部实际朝向计算";

        public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ApplyDamageFromBehindBonus))]
        private class Dam_EnemyDamageLimb__ApplyDamageFromBehindBonus__Patch
        {
            static Quaternion rotation = Quaternion.Euler(0, -90, 0);

            private static bool Prefix(Dam_EnemyDamageLimb __instance, float dam, Vector3 dir, float backstabberMulti, ref float __result)
            {
                var enemy = __instance.m_base.Owner;
                if (enemy == null)
                    return ArchivePatch.RUN_OG;

                if (enemy.EnemyBalancingData.AllowDamgeBonusFromBehind)
                {
                    dir.Normalize();

                    var fwd = enemy.transform.forward;
                    var dot = Vector3.Dot(fwd, dir);
                    var dot2 = Vector3.Dot(dir, fwd);

                    var spineBone = enemy.Anim.GetBoneTransform(HumanBodyBones.Spine);
                    if (spineBone != null)
                    {
                        Vector3 chestFwd = rotation * spineBone.forward;
                        dot = Vector3.Dot(chestFwd, dir);
                        dot2 = Vector3.Dot(dir, spineBone.up * -1f);
                    }

                    if (dot > 0f)
                    {
                        dam *= Mathf.Clamp01(dot + 0.25f) + 1f;
                    }

                    if (backstabberMulti > 1f && (dot > 0.55f || dot2 > 0.55f))
                        dam *= backstabberMulti;
                }
                __result = dam;
                return ArchivePatch.SKIP_OG;
            }
        }
    }
}
