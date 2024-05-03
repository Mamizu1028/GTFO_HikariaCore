using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core.Interfaces;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace Hikaria.Core.Managers;

public sealed class PauseManager : MonoBehaviour
{
    private void Awake()
    {
        Current = this;
    }

    private void SetPaused()
    {
        SNet.Capture.CaptureGameState(eBufferType.Migration_A);
        if (_PauseUpdateCoroutine != null)
        {
            StopCoroutine(_PauseUpdateCoroutine);
        }
        _PauseUpdateCoroutine = StartCoroutine(UpdateRegistered().WrapToIl2Cpp());
        foreach (IPauseable item in _updaters)
        {
            item.OnPaused();
        }
    }

    private void SetUnpaused()
    {
        if (_PauseUpdateCoroutine != null)
        {
            StopCoroutine(_PauseUpdateCoroutine);
            _PauseUpdateCoroutine = null;
        }
        foreach (IPauseable item in _updaters)
        {
            item.OnUnpaused();
        }
        SNet.Sync.StartRecallWithAllSyncedPlayers(eBufferType.Migration_A, false);
    }

    private IEnumerator UpdateRegistered()
    {
        while (true)
        {
            foreach (IPauseable item in _updaters)
            {
                item.PausedUpdate();
            }
            yield return new WaitForSecondsRealtime(PauseUpdateInterval);
        }
    }

    public void RegisterPauseable(IPauseable pu)
    {
        _updaters.Add(pu);
    }

    public void UnregisterPauseable(IPauseable pu)
    {
        _updaters.Remove(pu);
    }

    private Coroutine _PauseUpdateCoroutine;

    public static PauseManager Current;

    private readonly HashSet<IPauseable> _updaters = new();

    public static bool IsPaused
    {
        get
        {
            return s_isPaused;
        }
        set
        {
            if (s_isPaused != value)
            {
                s_isPaused = value;
                if (value)
                {
                    Current.SetPaused();
                    global::PauseManager.IsPaused = true;
                    return;
                }
                Current.SetUnpaused();
                global::PauseManager.IsPaused = false;
            }
        }
    }

    private static bool s_isPaused;

    public static float PauseUpdateInterval => Time.fixedDeltaTime;
}
