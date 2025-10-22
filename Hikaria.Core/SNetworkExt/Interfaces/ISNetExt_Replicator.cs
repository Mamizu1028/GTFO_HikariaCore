namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_Replicator
{
    bool LocallyOwned { get; }

    SNetExt_ReplicatedPacket<T> CreatePacket<T>(string packetKey, Action<T> receiveAction, Action<T> validateAction = null) where T : struct;

    ISNetExt_ReplicatorSupplier ReplicatorSupplier { get; set; }

    void ReceiveBytes(string key, int packetIndex, byte[] bytes);

    SNetwork.SNet_Player OwningPlayer { get; set; }

    SNetExt_ReplicatorType Type { get; }

    byte[] KeyHashBytes { get; }

    string Key { get; set; }

    string KeyHash { get; set; }

    bool IsAnonymous { get; }

    bool HasValidKeyHash { get; }

    void Despawn();

    Type GetPacketType(string key, int packetIndex);
}
