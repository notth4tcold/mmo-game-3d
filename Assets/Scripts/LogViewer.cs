using UnityEngine;
using System.Collections;
using System;

public class LogViewer : MonoBehaviour {
    public static LogViewer instance;

    uint size = 15;
    Queue logQueue = new Queue();

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    public void Log(string log) {
        logQueue.Enqueue(log);

        while (logQueue.Count > size)
            logQueue.Dequeue();
    }

    void OnGUI() {
        var style = new GUIStyle();
        style.normal.textColor = Color.black;

        GUILayout.BeginArea(new Rect(20, 0, Screen.width - 20, Screen.height));
        GUILayout.Label("\n" + string.Join("\n", logQueue.ToArray()), style);
        GUILayout.EndArea();

        if (!Client.instance || !Client.instance.isReady) return;

        style.normal.textColor = Color.green;
        GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, 300));

        GUILayout.Label("Fps: " + (1.0f / Time.deltaTime).ToString("F2"), style);
        GUILayout.Label("Ping: " + Client.instance.tcp.getPing().ToString("F2") + " ms", style);
        GUILayout.Label("Jitter: " + Client.instance.tcp.getJitter().ToString("F2") + " ms", style);

        // Client.instance.tcp.updateThroughput();
        // GUILayout.Label("Tcp Bytes Enviados: " + Client.instance.tcp.totalBytesSent.ToString("F2") + " (" + Client.instance.tcp.bytesUploadRate.ToString("F2") + " KB/s)", style);
        // GUILayout.Label("Tcp Bytes Recebidos: " + Client.instance.tcp.totalBytesReceived.ToString("F2") + " (" + Client.instance.tcp.bytesDownloadRate.ToString("F2") + " KB/s)", style);
        // GUILayout.Label("Tcp Pacotes Enviados: " + Client.instance.tcp.totalPacketsSent + " (" + Client.instance.tcp.packetsUploadRate.ToString("F2") + " P/S)", style);
        // GUILayout.Label("Tcp Pacotes Recebidos: " + Client.instance.tcp.totalPacketsReceived + " (" + Client.instance.tcp.packetsDownloadRate.ToString("F2") + " P/S)", style);

        Client.instance.udp.updateThroughput();
        GUILayout.Label("Udp Bytes Enviados: " + Client.instance.udp.totalBytesSent.ToString("F2") + " (" + Client.instance.udp.bytesUploadRate.ToString("F2") + " KB/s)", style);
        GUILayout.Label("Udp Bytes Recebidos: " + Client.instance.udp.totalBytesReceived.ToString("F2") + " (" + Client.instance.udp.bytesDownloadRate.ToString("F2") + " KB/s)", style);
        GUILayout.Label("Udp Pacotes Enviados: " + Client.instance.udp.totalPacketsSent + " (" + Client.instance.udp.packetsUploadRate.ToString("F2") + " P/S)", style);
        GUILayout.Label("Udp Pacotes Recebidos: " + Client.instance.udp.totalPacketsReceived + " (" + Client.instance.udp.packetsUploadRate.ToString("F2") + " P/S)", style);
        GUILayout.Label("Udp Packet Loss: " + Client.instance.udp.GetPacketLossRate().ToString("F2") + "%", style);

        Client.instance.updateTickRateCheck();
        GUILayout.Label("Tick Rate: " + Client.instance.tickRateCheck.ToString("F2") + "/s", style);

        GUILayout.Label("Latency added: " + (Client.instance.latency * 1000) + " ms", style);
        GUILayout.Label("Packet lost added: " + (Client.instance.packetLossChance * 100) + " %", style);

        GUILayout.Label("TOTAL input cached on server: " + Server.instance.players[Client.instance.id].getInputsSize());

        GUILayout.EndArea();
    }
}
