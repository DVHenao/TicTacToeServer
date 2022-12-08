using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    List<int> recIDList = new List<int>();

    List<string> gameroomIDs = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);
        
    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                recIDList.Add(recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                recIDList.Remove(recConnectionID);
                break;
        }

    }
  
    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    public void SendMessageToClients(string msg)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);

        for(int i = 0; i <= recIDList.Count; i++)
        {
            NetworkTransport.Send(hostID, i, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
        }
        
    }

    private void ProcessRecievedMsg(string msg, int id)
    {


        string[] fortnite = msg.Split(',');
       switch(fortnite[0])
        {
            case "gameroom":
            
                if (gameroomIDs.Contains(fortnite[1])) // Game room already exists, join
                {
                    if(id == 2)
                    SendMessageToClients("gameroomjoined,filled");
                    if (id == 3)
                    SendMessageToClient("gameroomjoined,spectate", id);
                }
                else  // Game room doesnt exist, create
                {
                    gameroomIDs.Add(fortnite[1]);
                    SendMessageToClient("gameroomjoined,empty", id);
                    Debug.Log(id);
                }
                    


                Debug.Log(fortnite[1]);
                break;
            case "disconnect": // something disconnect, send message to remaining player to HALT
                if (id == 1)
                    SendMessageToClient("disconnect", 2);
                if (id == 2)
                    SendMessageToClient("disconnect", 1);
                break;
        }
    }

}
