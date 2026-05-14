using TheArchive.Interfaces;
using TheArchive.Loader;

namespace Hikaria.Core.SNetworkExt;

public sealed class SNetExt_Replicator_VanillaWrapper : SNetExt_Replicator
{
    private static readonly IArchiveLogger _logger =
        LoaderWrapper.CreateArSubLoggerInstance(nameof(SNetExt_Replicator_VanillaWrapper));

    private SNetwork.IReplicator m_vanillaInterface;
    private SNetwork.SNet_Replicator m_vanilla;

    public SNetwork.IReplicator Vanilla => m_vanillaInterface;

    public bool IsAlive => m_vanillaInterface != null;

    public SNetwork.SNet_ReplicatorType VanillaType => m_vanillaInterface.Type;

    public override SNetExt_ReplicatorType Type => SNetExt_ReplicatorType.VanillaWrapper;

    public override SNetwork.SNet_Player OwningPlayer => m_vanillaInterface?.OwningPlayer;

    public override bool OwnedByMaster => m_vanilla != null && m_vanilla.OwnedByMaster;

    public override bool LocallyOwned => m_vanillaInterface != null && m_vanillaInterface.LocallyOwned;

    internal SNetExt_Replicator_VanillaWrapper(SNetwork.IReplicator vanilla)
    {
        m_vanillaInterface = vanilla ?? throw new ArgumentNullException(nameof(vanilla));
        m_vanilla = vanilla.Cast<SNetwork.SNet_Replicator>();
        ((ISNetExt_MutableReplicator)this).AssignKey($"VanillaWrapper_{m_vanilla.Key}");
    }

    public override void Despawn()
    {
        SNetExt_Replication.DeallocateVanillaWrapper(this);
    }
}
