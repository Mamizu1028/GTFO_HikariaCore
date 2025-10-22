using LevelGeneration;
using Player;
using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Accessibility;

[EnableFeatureByDefault]
internal class ResourceStack : Feature
{
    public override string Name => "资源堆叠";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Accessibility");

    [ArchivePatch(typeof(LG_PickupItem_Sync), nameof(LG_PickupItem_Sync.AttemptInteract))]
    private class LG_PickupItem_Sync__AttemptInteract__Patch
    {
        private static void Prefix(LG_PickupItem_Sync __instance, pPickupItemInteraction interaction)
        {
            if (!SNet.IsMaster)
                return;
            
            if (interaction.type == ePickupItemInteractionType.Pickup)
            {
                if (interaction.pPlayer.TryGetPlayer(out var player))
                {
                    TryStackItem(__instance.item, player);
                }
            }
        }
    }

    private static void TryStackItem(Item item, SNet_Player player)
    {
        var slot = item.pItemData.slot;
        if (slot != InventorySlot.ResourcePack && slot != InventorySlot.Consumable)
            return;
        if (!PlayerBackpackManager.TryGetBackpack(player, out var backpack))
            return;
        if (!backpack.TryGetBackpackItem(slot, out var backpackItem))
            return;

        if (backpackItem.Instance.pItemData.itemID_gearCRC != item.pItemData.itemID_gearCRC)
            return;

        float consumableAmmoMax = item.ItemDataBlock.ConsumableAmmoMax;
        if (slot == InventorySlot.ResourcePack)
        {
            consumableAmmoMax = 100f;
        }
        var ammoType = slot == InventorySlot.ResourcePack ? AmmoType.ResourcePackRel : AmmoType.CurrentConsumable;
        float ammoInPack = backpack.AmmoStorage.GetAmmoInPack(ammoType);
        if (ammoInPack >= consumableAmmoMax)
            return;
        float totalAmmo = item.pItemData.custom.ammo + ammoInPack;
        pItemData_Custom customData = item.GetCustomData();
        if (totalAmmo > consumableAmmoMax)
        {
            customData.ammo = consumableAmmoMax;
            item.TryCast<ItemInLevel>().GetSyncComponent().SetCustomData(customData, true);
            backpack.AmmoStorage.SetAmmo(ammoType, totalAmmo - consumableAmmoMax);
            return;
        }
        PlayerBackpackManager.MasterRemoveItem(backpackItem.Instance, player);
        customData.ammo = totalAmmo;
        item.TryCast<ItemInLevel>().GetSyncComponent().SetCustomData(customData, true);
    }
}
