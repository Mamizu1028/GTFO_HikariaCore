using SNetwork;
using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Patches;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Groups;

namespace Hikaria.Core.Features.Fixes;

[EnableFeatureByDefault]
internal class LobbyGhostPlayerFix : Feature
{
    public override string Name => "卡房修复";

    public override string Description => "在玩家离开大厅时自动检查是否有位置被卡\n" +
        "可通过锁定位置的方法手动修复特定位置被卡的问题";

    public override GroupBase Group => ModuleGroup.GetOrCreateSubGroup("Fixes");

    public override void OnEnable()
    {
        SNetEventAPI.OnSessionMemberChanged += OnSessionMemberChanged;
    }

    public override void OnDisable()
    {
        SNetEventAPI.OnSessionMemberChanged -= OnSessionMemberChanged;
    }

    public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
    {
        if (!SNet.IsMaster || player.IsLocal || playerEvent != SessionMemberEvent.LeftSessionHub)
            return;
        if (CheckNeedCleanup(player))
            CleanupForPlayer(player);
    }

    [ArchivePatch(typeof(SNet_PlayerSlotManager), nameof(SNet_PlayerSlotManager.SetSlotPermission))]
    private class SNet_PlayerSlotManager__SetSlotPermission__Patch
    {
        private static void Postfix(SNet_PlayerSlotManager __instance, int playerIndex, SNet_PlayerSlotManager.SlotPermission permission)
        {
            if (!SNet.IsMaster || permission != SNet_PlayerSlotManager.SlotPermission.Forbidden)
                return;
            CleanupForPlayer(__instance.PlayerSlots[playerIndex].player);
        }
    }

    private static bool CheckNeedCleanup(SNet_Player player)
    {
        if (player == null) return false;
        var slots = SNet.Slots;
        for (int i = 0; i < slots.CharacterSlots.Count; i++)
        {
            var characterSlot = slots.CharacterSlots[i];
            if (characterSlot.player != null && characterSlot.player.Lookup == player.Lookup)
                return true;
        }
        for (int i = 0; i < slots.PlayerSlots.Count; i++)
        {
            var playerSlot = slots.PlayerSlots[i];
            if (playerSlot.player != null && playerSlot.player.Lookup == player.Lookup)
                return true;
        }
        for (int i = 0; i < SNet.Lobby.Players.Count; i++)
        {
            var lobbyPlayer = SNet.Lobby.Players[i];
            if (lobbyPlayer.Lookup == player.Lookup)
                return true;
        }
        return false;
    }

    private static void CleanupForPlayer(SNet_Player player)
    {
        if (player == null) return;
        var slots = SNet.Slots;
        SNet.Sync.KickPlayer(player, SNet_PlayerEventReason.Kick_GameFull);
        for (int i = 0; i < slots.CharacterSlots.Count; i++)
        {
            var characterSlot = slots.CharacterSlots[i];
            if (characterSlot.player != null && characterSlot.player.Lookup == player.Lookup)
                slots.Internal_ManageSlot(player, ref characterSlot, slots.CharacterSlots, SNet_SlotType.CharacterSlot, SNet_SlotHandleType.Remove);
        }
        for (int i = 0; i < slots.PlayerSlots.Count; i++)
        {
            var playerSlot = slots.PlayerSlots[i];
            if (playerSlot.player != null && playerSlot.player.Lookup == player.Lookup)
                slots.Internal_ManageSlot(player, ref playerSlot, slots.PlayerSlots, SNet_SlotType.PlayerSlot, SNet_SlotHandleType.Remove);
        }
        SNet.Lobby.Players.RemoveAll((Func<SNet_Player, bool>)((p) => p.Lookup == player.Lookup));
    }
}
