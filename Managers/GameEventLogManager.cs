using Hikaria.Core.Extensions;
using Hikaria.Core.Interfaces;
using UnityEngine;

namespace Hikaria.Core.Managers;

public class GameEventLogManager : MonoBehaviour, IPauseable
{
    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer < 0.4)
            return;
        _timer = 0f;
        if (queue.TryDequeue(out var log))
        {
            PageLoadoutLog.AddLogItem(log);
            PlayerLayerLog.AddLogItem(log);
        }
    }

    public void AddLog(string log)
    {
        queue.Enqueue(log);
    }

    public void AddLogInSeparate(string log, int chunkSize = 50)
    {
        foreach (var str in log.SplitInChunks(chunkSize))
        {
            queue.Enqueue(str);
        }
    }

    public void PausedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer < 0.4)
            return;
        _timer = 0f;
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

    private Queue<string> queue = new();
    private float _timer = 0f;
    private PUI_GameEventLog PageLoadoutLog => MainMenuGuiLayer.Current.PageLoadout.m_gameEventLog;
    private PUI_GameEventLog PlayerLayerLog => GuiManager.PlayerLayer.m_gameEventLog;
}
