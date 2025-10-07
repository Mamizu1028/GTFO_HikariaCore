namespace Hikaria.Core.SNetworkExt;

public interface IReplicator
{
    bool LocallyOwned { get; }

    SNetExt_ReplicatedPacket<T> CreatePacket<T>(string packetKey, Action<T> receiveAction = null, Action<T> validateAction = null) where T : struct;

    IReplicatorSupplier ReplicatorSupplier { get; set; }

    void ReceiveBytes(string key, int packetIndex, byte[] bytes);

    SNetwork.SNet_Player OwningPlayer { get; set; }

    SNetExt_ReplicatorType Type { get; }

    byte[] KeyBytes { get; set; }

    string Key { get; set; }

    string KeyHash { get; }

    void Despawn();

    Type GetPacketType(string key, int packetIndex);
}
