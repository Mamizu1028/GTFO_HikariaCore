using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core.Interfaces;
using SNetwork;
using System.Collections;
using TheArchive.Interfaces;
using TheArchive.Loader;
using TheArchive.Utilities;
using UnityEngine;

namespace Hikaria.Core.Managers;

internal class PauseManager : MonoBehaviour
{
    private void Awake()
    {
        Current = this;
    }

    private void SetPaused()
    {
        if (SNet.IsMaster && GameStateManager.CurrentStateName == eGameStateName.InLevel)
        {
            SNet.Capture.CaptureGameState(eBufferType.Migration_A);
        }
        if (m_pauseUpdateCoroutine != null)
        {
            StopCoroutine(m_pauseUpdateCoroutine);
        }
        m_pauseUpdateCoroutine = StartCoroutine(UpdateRegistered().WrapToIl2Cpp());
        foreach (IPauseable pauseable in m_pausableUpdaters)
        {
            try
            {
                pauseable.OnPaused();
            }
            catch
            {
            }
        }

        Utils.SafeInvoke(OnPaused);
        Logger.Notice("Game Paused");
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
            try
            {
                pauseable.OnUnpaused();
            }
            catch
            {
            }
        }
        Utils.SafeInvoke(OnUnpaused);
        if (SNet.IsMaster && GameStateManager.CurrentStateName == eGameStateName.InLevel)
        {
            SNet.Sync.StartRecallWithAllSyncedPlayers(eBufferType.Migration_A, false);
        }
        Logger.Notice("Game Unpaused");
    }

    private IEnumerator UpdateRegistered()
    {
        var yielder = new WaitForSecondsRealtime(PauseUpdateInterval);
        while (true)
        {
            foreach (IPauseable pauseable in m_pausableUpdaters)
            {
                try
                {
                    pauseable.PausedUpdate();
                }
                catch
                {
                }
            }
            yield return yielder;
        }
    }

    public static void RegisterPauseable(IPauseable pu)
    {
        m_pausableUpdaters.Add(pu);
    }

    public static void DeregisterPauseable(IPauseable pu)
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
                    return;
                }
                Current.SetUnpaused();
            }
        }
    }

    public static float PauseUpdateInterval => Time.fixedUnscaledDeltaTime;

    private Coroutine m_pauseUpdateCoroutine;

    public static PauseManager Current { get; private set; }

    private static HashSet<IPauseable> m_pausableUpdaters = new();

    private static bool s_isPaused;

    public static event Action OnPaused;
    public static event Action OnUnpaused;

    private static IArchiveLogger _logger;

    private static IArchiveLogger Logger => _logger ??= LoaderWrapper.CreateLoggerInstance(nameof(PauseManager));
}