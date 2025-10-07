using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public interface ISNetExt_StateReplicatorProvider
{
    ISNetExt_StateReplicator GetStateReplicator();

    GameObject gameObject { get; }
}

public interface ISNetExt_StateReplicatorProvider<S> : ISNetExt_StateReplicatorProvider where S : struct
{
    void OnStateChange(S oldState, S newState, bool isRecall);
}

public interface ISNetExt_StateReplicatorProvider<S, I> : ISNetExt_StateReplicatorProvider where S : struct where I : struct
{
    void OnStateChange(S oldState, S newState, bool isRecall);

    void AttemptInteract(I interaction);
}
