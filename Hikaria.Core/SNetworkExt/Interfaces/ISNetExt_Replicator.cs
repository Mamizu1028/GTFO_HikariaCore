namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_Replicator
{
    string Key { get; }

    string KeyHash { get; }

    ReadOnlySpan<byte> KeyHashBytes { get; }

    SNetExt_ReplicatorType Type { get; }

    bool LocallyOwned { get; }

    bool IsAnonymous { get; }

    bool HasValidKeyHash { get; }

    SNetwork.SNet_Player OwningPlayer { get; }

    bool OwnedByMaster { get; }

    ISNetExt_ReplicatorSupplier ReplicatorSupplier { get; set; }

    SNetExt_ReplicatedPacket<T> CreatePacket<T>(string packetKey, Action<T> receiveAction, Action<T> validateAction = null) where T : struct;

    SNetExt_ReplicatedPacketBytes CreatePacketBytes(string key, SNetExt_ReplicatedPacketBytes.SpanReceiveDelegate receiveAction);

    SNetExt_ReplicatedPacketBufferBytes CreatePacketBufferBytes(string key, SNetExt_ReplicatedPacketBufferBytes.SpanBufferReceiveDelegate receiveAction);

    void Despawn();

    Type GetPacketType(string key, int packetIndex);
}
