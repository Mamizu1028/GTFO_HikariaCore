using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public interface IReplicatorSupplier
{
    string Key { get; }

    string name { get; set; }

    IReplicator Replicator { get; set; }

    GameObject gameObject { get; }
}