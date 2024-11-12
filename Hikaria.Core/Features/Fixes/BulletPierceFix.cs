using Agents;
using FX_EffectSystem;
using Gear;
using HarmonyLib;
using Player;
using TheArchive;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.Core.Features.Fixes
{
    [EnableFeatureByDefault]
    public class BulletPierceFix : Feature
    {
        public override string Name => "子弹穿透修复";

        public override string Description => "修正了下列问题\n" +
            "1. 穿透存在上限 --> 解除穿透次数限制\n" +
            "2. 穿透后扩散增加 --> 穿透后将扩散设为0\n" +
            "3. 穿透次数代表最多可命中敌人数 --> 穿透次数代表可穿透敌人数";

        public override FeatureGroup Group => EntryPoint.Groups.Fixes;

        [ArchivePatch(typeof(Weapon), nameof(Weapon.CastWeaponRay))]
        private class Weapon__CastWeaponRay__Patch
        {
            public static Type[] ParameterTypes() => new[]
            {
                typeof(Transform),
                typeof(global::Weapon.WeaponHitData).MakeByRefType(),
                typeof(Vector3),
                typeof(int)
            };

            private static void Postfix(ref Weapon.WeaponHitData weaponRayData)
            {
                weaponRayData.randomSpread = 0f;
                weaponRayData.angOffsetX = 0;
                weaponRayData.angOffsetY = 0;
            }
        }

        [HarmonyAfter(new string[] { $"{ArchiveMod.MOD_NAME}_FeaturesAPI_AccuracyTracker", $"{ArchiveMod.MOD_NAME}_FeaturesAPI_{nameof(WeaponRayUpdateFix)}" })]
        [HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private class BulletWeapon__Fire__Patch
        {
            private static bool Prefix(BulletWeapon __instance, bool resetRecoilSimilarity = true)
            {
                float num;
                if (__instance.FPItemHolder.ItemAimTrigger)
                {
                    num = __instance.ArchetypeData.AimSpread;
                }
                else
                {
                    num = __instance.ArchetypeData.HipFireSpread;
                }
                if (Clock.Time - __instance.m_lastFireTime > __instance.m_fireRecoilCooldown)
                {
                    num *= 0.2f;
                }
                __instance.m_lastFireTime = Clock.Time;
                if (__instance.Owner.IsLocallyOwned)
                {
                    PlayerAgent.LastLocalShotFiredTime = __instance.m_lastFireTime;
                }
                Vector3 firePos = __instance.Owner.FPSCamera.Position;
                Weapon.s_weaponRayData = new Weapon.WeaponHitData
                {
                    randomSpread = num,
                    maxRayDist = __instance.MaxRayDist,
                    fireDir = (__instance.Owner.FPSCamera.CameraRayPos - firePos).normalized,
                    owner = __instance.Owner,
                    damage = __instance.ArchetypeData.GetDamageWithBoosterEffect(__instance.Owner, __instance.ItemDataBlock.inventorySlot),
                    staggerMulti = __instance.ArchetypeData.StaggerDamageMulti,
                    precisionMulti = __instance.ArchetypeData.PrecisionDamageMulti,
                    damageFalloff = __instance.ArchetypeData.DamageFalloff,
                };
                var rayData = Weapon.s_weaponRayData;
                if (__instance.ArchetypeData.PiercingBullets)
                {
                    if (__instance.m_damageSearchID >= uint.MaxValue)
                    {
                        __instance.m_damageSearchID = 0U;
                    }
                    __instance.m_damageSearchID += 1U;
                    bool breakPierce = false;
                    float additionalDis = 0f;
                    int pierceCount = -1;
                    while (!breakPierce && Weapon.s_weaponRayData.maxRayDist > 0f)
                    {
                        if (pierceCount >= __instance.ArchetypeData.PiercingDamageCountLimit)
                        {
                            break;
                        }

                        if (Weapon.CastWeaponRay(__instance.Owner.FPSCamera.transform, ref rayData, firePos, -1))
                        {
                            if (BulletWeapon.BulletHit(Weapon.s_weaponRayData, true, additionalDis, __instance.m_damageSearchID))
                            {
                                pierceCount++;
                            }
                            FX_Manager.EffectTargetPosition = Weapon.s_weaponRayData.rayHit.point;
                            breakPierce = !Weapon.s_weaponRayData.rayHit.collider.gameObject.IsInLayerMask(LayerManager.MASK_BULLETWEAPON_PIERCING_PASS);
                            firePos = Weapon.s_weaponRayData.rayHit.point + Weapon.s_weaponRayData.fireDir * 0.1f;
                            additionalDis += Weapon.s_weaponRayData.rayHit.distance;
                            Weapon.s_weaponRayData.maxRayDist -= Weapon.s_weaponRayData.rayHit.distance;
                        }
                        else
                        {
                            breakPierce = true;
                            FX_Manager.EffectTargetPosition = __instance.Owner.FPSCamera.CameraRayPos;
                        }
                    }
                }
                else if (Weapon.CastWeaponRay(__instance.Owner.FPSCamera.transform, ref rayData, firePos, -1))
                {
                    BulletWeapon.BulletHit(Weapon.s_weaponRayData, true, 0f, 0U);
                    FX_Manager.EffectTargetPosition = Weapon.s_weaponRayData.rayHit.point;
                }
                else
                {
                    FX_Manager.EffectTargetPosition = __instance.Owner.FPSCamera.CameraRayPos;
                }
                FX_Manager.PlayLocalVersion = false;
                BulletWeapon.s_tracerPool.AquireEffect().Play(null, __instance.MuzzleAlign.position, Quaternion.LookRotation(Weapon.s_weaponRayData.fireDir));
                EX_SpriteMuzzleFlash muzzleFlash = __instance.m_muzzleFlash;
                if (muzzleFlash != null)
                {
                    muzzleFlash.Play();
                }
                if (__instance.m_rotatingCylinder != null)
                {
                    if (__instance.m_cylinderRotationCoroutineScript == null)
                    {
                        __instance.m_cylinderRotationCoroutineScript = __instance.m_rotatingCylinder.GetComponent<CylinderRotationCoroutine>();
                    }
                    float num6 = 360f / __instance.ClipSize;
                    __instance.m_cylinderRotationCoroutineScript.RotationAngle = Quaternion.Euler(0f, num6, 0f);
                }
                if (__instance.ShellCasingData != null && __instance.ShellCasingData.ShellCasingType != ShellTypes.Shell_None)
                {
                    if (__instance.Owner.IsLocallyOwned)
                    {
                        WeaponShellManager.RegisterFPSShellEject(__instance.ShellCasingData.ShellCasingType, 1f, 1f, __instance.ShellEjectAlign);
                    }
                    else
                    {
                        WeaponShellManager.EjectShell(__instance.ShellCasingData.ShellCasingType, 1f, 1f, __instance.ShellEjectAlign, Vector3.one);
                    }
                }
                if (__instance.RecoilAnimation != null)
                {
                    __instance.ApplyRecoil(true);
                }
                else
                {
                    __instance.ApplyRecoil(resetRecoilSimilarity);
                }
                __instance.TriggerFireAnimationSequence();
                __instance.Owner.Noise = Agent.NoiseType.Shoot;
                __instance.Owner.Sync.RegisterFiredBullets(1);
                __instance.FPItemHolder.DontRelax();
                for (int i = 0; i < __instance.m_itemPartAnimators.Count; i++)
                {
                    __instance.m_itemPartAnimators[i].CrossFadeInFixedTime("Fire", 0f, 0);
                }
                __instance.m_clip--;
                __instance.UpdateAmmoStatus();

                return false;
            }
        }

        [HarmonyAfter(new string[] { $"{ArchiveMod.MOD_NAME}_FeaturesAPI_AccuracyTracker", $"{ArchiveMod.MOD_NAME}_FeaturesAPI_{nameof(WeaponRayUpdateFix)}" })]
        [HarmonyPatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private class Shotgun__Fire__Patch
        {
            private static bool Prefix(Shotgun __instance, bool resetRecoilSimilarity = true)
            {
                for (int i = 0; i < __instance.ArchetypeData.ShotgunBulletCount; i++)
                {
                    Vector3 firePos = __instance.Owner.FPSCamera.Position;
                    float num = __instance.m_segmentSize * i;
                    float angOffsetX = 0f;
                    float angOffsetY = 0f;
                    if (i > 0)
                    {
                        angOffsetX += __instance.ArchetypeData.ShotgunConeSize * Mathf.Cos(num);
                        angOffsetY += __instance.ArchetypeData.ShotgunConeSize * Mathf.Sin(num);
                    }
                    Weapon.s_weaponRayData = new Weapon.WeaponHitData
                    {
                        owner = __instance.Owner,
                        damage = __instance.ArchetypeData.GetDamageWithBoosterEffect(__instance.Owner, __instance.ItemDataBlock.inventorySlot),
                        staggerMulti = __instance.ArchetypeData.StaggerDamageMulti,
                        precisionMulti = __instance.ArchetypeData.PrecisionDamageMulti,
                        damageFalloff = __instance.ArchetypeData.DamageFalloff,
                        maxRayDist = __instance.MaxRayDist,
                        angOffsetX = angOffsetX,
                        angOffsetY = angOffsetY,
                        randomSpread = __instance.ArchetypeData.ShotgunBulletSpread,
                        fireDir = (__instance.Owner.FPSCamera.CameraRayPos - firePos).normalized
                    };
                    var rayData = Weapon.s_weaponRayData;
                    if (__instance.ArchetypeData.PiercingBullets)
                    {
                        if (__instance.m_damageSearchID >= uint.MaxValue)
                        {
                            __instance.m_damageSearchID = 0U;
                        }
                        __instance.m_damageSearchID += 1U;
                        bool breakPierce = false;
                        float additionalDis = 0f;
                        int pierceCount = -1;
                        while (!breakPierce && Weapon.s_weaponRayData.maxRayDist > 0f)
                        {
                            if (pierceCount >= __instance.ArchetypeData.PiercingDamageCountLimit)
                            {
                                break;
                            }

                            if (Weapon.CastWeaponRay(__instance.Owner.FPSCamera.transform, ref rayData, firePos, -1))
                            {
                                if (BulletWeapon.BulletHit(Weapon.s_weaponRayData, true, additionalDis, __instance.m_damageSearchID))
                                {
                                    pierceCount++;
                                }
                                FX_Manager.EffectTargetPosition = Weapon.s_weaponRayData.rayHit.point;
                                breakPierce = !Weapon.s_weaponRayData.rayHit.collider.gameObject.IsInLayerMask(LayerManager.MASK_BULLETWEAPON_PIERCING_PASS);
                                firePos = Weapon.s_weaponRayData.rayHit.point + Weapon.s_weaponRayData.fireDir * 0.1f;
                                additionalDis += Weapon.s_weaponRayData.rayHit.distance;
                                Weapon.s_weaponRayData.maxRayDist -= Weapon.s_weaponRayData.rayHit.distance;

                                if (rayData.GetHashCode() != Weapon.s_weaponRayData.GetHashCode())
                                {
                                    Logs.LogError("RayHitData Not Match!");
                                }
                            }
                            else
                            {
                                breakPierce = true;
                                FX_Manager.EffectTargetPosition = __instance.Owner.FPSCamera.CameraRayPos;
                            }
                        }
                    }
                    else if (Weapon.CastWeaponRay(__instance.MuzzleAlign, ref rayData, firePos, -1))
                    {
                        BulletWeapon.BulletHit(Weapon.s_weaponRayData, true, 0f, 0U);
                        FX_Manager.EffectTargetPosition = Weapon.s_weaponRayData.rayHit.point;
                    }
                    else
                    {
                        FX_Manager.EffectTargetPosition = __instance.Owner.FPSCamera.CameraRayPos;
                    }
                    FX_Manager.PlayLocalVersion = false;
                    BulletWeapon.s_tracerPool.AquireEffect().Play(null, __instance.MuzzleAlign.position, Quaternion.LookRotation(Weapon.s_weaponRayData.fireDir));
                }
                __instance.TriggerFireAnimationSequence();
                __instance.Owner.Noise = Agent.NoiseType.Shoot;
                __instance.ApplyRecoil(true);
                EX_SpriteMuzzleFlash muzzleFlash = __instance.m_muzzleFlash;
                if (muzzleFlash != null)
                {
                    muzzleFlash.Play();
                }
                if (__instance.ShellCasingData != null)
                {
                    WeaponShellManager.EjectShell(__instance.ShellCasingData.ShellCasingType, 1f, 1f, __instance.ShellEjectAlign, Vector3.one);
                }
                __instance.FPItemHolder.DontRelax();
                __instance.Owner.Sync.RegisterFiredBullets(1);
                for (int j = 0; j < __instance.m_itemPartAnimators.Count; j++)
                {
                    __instance.m_itemPartAnimators[j].CrossFadeInFixedTime("Fire", 0f, 0);
                }
                __instance.m_lastFireTime = Clock.Time;
                if (__instance.Owner.IsLocallyOwned)
                {
                    PlayerAgent.LastLocalShotFiredTime = __instance.m_lastFireTime;
                }
                __instance.m_clip--;
                __instance.UpdateAmmoStatus();

                return false;
            }
        }
    }
}
