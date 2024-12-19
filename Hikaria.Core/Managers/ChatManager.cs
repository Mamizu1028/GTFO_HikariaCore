using Hikaria.Core.Extensions;
using Hikaria.Core.Interfaces;
using Player;
using SNetwork;
using UnityEngine;

namespace Hikaria.Core.Managers;

public sealed class ChatManager : MonoBehaviour, IPauseable
{
    private void Awake()
    {
        Instance = this;
        GameEventAPI.RegisterListener(this);
    }
    private void OnDestroy()
    {
        GameEventAPI.UnregisterListener(this);
    }

    private void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer < 0.333)
        {
            return;
        }
        _timer = 0f;
        if (queue.TryDequeue(out var item))
        {
            Speak(item);
        }
    }

    public static void AddQueue(string[] msgs, SNet_Player player = null)
    {
        List<string> result = new List<string>();
        foreach (string str1 in msgs)
        {
            foreach (string str2 in str1.Split('\n'))
            {
                if (str2.Length > 50)
                {
                    foreach (string str3 in str2.SplitInChunks(50))
                    {
                        result.Add(str3);
                    }
                }
                else
                {
                    result.Add(str2);
                }
            }
        }
        foreach (string msg in result)
        {
            queue.Enqueue(Tuple.Create(msg, player));
        }
    }

    public static void AddQueue(string msg, SNet_Player player = null)
    {
        string[] msgs = msg.Split("\n");
        if (msgs.Length == 1)
        {
            if (msgs[0].Length > 50)
            {
                string[] splitMsgs = msgs[0].SplitInChunks(50);
                foreach (string splitMsg in splitMsgs)
                {
                    queue.Enqueue(Tuple.Create(splitMsg, player));
                }
            }
            else
            {
                queue.Enqueue(Tuple.Create(msgs[0], player));
            }
        }
        else
        {
            foreach (string splitMsg in msgs)
            {
                queue.Enqueue(Tuple.Create(splitMsg, player));
            }
        }
    }

    public static void ClearQueue()
    {
        queue.Clear();
    }

    private static void Speak(Tuple<string, SNet_Player> tuple)
    {
        if (tuple.Item2 != null && tuple.Item2.HasPlayerAgent)
        {
            PlayerAgent playerAgent = tuple.Item2.PlayerAgent.Cast<PlayerAgent>();
            PlayerChatManager.WantToSentTextMessage(PlayerManager.GetLocalPlayerAgent(), tuple.Item1, playerAgent);
        }
        else
        {
            PlayerChatManager.WantToSentTextMessage(PlayerManager.GetLocalPlayerAgent(), tuple.Item1);
        }
    }

    public void PausedUpdate()
    {
        _timer += PauseManager.PauseUpdateInterval;
        if (_timer < 0.333)
        {
            return;
        }
        _timer = 0f;
        if (queue.TryDequeue(out var item))
        {
            Speak(item);
        }
    }

    public void OnPaused()
    {
    }

    public void OnUnpaused()
    {
    }

    private static Queue<Tuple<string, SNet_Player>> queue = new();

    private float _timer = 0f;

    public static ChatManager Instance { get; private set; }
}