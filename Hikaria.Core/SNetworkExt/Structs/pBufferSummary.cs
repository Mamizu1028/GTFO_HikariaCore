using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public struct pBuffersSummary
{
    public pBuffersSummary(SNetExt_CaptureBuffer buffer)
    {
        bufferType = buffer.type;
        if (buffer.isValid && SNetwork.SNet.LocalPlayer.Session.levelChecksum == buffer.data.levelChecksum)
        {
            bufferID = buffer.data.bufferID;
            progressionTime = buffer.data.progressionTime;
            levelChecksum = buffer.data.levelChecksum;
            return;
        }
        bufferID = 0U;
        progressionTime = 0f;
        levelChecksum = 0UL;
    }

    public readonly bool IsValid()
    {
        return bufferID > 0U;
    }

    public readonly bool IsSame(ref pBuffersSummary sum)
    {
        if (sum.levelChecksum != levelChecksum)
        {
            _logger.Error(sum.levelChecksum + " != " + levelChecksum);
            return false;
        }
        if (sum.bufferID != bufferID)
        {
            _logger.Error(sum.bufferID + " != " + bufferID);
            return false;
        }
        if (Mathf.Abs(sum.progressionTime - progressionTime) > 1f)
        {
            _logger.Error(sum.progressionTime + " != " + progressionTime);
            return false;
        }
        return true;
    }

    private static readonly IArchiveLogger _logger = LoaderWrapper.CreateArSubLoggerInstance(nameof(pBuffersSummary));

    public SNetExt_BufferType bufferType;

    public ulong levelChecksum;

    public uint bufferID;

    public float progressionTime;
}
