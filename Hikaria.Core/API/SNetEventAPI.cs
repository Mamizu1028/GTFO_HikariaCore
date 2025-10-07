using Hikaria.Core.Features.Dev;
using SNetwork;

namespace Hikaria.Core;

public static class SNetEventAPI
{
    public static event Action<eBufferType> OnBufferCapture { add => SNetEventAPI_Impl.OnBufferCapture += value; remove => SNetEventAPI_Impl.OnBufferCapture -= value; }
    public static event Action<eBufferType> OnBufferRecalled { add => SNetEventAPI_Impl.OnBufferRecalled += value; remove => SNetEventAPI_Impl.OnBufferRecalled -= value; }
    public static event Action<eBufferType> OnRecallDone { add => SNetEventAPI_Impl.OnRecallDone += value; remove => SNetEventAPI_Impl.OnRecallDone -= value; }
    public static event Action<eBufferType> OnRecallComplete { add => SNetEventAPI_Impl.OnRecallComplete += value; remove => SNetEventAPI_Impl.OnRecallComplete -= value; }
    public static event Action<eBufferType> OnPrepareForRecall { add => SNetEventAPI_Impl.OnPrepareForRecall += value; remove => SNetEventAPI_Impl.OnPrepareForRecall -= value; }
    public static event Action<pBufferCommand> OnBufferCommand { add => SNetEventAPI_Impl.OnBufferCommand += value; remove => SNetEventAPI_Impl.OnBufferCommand -= value; }
    public static event Action OnResetSession { add => SNetEventAPI_Impl.OnResetSession += value; remove => SNetEventAPI_Impl.OnResetSession -= value; }
    public static event Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason> OnPlayerEvent { add => SNetEventAPI_Impl.OnPlayerEvent += value; remove => SNetEventAPI_Impl.OnPlayerEvent -= value; }
    public static event Action<SNet_Player, SessionMemberEvent> OnSessionMemberChanged { add => SNetEventAPI_Impl.OnSessionMemberChanged += value; remove => SNetEventAPI_Impl.OnSessionMemberChanged -= value; }
    public static event Action OnMasterChanged { add => SNetEventAPI_Impl.OnMasterChanged += value; remove => SNetEventAPI_Impl.OnMasterChanged -= value; }
    public static event Action<eMasterCommandType, int> OnMasterCommand { add => SNetEventAPI_Impl.OnMasterCommand += value; remove => SNetEventAPI_Impl.OnMasterCommand -= value; }
}
