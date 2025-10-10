using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_ReplicatorSupplier
{
    string Key { get; }

    ISNetExt_Replicator Replicator { get; set; }

    GameObject gameObject { get; }
}