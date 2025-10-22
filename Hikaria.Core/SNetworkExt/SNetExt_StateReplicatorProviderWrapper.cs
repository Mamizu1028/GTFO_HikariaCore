using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

internal class SNetExt_StateReplicatorProviderWrapper<S> : MonoBehaviour, ISNetExt_StateReplicatorProvider<S> where S : struct
{
    [HideFromIl2Cpp]
    public ISNetExt_StateReplicator GetStateReplicator()
    {
        return m_stateReplicator;
    }

    [HideFromIl2Cpp]
    public void OnStateChange(S oldState, S newState, bool isRecall)
    {
        m_onStateChange(m_stateReplicator, oldState, newState, isRecall);
    }

    [HideFromIl2Cpp]
    public void Setup(SNetExt_StateReplicator<S> stateReplicator, Action<SNetExt_StateReplicator<S>, S, S, bool> onStateChange)
    {
        m_stateReplicator = stateReplicator;
        m_onStateChange = onStateChange;
    }

    private void OnDestroy()
    {
        SNetExt_Capture.UnRegisterForDropInCallback(m_stateReplicator);
        SNetExt_Replication.DeallocateReplicator(m_stateReplicator.Replicator);
    }

    private Action<SNetExt_StateReplicator<S>, S, S, bool> m_onStateChange;
    private SNetExt_StateReplicator<S> m_stateReplicator;
}

internal class SNetExt_StateReplicatorProviderWrapper<S, I> : MonoBehaviour, ISNetExt_StateReplicatorProvider<S, I> where S : struct where I : struct
{
    [HideFromIl2Cpp]
    public ISNetExt_StateReplicator GetStateReplicator()
    {
        return m_stateReplicator;
    }

    [HideFromIl2Cpp]
    public void OnStateChange(S oldState, S newState, bool isRecall)
    {
        m_onStateChange(m_stateReplicator, oldState, newState, isRecall);
    }

    [HideFromIl2Cpp]
    public void AttemptInteract(I interaction)
    {
        m_attemptInteract(m_stateReplicator, interaction);
    }

    [HideFromIl2Cpp]
    public void Setup(SNetExt_StateReplicator<S, I> stateReplicator, Action<SNetExt_StateReplicator<S, I>, S, S, bool> onStateChange, Action<SNetExt_StateReplicator<S, I>, I> attemptInteract)
    {
        m_stateReplicator = stateReplicator;
        m_onStateChange = onStateChange;
        m_attemptInteract = attemptInteract;
    }

    private void OnDestroy()
    {
        SNetExt_Capture.UnRegisterForDropInCallback(m_stateReplicator);
        SNetExt_Replication.DeallocateReplicator(m_stateReplicator.Replicator);
    }

    private Action<SNetExt_StateReplicator<S, I>, S, S, bool> m_onStateChange;
    private Action<SNetExt_StateReplicator<S, I>, I> m_attemptInteract;
    private SNetExt_StateReplicator<S, I> m_stateReplicator;
}
