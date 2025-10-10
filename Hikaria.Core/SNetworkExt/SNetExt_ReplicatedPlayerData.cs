using TheArchive.Utilities;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_ReplicatedPlayerData<A> where A : struct
{
    public static void Setup(string eventName, Action<SNetwork.SNet_Player, A> callback)
    {
        if (s_singleton == null)
        {
            s_singleton = new()
            {
                m_syncPacket = SNetExt_Packet<A>.Create(eventName, OnReceiveData, null, SNetwork.SNet_ChannelType.SessionOrderCritical)
            };
        }
        s_singleton.m_onChangeCallback = callback;
    }

    private static void OnReceiveData(SNetwork.SNet_Player sender, A wrappedData)
    {
        if ((wrappedData as ISNetExt_ReplicatedPlayerData).PlayerData.TryGetPlayer(out var player) && !player.IsLocal)
        {
            player.StoreCustomData(wrappedData);
            Utils.SafeInvoke(s_singleton.m_onChangeCallback, player, wrappedData);
        }
    }

    public static void SendData(SNetwork.SNet_Player player, A data, SNetwork.SNet_Player toPlayer = null)
    {
        if (toPlayer != null && toPlayer.IsBot)
        {
            return;
        }
        if (player.IsLocal || SNetwork.SNet.IsMaster)
        {
            SNetwork.SNetStructs.pPlayer pPlayer = new();
            pPlayer.SetPlayer(player);
            ISNetExt_ReplicatedPlayerData replicatedPlayerData = data as ISNetExt_ReplicatedPlayerData;
            replicatedPlayerData.PlayerData = pPlayer;
            data = (A)replicatedPlayerData;
            if (toPlayer != null)
            {
                s_singleton.m_syncPacket.Send(data, toPlayer);
                return;
            }
            s_singleton.m_syncPacket.Send(data);
        }
    }

    public static bool Compare(delegateComparisonAction comparisonAction, A comparisonValue, SNetwork.eComparisonGroup group, bool includeBots = false)
    {
        Il2CppSystem.Collections.Generic.List<SNetwork.SNet_Player> list;
        if (group != SNetwork.eComparisonGroup.PlayersInSession)
        {
            if (group != SNetwork.eComparisonGroup.PlayersSynchedWithGame)
                return false;
            list = SNetwork.SNet.Slots.PlayersSynchedWithGame;
        }
        else
        {
            list = SNetwork.SNet.Slots.SlottedPlayers;
        }
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            var snet_Player = list[i];
            if (includeBots || !snet_Player.IsBot)
            {
                A a = snet_Player.LoadCustomData<A>();
                if (!comparisonAction(a, snet_Player, comparisonValue))
                    return false;
            }
        }
        return true;
    }

    private static SNetExt_ReplicatedPlayerData<A> s_singleton;

    private SNetExt_Packet<A> m_syncPacket;

    private Action<SNetwork.SNet_Player, A> m_onChangeCallback;

    public delegate bool delegateComparisonAction(A playerData, SNetwork.SNet_Player player, A comparisonData);
}