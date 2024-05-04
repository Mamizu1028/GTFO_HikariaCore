using BepInEx.Unity.IL2CPP.Utils;
using Hikaria.Core.Interfaces;
using SNetwork;
using System.Collections;
using UnityEngine;

namespace Hikaria.Core.Managers;

internal class PauseManager : MonoBehaviour
{
    public static void Setup()
    {
        if (s_Object == null)
        {
            s_Object = new(typeof(PauseManager).FullName);
            GameObject.DontDestroyOnLoad(s_Object);
            s_Object.AddComponent<PauseManager>();
        }
    }

    private void Awake()
    {
        Current = this;
    }

    private void SetPaused()
    {
        if (SNet.IsMaster)
        {
            SNet.Capture.CaptureGameState(eBufferType.Migration_A);
        }
        if (m_pauseUpdateCoroutine != null)
        {
            StopCoroutine(m_pauseUpdateCoroutine);
        }
        m_pauseUpdateCoroutine = this.StartCoroutine(UpdateRegistered());
        foreach (IPauseable pauseable in m_pausableUpdaters)
        {
            pauseable.OnPaused();
        }
    }

    private void SetUnpaused()
    {
        if (m_pauseUpdateCoroutine != null)
        {
            StopCoroutine(m_pauseUpdateCoroutine);
            m_pauseUpdateCoroutine = null;
        }
        foreach (IPauseable pauseable in m_pausableUpdaters)
        {
            pauseable.OnUnpaused();
        }
        if (SNet.IsMaster)
        {
            SNet.Sync.StartRecallWithAllSyncedPlayers(eBufferType.Migration_A, false);
        }
    }

    private IEnumerator UpdateRegistered()
    {
        var yielder = new WaitForSecondsRealtime(PauseUpdateInterval);
        while (true)
        {
            foreach (IPauseable pauseable in m_pausableUpdaters)
            {
                pauseable.PausedUpdate();
            }
            yield return yielder;
        }
    }

    public static void RegisterPauseable(IPauseable pu)
    {
        m_pausableUpdaters.Add(pu);
    }

    public static void UnregisterPauseable(IPauseable pu)
    {
        m_pausableUpdaters.Remove(pu);
    }

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

    public static float PauseUpdateInterval => Time.fixedDeltaTime;

    private Coroutine m_pauseUpdateCoroutine;

    public static PauseManager Current;

    private static HashSet<IPauseable> m_pausableUpdaters = new();

    private static bool s_isPaused;

    private static GameObject s_Object;
}