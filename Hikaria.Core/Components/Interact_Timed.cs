using AK;
using Localization;
using Player;
using UnityEngine;

namespace Hikaria.Core.Components;

public class Interact_Timed : Interact_Base
{
    public virtual float InteractDuration { get => m_interactDuration; set => m_interactDuration = value; }

    public event Action OnInteractionEvaluationAbort;

    public event Action<PlayerAgent> OnInteractionTriggered;

    public event Action<PlayerAgent, bool> OnInteractionSelected;

    public event Func<PlayerAgent, bool> ExternalPlayerCanInteract;

    public event Func<string> OnAdditionalInteractionText;

    public virtual uint SFXInteractStart { get; set; } = EVENTS.REVIVELOOP;

    public virtual uint SFXInteractCancel { get; set; } = EVENTS.REVIVECANCEL;

    public virtual uint SFXInteractEnd { get; set; } = EVENTS.REVIVEEND;

    public virtual uint SFXInteractTrigger { get; set; }

    public float InteractionTimerRel => m_timerProgressRel;

    protected virtual bool InternalAllowInput => true;

    public bool TimerIsActive => m_timerIsActive;

    public virtual string InteractionMessage { get; set; } = "Surprise";

    public bool AbortOnDotOrDistanceDiff { get; set; } = true;

    protected virtual void Start() { }

    protected override void Update()
    {
        base.Update();
        float num = 0f;
        for (int i = 0; i < m_interactors.Count; i++)
        {
            num = ApplySpeedModifier(m_interactors[i].Agent, 1f);
        }
        if (num > 0f && m_timerProgressRel != 1f)
        {
            m_timerProgressRel += Time.deltaTime * num / InteractDuration;
            m_timerProgressRel = Mathf.Min(1f, m_timerProgressRel);
            OnTimerUpdate(m_timerProgressRel);
        }
    }

    public virtual void Setup(PlayerAgent owner)
    {
        m_interactTargetAgent = owner;
    }

    protected virtual float ApplySpeedModifier(PlayerAgent sourceAgent, float dt)
    {
        return dt;
    }

    public virtual string GetAdditionalInteractionInfo()
    {
        if (OnAdditionalInteractionText != null)
        {
            return OnAdditionalInteractionText();
        }
        return string.Empty;
    }

    public override bool PlayerCanInteract(PlayerAgent source)
    {
        if (base.PlayerCanInteract(source))
        {
            return ExternalPlayerCanInteract?.Invoke(source) ?? true;
        }
        return false;
    }
    
    public void ForceUpdatePrompt()
    {
        if (IsSelected)
        {
            GuiManager.InteractionLayer.SetInteractPrompt(InteractionMessage + GetAdditionalInteractionInfo(), string.Format(Text.Get(827U), InputKey), ePUIMessageStyle.Default);
        }
    }

    protected override void OnSelectedChange(bool selected, PlayerAgent agent, bool forceUpdate = false)
    {
        if ((selected && !m_hasShownInteractionPrompt) || (!selected && m_hasShownInteractionPrompt) || forceUpdate)
        {
            GuiManager.InteractionLayer.InteractPromptVisible = selected;
        }
        if (selected)
        {
            GuiManager.InteractionLayer.SetInteractPrompt(InteractionMessage + GetAdditionalInteractionInfo(), string.Format(Text.Get(827U), InputKey), ePUIMessageStyle.Default);
        }
        m_hasShownInteractionPrompt = selected;
        TriggerOnInteractionSelectedCallback(agent, selected);
        if (!selected)
        {
            OnInteractorStateChanged(agent, false);
        }
    }

    public void TriggerOnInteractionSelectedCallback(PlayerAgent agent, bool selected)
    {
        OnInteractionSelected?.Invoke(agent, selected);
    }

    public override void SetActive(bool active)
    {
        if (m_timerIsActive)
        {
            SetTimerActive(false);
            SetUIState(false, false);
        }
        base.SetActive(active);
    }

    public override bool PlayerCheckInput(PlayerAgent agent)
    {
        if (!InternalAllowInput)
        {
            OnInteractorStateChanged(agent, false);
            return false;
        }
        if (m_localPlayerInteractInfo == null)
        {
            if (Input.GetKey(InputKey))
            {
                OnInteractorStateChanged(agent, true);
            }
        }
        else if (Input.GetKey(InputKey) && EvaluateTimedInteraction())
        {
            if (InteractionTimerRel == 1f)
            {
                OnInteractorCompleted(agent);
                return true;
            }
        }
        else
        {
            OnInteractorStateChanged(agent, false);
        }
        return false;
    }

    public override bool PlayerDoInteract(PlayerAgent source)
    {
        TriggerInteractionAction(source);
        return true;
    }

    protected virtual bool EvaluateTimedInteraction()
    {
        if (AbortOnDotOrDistanceDiff)
        {
            PlayerAgent agent = m_localPlayerInteractInfo.Agent;
            float num = Vector3.Dot(agent.FPSCamera.Forward, m_triggerStartAgentLookDir);
            float magnitude = (m_triggerStartAgentWorldPos - agent.Position).magnitude;
            if (num < m_minCamDotAllowed || magnitude > m_maxMoveDisAllowed)
            {
                return false;
            }
        }
        return true;
    }

    protected virtual void OnTimerUpdate(float timeRel)
    {
        GuiManager.InteractionLayer.SetTimer(timeRel);
    }

    protected virtual void TriggerInteractionAction(PlayerAgent source)
    {
        CheckSoundPlayer();
        m_sound.Post(SFXInteractEnd, true);
        if (SFXInteractTrigger > 0U)
        {
            m_sound.Post(SFXInteractTrigger, true);
        }
        source ??= PlayerManager.GetLocalPlayerAgent();
        OnInteractionTriggered?.Invoke(source);
    }

    private void CheckSoundPlayer()
    {
        m_sound ??= new CellSoundPlayer(transform.position);
    }

    protected virtual void SetTimerActive(bool active)
    {
        CheckSoundPlayer();
        if (active)
        {
            if (!m_timerIsActive)
            {
                m_sound.Post(SFXInteractStart, true);
                m_timerProgressRel = 0f;
            }
        }
        else if (m_timerIsActive)
        {
            m_sound.Post(SFXInteractCancel, true);
            m_timerProgressRel = 0f;
        }
        m_timerIsActive = active;
    }

    protected virtual void SetUIState(bool state, bool fadeOut)
    {
        if (state)
        {
            if (NeedsUI())
            {
                m_UIState = true;
                GuiManager.InteractionLayer.SetTimer(m_timerProgressRel);
                GuiManager.InteractionLayer.TimerVisible = true;
                GuiManager.InteractionLayer.SetTimerAlphaMul(1f);
                return;
            }
        }
        else if (m_UIState && NeedsUI())
        {
            m_UIState = false;
            if (fadeOut)
            {
                float num = 0.1f;
                GuiManager.InteractionLayer.TimerFlash(num);
                Callback(new Action(HideTimer), num * 2f);
                return;
            }
            HideTimer();
        }
    }

    protected bool NeedsUI()
    {
        return (m_interactTargetAgent != null && m_interactTargetAgent.IsLocallyOwned) || m_localPlayerInteractInfo != null;
    }

    private void HideTimer()
    {
        GuiManager.InteractionLayer.TimerVisible = false;
    }

    public void OnInteractorCompleted(PlayerAgent sourceAgent)
    {
        OnTimerUpdate(1f);
        SetTimerActive(false);
        SetUIState(false, true);
        OnInteractorStateChanged(sourceAgent, false);
    }

    public virtual void OnInteractorStateChanged(PlayerAgent sourceAgent, bool state)
    {
        if (state)
        {
            for (int i = 0; i < m_interactors.Count; i++)
            {
                if (m_interactors[i].Agent == sourceAgent)
                {
                    return;
                }
            }
            InteractorInfo interactorInfo = new()
            {
                Agent = sourceAgent
            };
            m_interactors.Add(interactorInfo);
            if (sourceAgent.IsLocallyOwned)
            {
                m_localPlayerInteractInfo = interactorInfo;
                m_triggerStartAgentWorldPos = sourceAgent.Position;
                m_triggerStartAgentLookDir = sourceAgent.FPSCamera.Forward;
            }
            SetTimerActive(true);
            SetUIState(true, false);
            return;
        }
        for (int j = 0; j < m_interactors.Count; j++)
        {
            if (m_interactors[j].Agent == sourceAgent)
            {
                if (m_interactors.Count == 1)
                {
                    SetTimerActive(false);
                }
                if (m_interactors.Count == 1 || sourceAgent.IsLocallyOwned)
                {
                    SetUIState(false, false);
                }
                m_interactors.RemoveAt(j);
                if (sourceAgent.IsLocallyOwned)
                {
                    m_localPlayerInteractInfo = null;
                }
                return;
            }
        }
    }

    private float m_interactDuration = 0.6f;

    protected PlayerAgent m_interactTargetAgent;

    protected bool m_timerIsActive;

    private bool m_UIState;

    protected InteractorInfo m_localPlayerInteractInfo;

    private CellSoundPlayer m_sound;

    protected List<InteractorInfo> m_interactors = new List<InteractorInfo>();

    protected bool m_hasShownInteractionPrompt;

    private Vector3 m_triggerStartAgentWorldPos;

    private Vector3 m_triggerStartAgentLookDir;

    private float m_minCamDotAllowed = 0.5f;

    private float m_maxMoveDisAllowed = 2f;

    private float m_timerProgressRel;

    protected class InteractorInfo
    {
        public PlayerAgent Agent;
    }
}
