namespace Hikaria.Core.SNetworkExt;

internal interface ISNetExt_MutableReplicator : ISNetExt_Replicator
{
    void AssignKey(string key);

    void AssignKeyHash(in SNetExt_KeyHash16 keyHash);

    void ReceiveBytes(in SNetExt_KeyHash16 packetKeyHash, byte packetIndex, byte[] bytes);
}
