using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public interface IReplicatorSupplier
{
    string Key { get; }

    IReplicator Replicator { get; set; }

    GameObject gameObject { get; }
}