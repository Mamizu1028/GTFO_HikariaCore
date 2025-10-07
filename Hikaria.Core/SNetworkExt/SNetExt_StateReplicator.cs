using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_StateReplicator<S> : ISNetExt_StateReplicator, ICaptureCallbackObject, IReplicatorSupplier where S : struct
{
    public string Key => typeof(S).FullName;

    public GameObject gameObject => m_provider.gameObject;

    public string name
    {
        get
        {
            return m_provider.gameObject.name;
        }
        set
        {
            m_provider.gameObject.name = value;
        }
    }

    public IReplicator Replicator { get; set; }

    public IReplicator GetReplicator() => Replicator;

    public static SNetExt_StateReplicator<S> Create(ISNetExt_StateReplicatorProvider<S> provider, SNetExt_ReplicatorLifeTime replictorLifeTime, S startingState = default, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var stateReplicator = new SNetExt_StateReplicator<S>
        {
            m_provider = provider
        };
        switch (replictorLifeTime)
        {
            case SNetExt_ReplicatorLifeTime.NeverDestroyed:
                stateReplicator.Replicator = SNetExt_Replication.AddManagerReplicator(stateReplicator);
                break;
            case SNetExt_ReplicatorLifeTime.DestroyedOnLevelReset:
                {
                    stateReplicator.Replicator = SNetExt_Replication.AddSelfManagedReplicator(stateReplicator);
                    break;
                }
            default:
                Logs.LogError("ERROR : SNetExt_StateReplicator does not support " + replictorLifeTime);
                break;
        }
        stateReplicator.m_channelType = channelType;
        stateReplicator.m_statePacket = stateReplicator.Replicator.CreatePacket<S>(typeof(S).FullName, stateReplicator.OnStateChangeReceive, null);
        stateReplicator.m_statePacket_Dropin = stateReplicator.Replicator.CreatePacket<S>(typeof(S).FullName, stateReplicator.OnStateChangeReceive_Dropin, null);
        stateReplicator.m_currentState = startingState;
        SNetExt_Capture.RegisterCaptureCallback(stateReplicator);
        return stateReplicator;
    }

    public pStateReplicatorProvider GetProviderSyncStruct()
    {
        pStateReplicatorProvider provider = default;
        provider.Set(this);
        return provider;
    }

    public S State
    {
        get
        {
            return m_currentState;
        }
        set
        {
            if (SNetwork.SNet.IsMaster)
            {
                m_statePacket.Send(value, m_channelType);
                OnStateChange(value, false);
            }
        }
    }

    public void SetStateUnsynced(S state)
    {
        m_currentState = state;
    }

    private void OnStateChangeReceive(S state)
    {
        if (!SNetExt.Replication.IsLastSenderMaster)
            return;

        OnStateChange(state, false);
    }

    private void OnStateChangeReceive_Dropin(S state)
    {
        OnStateChange(state, true);
    }

    private void OnStateChange(S newState, bool isRecall)
    {
        m_provider.OnStateChange(m_currentState, newState, isRecall);
        m_currentState = newState;
    }

    public void OnStateChange_Master(S newState)
    {
        if (!SNetwork.SNet.IsMaster)
            return;

        OnStateChange(newState, false);
    }

    public void OnStateCapture()
    {
        m_statePacket_Dropin.CaptureToBuffer(m_currentState, SNetwork.eCapturePass.FirstPass);
    }

    internal ISNetExt_StateReplicatorProvider<S> m_provider;

    private SNetwork.SNet_ChannelType m_channelType;

    private SNetExt_ReplicatedPacket<S> m_statePacket;

    private SNetExt_ReplicatedPacket<S> m_statePacket_Dropin;

    private S m_currentState;

    private struct pWrappedState
    {
        public S state;

        public bool isDropIn;
    }
}

public class SNetExt_StateReplicator<S, I> : ISNetExt_StateReplicator, ICaptureCallbackObject, IReplicatorSupplier where S : struct where I : struct
{
    public GameObject gameObject => m_provider.gameObject;

    public string name
    {
        get
        {
            return m_provider.gameObject.name;
        }
        set
        {
            m_provider.gameObject.name = value;
        }
    }

    public string Key => typeof(S).FullName;

    public IReplicator Replicator { get; set; }

    public IReplicator GetReplicator() => Replicator;

    public static SNetExt_StateReplicator<S, I> Create(ISNetExt_StateReplicatorProvider<S, I> provider, SNetExt_ReplicatorLifeTime replictorLifeTime, S startingState = default, SNetwork.SNet_ChannelType channelType = SNetwork.SNet_ChannelType.GameOrderCritical)
    {
        var stateReplicator = new SNetExt_StateReplicator<S, I>
        {
            m_provider = provider
        };
        switch (replictorLifeTime)
        {
            case SNetExt_ReplicatorLifeTime.NeverDestroyed:
                stateReplicator.Replicator = SNetExt_Replication.AddManagerReplicator(stateReplicator);
                break;
            case SNetExt_ReplicatorLifeTime.DestroyedOnLevelReset:
                stateReplicator.Replicator = SNetExt_Replication.AddSelfManagedReplicator(stateReplicator);
                break;
            default:
                Logs.LogError("ERROR : SNetExt_StateReplicator does not support " + replictorLifeTime);
                break;
        }
        stateReplicator.m_channelType = channelType;
        stateReplicator.m_statePacket = stateReplicator.Replicator.CreatePacket<S>(typeof(S).FullName, stateReplicator.OnStateChangeReceive, null);
        stateReplicator.m_statePacket_Recall = stateReplicator.Replicator.CreatePacket<S>(typeof(S).FullName, stateReplicator.OnStateChangeReceive_Recall, null);
        stateReplicator.m_interactionPacket = stateReplicator.Replicator.CreatePacket<I>(typeof(I).FullName, stateReplicator.AttemptInteract, null);
        stateReplicator.m_currentState = startingState;
        SNetExt_Capture.RegisterCaptureCallback(stateReplicator);
        return stateReplicator;
    }

    public pStateReplicatorProvider GetProviderSyncStruct()
    {
        pStateReplicatorProvider provider = default;
        provider.Set(this);
        return provider;
    }

    private void AttemptInteract(I interaction)
    {
        if (!SNetExt.Replication.IsLastSenderInSessionHub)
            return;

        m_provider.AttemptInteract(interaction);
    }

    public void InteractWithState(S newState, I interaction)
    {
        if (SNetwork.SNet.IsMaster)
        {
            m_statePacket.Send(m_currentState, m_channelType);
            OnStateChange(false, newState);
            return;
        }
        if (SNetwork.SNet.HasMaster)
        {
            m_interactionPacket.Send(interaction, m_channelType, SNetwork.SNet.Master);
        }
    }

    public S State
    {
        get
        {
            return m_currentState;
        }
        private set
        {
            OnStateChange(false, value);
        }
    }

    public void SetStateUnsynced(S state)
    {
        m_currentState = state;
    }

    private void OnStateChangeReceive(S state)
    {
        if (!SNetExt.Replication.IsLastSenderMaster)
            return;

        OnStateChange(false, state);
    }

    private void OnStateChangeReceive_Recall(S state)
    {
        OnStateChange(true, state);
    }

    public void OnStateChange(bool isRecall, S newState)
    {
        if (!isRecall && SNetwork.SNet.IsMaster)
        {
            m_statePacket.Send(newState, m_channelType);
        }
        S currentState = m_currentState;
        m_currentState = newState;
        m_provider.OnStateChange(currentState, newState, isRecall);
    }

    public void OnStateChange_Master(S newState)
    {
        if (!SNetwork.SNet.IsMaster)
        {
            return;
        }
        OnStateChange(false, newState);
    }

    public void OnStateCapture()
    {
        m_statePacket_Recall.CaptureToBuffer(m_currentState, CapturePass);
    }

    internal ISNetExt_StateReplicatorProvider<S, I> m_provider;

    private SNetwork.SNet_ChannelType m_channelType;

    private SNetExt_ReplicatedPacket<S> m_statePacket;

    private SNetExt_ReplicatedPacket<S> m_statePacket_Recall;

    private SNetExt_ReplicatedPacket<I> m_interactionPacket;

    private S m_currentState;

    public SNetwork.eCapturePass CapturePass = SNetwork.eCapturePass.FirstPass;

    private struct pWrappedState
    {
        public S state;

        public bool isRecall;
    }
}
