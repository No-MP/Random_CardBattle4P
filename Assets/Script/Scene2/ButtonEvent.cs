using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ButtonEvent : MonoBehaviour {
    public TcpSocket TCP;
    public GameObject Panel;
    public GameObject Error;
    public InputField textfield;
    public GameObject Viewport;
    public GameObject RoomCanvas;
    public GameObject ListCanvas;
    public ChatRoom chatroom;

    public bool loopFlags = true;
    private bool Received = false;
    private bool CanReceive = true;
    private byte[] recvbuffer;
    private string[] split;

    // Use this for initialization
    void Start() {
        Screen.SetResolution(Screen.width, Screen.width * 9 / 16, true);
        StartCoroutine(recv());
    }

    // Update is called once per frame
    void Update () {

    }
    
    void OnApplicationQuit()
    {
        TCP.CloseSocket();
    }

    IEnumerator recv()
    {
        while (loopFlags)
        {
            if (CanReceive)
            {
                CanReceive = false;
                recvbuffer = new byte[1024];
                TCP.Receive(recvbuffer, new AsyncCallback(recvCallback));
            }

            if (Received)
                yield return StartCoroutine(ClearandRefresh());
            yield return null;
        }
    }

    public void RestartLobby()
    {
        loopFlags = true;
        CanReceive = true;
    }

    private void recvCallback(IAsyncResult async)
    {
        int byteread = TCP.socket.EndReceive(async);

        if (byteread > 0)
        {
            byte[] newarray = new byte[byteread];
            Array.Copy(recvbuffer, newarray, byteread);

            if (byteread == 1)
            {
                recvbuffer = newarray;
            }
            else
            {
                string str = Encoding.UTF8.GetString(newarray);

                string[] sp = str.Split('\n');
                this.split = sp;
            }

            Received = true;
        }
        else
            CanReceive = true;
    }

    IEnumerator ClearandRefresh()
    {
        Received = false;

        if (recvbuffer.Length == 1)
        {
            
            if (recvbuffer[0] == 0)
            {
                loopFlags = false;

                CancelCreate();
                ListCanvas.SetActive(false);

                RoomCanvas.SetActive(true);
                RoomCanvas.transform.GetChild(3).GetChild(0).GetComponent<Text>().text = "게임시작";

                chatroom.StartLoop();
            } else if(recvbuffer[0] == 1)
            {
                loopFlags = false;

                ListCanvas.SetActive(false);
                RoomCanvas.SetActive(true);

                chatroom.StartLoop();
            }
            else
                CanReceive = true;
        } else
        {
            int size = split.Length;

            for (int i = 0; i < Viewport.transform.childCount; i++)
                Destroy(Viewport.transform.GetChild(i).gameObject);

            for (int i = 0; i < size; i++)
            {
                string name = split[i].Split('&')[0];
                string counts = split[i].Split('&')[1];

                GameObject prefabs = Resources.Load("RoomItem") as GameObject;
                GameObject roomitem = Instantiate(prefabs, Viewport.transform);

                roomitem.transform.name = i + "";
                roomitem.transform.GetChild(1).GetComponent<Text>().text = name; //Name
                roomitem.transform.GetChild(2).GetComponent<Text>().text = counts + "/4"; //ClientCounts
            }
            CanReceive = true;
        }
        yield return null;
    }

    public void CreateClicked()
    {
        Panel.SetActive(true);
    }

    public void ExitClicked()
    {
        Application.Quit();
    }

    public void CreateRoom()
    {
        if (textfield.text != "")
        {
            string str = textfield.text + "$Create";
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            TCP.socket.Send(buffer);
        }
        else
        {
            Error.GetComponent<Text>().text = "방 제목을 입력해주세요.";
        }
        
    }

    private void sendCallback(IAsyncResult res)
    {
        TCP.socket.EndSend(res);
    }

    public void CancelCreate()
    {
        Panel.SetActive(false);
        textfield.text = "";
        Error.GetComponent<Text>().text = "";
    }
}