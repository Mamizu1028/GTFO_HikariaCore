using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_DynamicReplication
{
    pReplicationData ReplicationData { get; set; }

    Vector3 Position { get; set; }

    Quaternion Rotation { get; set; }
}
