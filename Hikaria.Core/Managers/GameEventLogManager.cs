using Hikaria.Core.Interfaces;
using UnityEngine;

namespace Hikaria.Core.Managers;

public class GameEventLogManager : MonoBehaviour, IPauseable
{
    private void Awake()
    {
        Instance = this;
        PauseManager.RegisterPauseable(this);
    }

    private void OnDestroy()
    {
        PauseManager.DeregisterPauseable(this);
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer < _updateInterval)
            return;
        timer = 0f;
        if (queue.TryDequeue(out var log))
        {
            PageLoadoutLog.AddLogItem(log);
            PlayerLayerLog.AddLogItem(log);
        }
    }

    public static void AddLog(string log)
    {
        queue.Enqueue(log);
    }

    public static void AddLogInSeparate(string log, int chunkSize = 50)
    {
        foreach (var str in log.SplitInChunks(chunkSize))
        {
            queue.Enqueue(str);
        }
    }

    public void PausedUpdate()
    {
        timer += PauseManager.PauseUpdateInterval;
        if (timer < _updateInterval)
            return;
        timer = 0f;
        if (queue.TryDequeue(out var log))
        {
            PageLoadoutLog.AddLogItem(log);
            PlayerLayerLog.AddLogItem(log);
        }
    }

    public void OnPaused()
    {
    }

    public void OnUnpaused()
    {
    }

    public static GameEventLogManager Instance { get; private set; }

    private static Queue<string> queue = new();
    private float timer = 0f;
    private const float _updateInterval = 1 / 3f;
    private static PUI_GameEventLog PageLoadoutLog => MainMenuGuiLayer.Current.PageLoadout.m_gameEventLog;
    private static PUI_GameEventLog PlayerLayerLog => GuiManager.PlayerLayer.m_gameEventLog;
}
