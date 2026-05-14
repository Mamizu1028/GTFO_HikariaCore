namespace Hikaria.Core.SNetworkExt;

internal interface ISNetExt_OwnedReplicator : ISNetExt_MutableReplicator
{
    new SNetwork.SNet_Player OwningPlayer { get; set; }

    new bool OwnedByMaster { get; set; }
}
