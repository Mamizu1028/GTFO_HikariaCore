using Player;
using UnityEngine;

namespace Hikaria.Core.Components;

public class Interact_Base : MonoBehaviourExtended, Interfaces.IInteractable
{
    public virtual KeyCode InputKey => KeyCode.None;

    public virtual bool ManualTriggeringOnly => false;

    public virtual bool AllowTriggerWithCarryItem { get; set; }

    public virtual void SetupFromItem(Item item) { }

    public virtual bool OnlyActiveWhenLookingStraightAt { get; set; }

    public virtual void SetActive(bool active)
    {
        m_isActive = active;
        enabled = active;
        if (m_colliderToOwn != null)
        {
            m_colliderToOwn.enabled = active;
        }
    }

    public bool IsActive => m_isActive;

    public void SetBlocked(bool state) => m_isBlocked = state;

    public bool IsBlocked => m_isBlocked;

    public virtual bool PlayerCanInteract(PlayerAgent source) => m_isActive;

    public bool RequireCollisionCheck { get; set; } = true;

    public virtual bool PlayerDoInteract(PlayerAgent source) => false;

    public bool IsSelected { get; private set; }

    public virtual void PlayerSetSelected(bool selected, PlayerAgent agent)
    {
        bool flag = (selected && !GuiManager.InteractionLayer.InteractPromptVisible);
        if (IsSelected != selected || flag)
        {
            OnSelectedChange(selected, agent, flag);
        }
        IsSelected = selected;
    }

    protected virtual void OnSelectedChange(bool selected, PlayerAgent agent, bool forceUpdate = false) { }

    public virtual bool PlayerCheckInput(PlayerAgent agent) => Input.GetKey(InputKey);

    public virtual void OnProximityEnter(PlayerAgent agent) { }
    public virtual void OnProximityExit(PlayerAgent agent) { }

    public virtual void LocalPickup(PlayerAgent agent) { }
    public virtual void SyncedPickup(PlayerAgent agent) { }

    protected virtual void Update() { }

    public Transform Root => transform;

    public Collider m_colliderToOwn;

    public Action OnPickedUp;

    private bool m_isBlocked;

    private bool m_isActive = true;
}
