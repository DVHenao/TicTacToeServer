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

    int gameOverCounter = 0;

    string ID1Side, ID2Side;

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
                if (recConnectionID > 2)
                    SendMessageToClient("gameroomjoined,spectate", recConnectionID);


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

        for (int i = 0; i <= recIDList.Count; i++)
        {
            NetworkTransport.Send(hostID, i, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
        }

    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("full message: " + msg + " host ID: " + id);

        string[] fortnite = msg.Split(',');
        switch (fortnite[0])
        {
            case "gameroom":
                Debug.Log(fortnite[1]);

                if (gameroomIDs.Contains(fortnite[1])) // Game room already exists, join
                {
                    if (id == 2)
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
                break;


            case "buttonpressed":
                Debug.Log(fortnite[1]);

                if (fortnite[1] == "X")
                {
                    if (id == 1)
                    {
                        SendMessageToClient("buttonpressed,otherplayerX", 2);

                        break;
                    }
                    else
                    {
                        SendMessageToClient("buttonpressed,otherplayerX", 1);

                        break;
                    }
                }
                else if (fortnite[1] == "O")
                {
                    if (id == 1)
                    {
                        SendMessageToClient("buttonpressed,otherplayerO", 2);
                        break;
                    }
                    else
                    {
                        SendMessageToClient("buttonpressed,otherplayerO", 1);
                        break;
                    }

                }
                else // selecting tictactoe buttons
                {
                    string messageOut;

                    if (id == 1)
                    {
                        messageOut = "buttonpressed," + fortnite[1];
                        SendMessageToClient(messageOut, 2);
                    }
                    else if (id == 2)
                    {
                        messageOut = "buttonpressed," + fortnite[1];
                        SendMessageToClient(messageOut, 1);
                    }
                }

                break;

            case "messagesent":
                {
                    string messageOut = "messagesent," + fortnite[1];
                    SendMessageToClients(messageOut);
                }
                break;


            case "disconnect": // something disconnect, send message to remaining player to HALT
                if (id == 1)
                    SendMessageToClient("disconnect", 2);
                if (id == 2)
                    SendMessageToClient("disconnect", 1);
                break;

            case "login":
                checkLoginCredentials(fortnite, id);
                break;

            case "register":
                CreateAccount(fortnite, id);
                break;
        }

    }
    public void checkLoginCredentials(string[] fortnite, int id)
    {
        using (StreamReader sr = new StreamReader("c:/Temp/LoginVerification.txt"))
        {
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                string[] loginVerification = line.Split(',');

                if (loginVerification[0] == fortnite[1])
                {
                    Debug.Log("username correct, testing pass");

                    if (loginVerification[1] == fortnite[2])
                    {
                        SendMessageToClient("loginregister,loginsuccess", id);
                        return;
                    }
                    else // right user, wrong pass
                    {
                        SendMessageToClient("loginregister,wrongpassword", id);
                        return;
                    }
                }
            }

            SendMessageToClient("loginregister,noaccount", id);

        }
    }

    public void CreateAccount(string[] fortnite, int id)
    {
        using (StreamReader sr = new StreamReader("c:/Temp/LoginVerification.txt"))
        {
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                string[] loginVerification = line.Split(',');

                if (loginVerification[0] == fortnite[1])
                {
                    Debug.Log("username already exists");
                    SendMessageToClient("loginregister,usernametaken", id);
                    return;
                }
            }
        }

        DirectoryInfo[] cDirs = new DirectoryInfo(@"c:\Temp").GetDirectories();

        string account = fortnite[1] + "," + fortnite[2];

        using (StreamWriter sw = new StreamWriter("c:/Temp/LoginVerification.txt", true))
        {
            sw.WriteLine(account);
            SendMessageToClient("loginregister,accountcreated", id);
        }
    }
}

