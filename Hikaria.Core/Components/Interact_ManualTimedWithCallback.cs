using AK;
using Player;
using UnityEngine;

namespace Hikaria.Core.Components;

public class Interact_ManualTimedWithCallback : Interact_Timed
{
    public override bool ManualTriggeringOnly => true;

    public override KeyCode InputKey => m_inputKey;

    public Action OnTrigger { get; set; }

    public override uint SFXInteractStart { get; set; } = EVENTS.INTERACT_TOOL_START;
    public override uint SFXInteractCancel { get; set; } = EVENTS.INTERACT_TOOL_CANCEL;
    public override uint SFXInteractEnd { get; set; } = EVENTS.INTERACT_TOOL_FINISHED;

    public void SetAction(string desc, KeyCode inputKey)
    {
        InteractionMessage = desc;
        m_inputKey = inputKey;
    }

    public void ManualUpdateWithCondition(bool condition, PlayerAgent source, bool selectedOnIdle = false)
    {
        if (condition)
        {
            if (PlayerCheckInput(source))
            {
                PlayerDoInteract(source);
                PlayerSetSelected(false, source);
                return;
            }
            if (InteractionTimerRel > 0f && !IsSelected)
            {
                PlayerSetSelected(true, source);
                return;
            }
            if (InteractionTimerRel <= 0f && IsSelected != selectedOnIdle)
            {
                PlayerSetSelected(selectedOnIdle, source);
                return;
            }
        }
        else if (IsSelected)
        {
            PlayerSetSelected(false, source);
        }
    }

    protected override void TriggerInteractionAction(PlayerAgent source)
    {
        base.TriggerInteractionAction(source);
        OnTrigger?.Invoke();
    }

    private KeyCode m_inputKey;
}
