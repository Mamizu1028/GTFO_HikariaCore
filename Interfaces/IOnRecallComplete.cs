using SNetwork;

namespace Hikaria.Core.Interfaces;

public interface IOnRecallComplete
{
    void OnRecallComplete(eBufferType bufferType);
}
