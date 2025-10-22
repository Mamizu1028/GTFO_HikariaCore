using GameData;
using Gear;
using Player;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Accessibility;

[EnableFeatureByDefault]
internal class ReceiveAmmoGiveFix : Feature
{
    public override string Name => "弹药补给修复";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Accessibility");

    [ArchivePatch(typeof(PlayerBackpackManager), nameof(PlayerBackpackManager.ReceiveAmmoGive))]
    public class PlayerBackpackManager__ReceiveAmmoGive__Patch
    {
        private static Dictionary<InventorySlot, float> AmmoMaxCapLookup = new()
        {
            { InventorySlot.GearStandard, 0f },
            { InventorySlot.GearSpecial, 0f },
        };

        private static InventorySlotAmmo StandardAmmo;

        private static InventorySlotAmmo SpeacialAmmo;

        private static bool NeedRestoredStandardAmmoMaxCap = false;

        private static bool NeedRestoredSpecialAmmoMaxCap = false;

        private static float AmmoStandardResourcePackMaxCap => PlayerDataBlock.GetBlock(1U).AmmoStandardResourcePackMaxCap;
        private static float AmmoSpecialResourcePackMaxCap => PlayerDataBlock.GetBlock(1U).AmmoSpecialResourcePackMaxCap;

        private static void Prefix(ref pAmmoGive data)
        {
            bool StandardAmmoOverflow = false;
            bool SpecialAmmoOverflow = false;
            float OverflowStandardAmmo = 0f;
            float OverflowSpecialAmmo = 0f;

            if (data.targetPlayer.TryGetPlayer(out var player) && player.IsLocal)
            {
                BackpackItem backpackItem;
                if (PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.GearStandard, out backpackItem))
                {
                    BulletWeapon weapon = backpackItem.Instance.TryCast<BulletWeapon>();
                    float freeClipAmmo = (weapon.ClipSize - weapon.m_clip) * weapon.ArchetypeData.CostOfBullet;
                    StandardAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage.StandardAmmo;
                    AmmoMaxCapLookup[InventorySlot.GearStandard] = StandardAmmo.AmmoMaxCap;
                    StandardAmmo.AmmoMaxCap += freeClipAmmo;
                    float givedAmmo = data.ammoStandardRel * AmmoStandardResourcePackMaxCap;
                    OverflowStandardAmmo = givedAmmo + StandardAmmo.AmmoInPack - StandardAmmo.AmmoMaxCap;
                    StandardAmmoOverflow = OverflowStandardAmmo > 0;
                }

                if (PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.GearSpecial, out backpackItem))
                {
                    BulletWeapon weapon = backpackItem.Instance.TryCast<BulletWeapon>();
                    float freeClipAmmo = (weapon.ClipSize - weapon.m_clip) * weapon.ArchetypeData.CostOfBullet;
                    SpeacialAmmo = PlayerBackpackManager.LocalBackpack.AmmoStorage.SpecialAmmo;
                    AmmoMaxCapLookup[InventorySlot.GearSpecial] = SpeacialAmmo.AmmoMaxCap;
                    SpeacialAmmo.AmmoMaxCap += freeClipAmmo;
                    float givedAmmo = data.ammoSpecialRel * AmmoSpecialResourcePackMaxCap;
                    OverflowSpecialAmmo = givedAmmo + SpeacialAmmo.AmmoInPack - SpeacialAmmo.AmmoMaxCap;
                    SpecialAmmoOverflow = OverflowSpecialAmmo > 0;
                }

                if (StandardAmmoOverflow && !SpecialAmmoOverflow)
                {
                    data.ammoSpecialRel += OverflowStandardAmmo / AmmoStandardResourcePackMaxCap;
                }
                else if (!StandardAmmoOverflow && SpecialAmmoOverflow)
                {
                    data.ammoStandardRel += OverflowSpecialAmmo / AmmoSpecialResourcePackMaxCap;
                }
            }
        }

        private static void Postfix(pAmmoGive data)
        {
            if (data.targetPlayer.TryGetPlayer(out var player) && player.IsLocal)
            {
                if (PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.GearStandard, out _))
                {
                    StandardAmmo.AmmoMaxCap = AmmoMaxCapLookup[InventorySlot.GearStandard];
                    PlayerBackpackManager.LocalBackpack.AmmoStorage.UpdateSlotAmmoUI(InventorySlot.GearStandard);
                }
                if (PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.GearSpecial, out _))
                {
                    SpeacialAmmo.AmmoMaxCap = AmmoMaxCapLookup[InventorySlot.GearSpecial];
                    PlayerBackpackManager.LocalBackpack.AmmoStorage.UpdateSlotAmmoUI(InventorySlot.GearSpecial);
                }
            }
        }
    }
}
