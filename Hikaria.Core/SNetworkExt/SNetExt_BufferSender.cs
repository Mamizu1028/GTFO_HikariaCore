using UnityEngine;

namespace Hikaria.Core.SNetworkExt;

public class SNetExt_BufferSender
{
    public SNetExt_BufferSender(int packetsPerFrame, float sendInterval, SNetExt_CaptureBuffer buffer, List<SNetwork.SNet_Player> players, SNetwork.SNet_ChannelType channelType)
    {
        m_packetsPerFrame = packetsPerFrame;
        m_sendInterval = sendInterval;
        m_buffer = buffer;
        m_sendToPlayers.AddRange(players);
        m_bufferType = buffer.type;
        m_channelType = channelType;
    }

    private void UpdateBufferBytes()
    {
        m_bufferBytes = SNetExt_ReplicatedPacketBufferBytes.GetBufferDataBytes(new SNetExt_ReplicatedPacketBufferBytes.BufferData(m_buffer.data.bufferID, (byte)m_passIndex));
    }

    private bool UpdatePlayerList()
    {
        for (int i = m_sendToPlayers.Count - 1; i > -1; i--)
        {
            var snet_Player = m_sendToPlayers[i];
            if (!snet_Player.IsInSessionHub || snet_Player.IsBot)
            {
                m_sendToPlayers.RemoveAt(i);
            }
        }
        return m_sendToPlayers.Count > 0;
    }

    public bool Update()
    {
        if (m_state == State.Send)
        {
            m_sendTimer += Clock.Delta;
            if (m_sendTimer < m_sendInterval)
            {
                return false;
            }
            m_sendTimer = 0f;
        }
        else
        {
            if (m_state == State.Done)
            {
                return true;
            }
            m_sendTimer = m_sendInterval + 1f;
        }
        if (!UpdatePlayerList())
        {
            m_state = State.Done;
            return true;
        }
        switch (m_state)
        {
            case State.Open:
                SNetExt.Capture.m_bufferCommandPacket.Send(new pBufferCommand
                {
                    type = m_bufferType,
                    operation = SNetExt_BufferOperationType.StartReceive,
                    bufferID = m_buffer.data.bufferID
                }, m_channelType, m_sendToPlayers);
                UpdateBufferBytes();
                m_state = State.Send;
                break;
            case State.Send:
                {
                    List<byte[]> list = m_buffer.m_passes[m_passIndex];
                    int num = m_packetIndex;
                    if (num >= list.Count)
                    {
                        list = null;
                        m_passIndex++;
                        while (m_passIndex < m_buffer.m_passes.Length)
                        {
                            List<byte[]> list2 = m_buffer.m_passes[m_passIndex];
                            if (list2.Count > 0)
                            {
                                list = list2;
                                m_packetIndex = 0;
                                num = Mathf.Min(m_packetIndex + m_packetsPerFrame, list2.Count);
                                UpdateBufferBytes();
                                break;
                            }
                            m_passIndex++;
                        }
                        if (list == null)
                        {
                            m_state = State.Close;
                            break;
                        }
                    }
                    else
                    {
                        num = Mathf.Min(m_packetIndex + m_packetsPerFrame, list.Count);
                    }
                    for (int i = m_packetIndex; i < num; i++)
                    {
                        SNetExt.Capture.m_bufferBytesPacket.Send(list[i], m_bufferBytes, m_sendToPlayers);
                        m_packetIndex++;
                        m_totalPacketsSent++;
                    }
                    break;
                }
            case State.Close:
                SNetExt.Capture.m_bufferCompletionPacket.Send(new pBufferCompletion
                {
                    type = m_bufferType,
                    data = m_buffer.data
                }, m_channelType, m_sendToPlayers);
                m_state = State.Done;
                break;
            case State.Done:
                return true;
        }
        return false;
    }

    private State m_state;

    private readonly float m_sendInterval;

    private readonly int m_packetsPerFrame;

    private int m_passIndex;

    private int m_packetIndex;

    private SNetExt_CaptureBuffer m_buffer;

    private readonly List<SNetwork.SNet_Player> m_sendToPlayers = new();

    private readonly SNetExt_BufferType m_bufferType;

    private readonly SNetwork.SNet_ChannelType m_channelType;

    private float m_sendTimer;

    private int m_totalPacketsToSend;

    private int m_totalPacketsSent;

    private byte[] m_bufferBytes;

    public enum State
    {
        Open,
        Send,
        Close,
        Done
    }
}
