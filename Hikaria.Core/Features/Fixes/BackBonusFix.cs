using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes
{
    [EnableFeatureByDefault]
    public class BackBonusFix : Feature
    {
        public override string Name => "背伤加成修复";

        public override string Description => "使得背伤加成通过敌人的背部实际朝向计算";

        public override FeatureGroup Group => EntryPoint.Groups.Fixes;

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.ApplyDamageFromBehindBonus))]
        private class Dam_EnemyDamageLimb__ApplyDamageFromBehindBonus__Patch
        {
            private static bool Prefix(Dam_EnemyDamageLimb __instance, float dam, Vector3 dir, float backstabberMulti, ref float __result)
            {
                var enemy = __instance.m_base.Owner;
                if (enemy == null)
                    return ArchivePatch.RUN_OG;
                if (enemy.EnemyBalancingData.AllowDamgeBonusFromBehind)
                {
                    var spineBone = enemy.Anim.GetBoneTransform(HumanBodyBones.Spine);
                    var fwd = enemy.transform.forward;
                    var dot = Vector3.Dot(fwd, dir);
                    var dot2 = Vector3.Dot(dir, fwd);

                    if (spineBone != null)
                    {
                        Vector3 vector2 = Vector3.ProjectOnPlane(spineBone.forward, Vector3.up);
                        Vector3 vector3 = Quaternion.AngleAxis(-90f, vector2) * Vector3.up;
                        dot = Vector3.Dot(vector3.normalized, -dir);
                        dot2 = Vector3.Dot(dir, spineBone.up);
                    }

                    dam *= Mathf.Clamp01(dot + 0.25f) + 1f;

                    if (backstabberMulti > 1f && (dot > 0.55f || dot2 > 0.55f))
                        dam *= backstabberMulti;
                }
                __result = dam;
                return ArchivePatch.SKIP_OG;
            }
        }
    }
}
