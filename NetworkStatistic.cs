using BeardedMonkeys;
using FishNet;
using UnityEngine;

public class NetworkStatistic : MonoBehaviour
{
    [Tooltip("The transport which contains the calculation.")]
    [SerializeField]
    private FishyLatency m_transport;

    private GUIStyle _style = new GUIStyle();

    private void OnGUI()
    {
        _style.normal.textColor = Color.magenta;
        _style.fontSize = 30;
        _style.fontStyle = FontStyle.Bold;

        float width = 85f;
        float height = 15f;

        float horizontal = 10f;
        float vertical = 300f;

        if(InstanceFinder.IsServer)
        {
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Received Packets: {m_transport.ReceivedPacketsServer}/s", _style);
           
            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Received Bytes: {m_transport.ReceivedBytesServer}/s", _style);
            
            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Sent Packets: {m_transport.SentPacketsServer}/s", _style);
            
            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Sent Bytes: {m_transport.SentBytesServer}/s", _style);
        }
        else
        {
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Received Packets: {m_transport.ReceivedPacketsClient}/s", _style);

            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Received Bytes: {m_transport.ReceivedBytesClient}/s", _style);

            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Sent Packets: {m_transport.SentPacketsClient}/s", _style);

            vertical += 25f;
            GUI.Label(new Rect(horizontal, vertical, width, height), $"Sent Bytes: {m_transport.SentBytesClient}/s", _style);
        }
    }
}
