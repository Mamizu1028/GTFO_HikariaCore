using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core.SNetworkExt;
using SNetwork;
using System.Collections;
using TheArchive.Interfaces;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.Core.Managers;

public static class PopupMessageManager
{
    static PopupMessageManager()
    {
        s_PopupMessagePacket = SNetExt_Packet<pPopupMessage>.Create(typeof(pPopupMessage).FullName, OnReceivePopupMessage, null, true, SNet_ChannelType.SessionOrderCritical);
    }

    internal static void Setup()
    {
        CoroutineManager.StartPersistantCoroutine(ShowAllPopups().WrapToIl2Cpp());
    }

    private static IArchiveLogger _logger;
    private static IArchiveLogger Logger = _logger ??= LoaderWrapper.CreateLoggerInstance(nameof(PopupMessageManager));

    private static SNetExt_Packet<pPopupMessage> s_PopupMessagePacket;

    private static void OnReceivePopupMessage(ulong sender, pPopupMessage data)
    {
        Logger.Notice($"Receive pPopupMessage from {sender}");
        ShowPopup(data.UnpackPopupMessage());
    }

    public static void SendPopupMessage(pPopupMessage data, params SNet_Player[] players)
    {
        s_PopupMessagePacket.Send(data, players);
    }
    
    private static void Empty()
    {
    }

    public static Action EmptyAction { get; private set; } = new(Empty);

    private static readonly Queue<PopupMessage> _popupQueue = new Queue<PopupMessage>();

    public static void ShowPopup(PopupMessage popupMessage)
    {
        _popupQueue.Enqueue(popupMessage);
    }

    private static bool _runningShowAllPopups = false;

    public static bool AllowToShowPopup => (MainMenuGuiLayer.Current.PageRundownNew.m_isActive || MainMenuGuiLayer.Current.PageLoadout.m_isActive) && GameStateManager.CurrentStateName <= eGameStateName.Lobby;

    private static IEnumerator ShowAllPopups()
    {
        if (_runningShowAllPopups)
            yield break;

        _runningShowAllPopups = true;

        var yielder = new WaitForSeconds(0.1f);

        yield return yielder;

        while (true)
        {
            try
            {
                if (AllowToShowPopup && _popupQueue.TryDequeue(out var message))
                {
                    GlobalPopupMessageManager.ShowPopup(message);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show popup.");
                Logger.Exception(ex);
            }
            yield return yielder;
        }
    }
}
