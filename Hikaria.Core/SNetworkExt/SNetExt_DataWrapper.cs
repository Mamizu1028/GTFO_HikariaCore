namespace Hikaria.Core.SNetworkExt;

public abstract class DataWrapper
{
    public abstract void Send(SNetwork.SNet_Player fromPlayer, SNetwork.SNet_Player toPlayer = null);
}

public class DataWrapper<A> : DataWrapper where A : struct
{
    public A Load()
    {
        return m_data;
    }

    public void Store(SNetwork.SNet_Player player, A data)
    {
        m_data = data;
        SNetExt_ReplicatedPlayerData<A>.SendData(player, m_data);
    }

    public override void Send(SNetwork.SNet_Player fromPlayer, SNetwork.SNet_Player toPlayer = null)
    {
        SNetExt_ReplicatedPlayerData<A>.SendData(fromPlayer, m_data, toPlayer);
    }

    private A m_data;
}
