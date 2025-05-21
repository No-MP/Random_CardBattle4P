using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatRoom : MonoBehaviour {
    enum ReceivedDataType
    {
        Ready,
        Connection,
        Chat,
        Exit,
        ReadytoStart,
        GameStart
    }

    private ReceivedDataType Receivedtype;
    public TcpSocket tcp;
    public Gamemanage gm;

    public GameObject Viewport;
    public GameObject ListCanvas;
    public ButtonEvent RoomPanel;

    public Text ChatPanel;
    public InputField textfield;

    public bool loopFlags = false;
    public bool Received = false;
    private bool CanReceive = true;

    private byte[] buffer;

    // Use this for initialization
    void Start() {
        Screen.SetResolution(Screen.width, Screen.width * 9 / 16, true);
        StartCoroutine(Recv());
    }

    // Update is called once per frame
    void Update() {

    }

    public void SendChat()
    {
        if (textfield.text != "")
        {
            byte[] encode = Encoding.UTF8.GetBytes(textfield.text);

            byte[] sendbuffer = ConcatBytes(2, encode);
            tcp.socket.Send(sendbuffer);

            textfield.text = "";
        }
    }

    public void Ready()
    {
        byte[] sendstate = null;

        GameObject startbutton = transform.GetChild(3).gameObject;
        string bt = startbutton.transform.GetChild(0).GetComponent<Text>().text;

        switch (bt)
        {
            case "준비완료":
                sendstate = new byte[] { 0, 1 };
                break;
            case "준비해제":
                sendstate = new byte[] { 0, 0 };
                break;
            case "게임시작":
                sendstate = new byte[] { 3, 0 };
                break;
        }

        tcp.socket.Send(sendstate);
    }

    public void Exit()
    {
        byte[] buffer = { 1, 1 };
        tcp.socket.Send(buffer);
    }

    public void StartLoop()
    {
        loopFlags = true;

        byte[] response = { 1 };
        tcp.socket.Send(response);
        Debug.Log("check");
    }

    IEnumerator Recv()
    {
        while (true)
        {
            if (loopFlags)
            {
                
                if (CanReceive)
                {
                    CanReceive = false;

                    buffer = new byte[1024];
                    tcp.Receive(buffer, new AsyncCallback(RecvCallback));
                }

                if (Received)
                    yield return StartCoroutine(Repaint());
            }
            yield return null;
        }
    }

    IEnumerator Repaint()
    {
        Received = false;

        GameObject button1 = transform.GetChild(3).gameObject;
        GameObject button2 = transform.GetChild(4).gameObject;

        switch (Receivedtype)
        {
            case ReceivedDataType.Ready:
                StateReady();
                break;
            case ReceivedDataType.Connection:
                StateConnection();
                break;
            case ReceivedDataType.Chat:
                string chat = Encoding.UTF8.GetString(buffer) + "\n";
                ChatPanel.text += chat;
                break;
            case ReceivedDataType.Exit:
                StateExit();
                break;
            case ReceivedDataType.ReadytoStart:
                button1.GetComponent<Button>().interactable = false;
                button2.GetComponent<Button>().interactable = false;
                break;
            case ReceivedDataType.GameStart:
                loopFlags = false;

                transform.gameObject.SetActive(false);

                button1.transform.GetChild(0).GetComponent<Text>().text = "준비완료";
                button1.GetComponent<Button>().interactable = true;
                button2.GetComponent<Button>().interactable = true;

                ListCanvas.SetActive(true);
                SceneManager.LoadScene(2);
                //gm.StartGame();
                break;
        }

        byte[] response = {1};
        tcp.socket.Send(response);
        CanReceive = true;
        yield return null;
    }

    private void RecvCallback(IAsyncResult async)
    {
        int byteread = tcp.socket.EndReceive(async);
        
        if (byteread > 0)
        {
            byte read = buffer[0];
            byte[] newarray = new byte[byteread - 1];
            Array.Copy(buffer, 1, newarray, 0, newarray.Length);
            switch (read)
            {
                case 0:
                    Receivedtype = ReceivedDataType.Ready;
                    break;
                case 1:
                    Receivedtype = ReceivedDataType.Connection;
                    break;
                case 2:
                    Receivedtype = ReceivedDataType.Chat;
                    break;
                case 3:
                    Receivedtype = ReceivedDataType.Exit;
                    break;
                case 4:
                    Receivedtype = ReceivedDataType.ReadytoStart;
                    break;
                case 5:
                    Receivedtype = ReceivedDataType.GameStart;
                    break;
            }
            
            buffer = newarray;
            Received = true;
        }

    }

    private void StateReady()
    {
        GameObject startbutton = transform.GetChild(3).gameObject;

        int size = buffer.Length;

        int count = 0;
        for (int i = 0; i < size; i++)
        {
            string state = "";
            if (buffer[i] == 1)
            {
                state = "Ready";
                if (i == 0)
                    state = "Host";
                count++;
            }

            Viewport.transform.GetChild(i).GetChild(1).GetComponent<Text>().text = state;
        }

        string bt = startbutton.transform.GetChild(0).GetComponent<Text>().text;

        if (bt == "게임시작")
        {
            if (count != 4)
                startbutton.GetComponent<Button>().interactable = false;
            else
                startbutton.GetComponent<Button>().interactable = true;
        }

    }

    private void StateConnection()
    {
        string str = Encoding.UTF8.GetString(buffer);

        string[] split = str.Split('\n');

        int childcount = Viewport.transform.childCount;
        for (int i = 0; i < childcount; i++)
        {
            Destroy(Viewport.transform.GetChild(i).gameObject);
        }

        int splength = split.Length;
        for (int i = 0; i < splength; i++)
        {
            GameObject prefap = Instantiate(Resources.Load("User"), Viewport.transform) as GameObject;

            prefap.transform.GetChild(0).GetComponent<Text>().text = split[i];
            //prefap.transform.SetParent(Viewport.transform);
        }

    }

    private void StateExit()
    {
        loopFlags = false;

        GameObject startbutton = transform.GetChild(3).gameObject;

        transform.gameObject.SetActive(false);
        startbutton.transform.GetChild(0).GetComponent<Text>().text = "준비완료";
        startbutton.GetComponent<Button>().interactable = true;

        ListCanvas.SetActive(true);
        RoomPanel.RestartLobby();
    }

    private byte[] ConcatBytes(byte protocol, byte[] array)
    {
        byte[] newarray = new byte[array.Length + 1];

        for (int i = 1; i < newarray.Length; i++)
            newarray[i] = array[i - 1];

        newarray[0] = protocol;

        return newarray;
    }
}
