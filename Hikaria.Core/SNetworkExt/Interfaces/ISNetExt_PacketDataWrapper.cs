namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_PacketDataWrapper
{
    int GetTotalByteSize();
    int GetCount();
    void Serialize(byte[] bytes, ref int byteIndexOffset);
    void Deserialize(int objCount, byte[] bytes, ref int byteIndexOffset);
}
