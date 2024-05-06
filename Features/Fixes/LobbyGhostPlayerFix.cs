using Hikaria.Core.Interfaces;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.Core.Features.Fixes;

[EnableFeatureByDefault]
internal class LobbyGhostPlayerFix : Feature, IOnSessionMemberChanged
{
    public override string Name => "Lobby Ghost Player Fix";

    public override FeatureGroup Group => EntryPoint.Groups.Fixes;

    public override void OnEnable()
    {
        GameEventAPI.RegisterSelf(this);
    }

    public override void OnDisable()
    {
        GameEventAPI.UnregisterSelf(this);
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (!SNet.IsMaster || player.IsLocal || playerEvent != SessionMemberEvent.LeftSessionHub)
            return;
        if (SlotsNeedCleanup(player))
            CleanupSlotsForPlayer(player);
    }

    [ArchivePatch(typeof(SNet_PlayerSlotManager), nameof(SNet_PlayerSlotManager.SetSlotPermission))]
    private class SNet_PlayerSlotManager__SetSlotPermission__Patch
    {
        private static void Postfix(SNet_PlayerSlotManager __instance, int playerIndex, SNet_PlayerSlotManager.SlotPermission permission)
        {
            if (!SNet.IsMaster || permission != SNet_PlayerSlotManager.SlotPermission.Forbidden)
            {
                return;
            }
            CleanupSlotsForPlayer(__instance.PlayerSlots[playerIndex].player);
        }
    }

    private static bool SlotsNeedCleanup(SNet_Player player)
    {
        if (player == null) return false;
        var slots = SNet.Slots;
        for (int i = 0; i < slots.CharacterSlots.Count; i++)
        {
            var characterSlot = slots.CharacterSlots[i];
            if (characterSlot.player != null && characterSlot.player.Lookup == player.Lookup)
            {
                return true;
            }
        }
        for (int i = 0; i < slots.PlayerSlots.Count; i++)
        {
            var playerSlot = slots.PlayerSlots[i];
            if (playerSlot.player != null && playerSlot.player.Lookup == player.Lookup)
            {
                return true;
            }
        }
        return false;
    }

    private static void CleanupSlotsForPlayer(SNet_Player player)
    {
        if (player == null) return;
        var slots = SNet.Slots;
        SNet.Sync.KickPlayer(player, SNet_PlayerEventReason.Kick_GameFull);
        for (int i = 0; i < slots.CharacterSlots.Count; i++)
        {
            var characterSlot = slots.CharacterSlots[i];
            if (characterSlot.player != null && characterSlot.player.Lookup == player.Lookup)
            {
                slots.Internal_ManageSlot(player, ref characterSlot, slots.CharacterSlots, SNet_SlotType.CharacterSlot, SNet_SlotHandleType.Remove);
            }
        }
        for (int i = 0; i < slots.PlayerSlots.Count; i++)
        {
            var playerSlot = slots.PlayerSlots[i];
            if (playerSlot.player != null && playerSlot.player.Lookup == player.Lookup)
            {
                slots.Internal_ManageSlot(player, ref playerSlot, slots.PlayerSlots, SNet_SlotType.PlayerSlot, SNet_SlotHandleType.Remove);
            }
        }
    }
}
