using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class Players
{
    public GameObject Panel;
    public byte PlayerNumber;
    public int getCount;
    public Texture Color;
    public float localx;

    public Players(byte PlayerNum, GameObject PlayerPanel, int getCount, Texture CardColor, float axis)
    {
        this.PlayerNumber = PlayerNum;
        this.Panel = PlayerPanel;
        this.getCount = getCount;
        this.Color = CardColor;
        this.localx = axis;
    }
}

public class Gamemanage : MonoBehaviour {
    public enum DataType
    {
        Card,
        Board,
        Choice,
        Damage,
        GameEnd,
        Turn
    }

    public TcpSocket tcp;
    public Texture[] CardColor_Images;
    public Texture[] NumberCard_Images;
    public Text[] PlayerHPs;
    public GameObject P1Panel;
    public GameObject P2Panel;
    public GameObject P3Panel;
    public GameObject P4Panel;
    public GameObject Board;
    public GameObject[] QueenButton;
    public GameObject GameOver;

    public Text currentdamage;
    public Text addeddamage;

    private byte[] mycards;
    private Players[] players = new Players[3];

    private Vector3 prevpos;
    private byte[] recvbuffer = new byte[1024];
    private byte[] databuffer = null;
    private byte playercode;
    private bool received = false;
    private bool myturn = false;
    private bool canplay = true;
    private DataType datatype;

    // Use this for initialization
    void Start() {
        Screen.SetResolution(Screen.width, Screen.width * 9 / 16, true);
        StartGame();
    }

    // Update is called once per frame
    void Update() {
        if (received)
        {
            received = false;

            byte[] checkrecv = { 1 };
            switch (datatype)
            {
                case DataType.Card:
                    StartCoroutine(MakeMyCards());
                    break;
                case DataType.Choice:
                    myturn = false;

                    foreach (GameObject bts in QueenButton)
                        bts.SetActive(true);
                    break;
                case DataType.Board:
                    StartCoroutine(CardPaintOnBoard());
                    break;
                case DataType.Turn:
                    if (databuffer[0] == 0)
                    {
                        if(canplay)
                            myturn = true;
                        else
                            checkrecv = new byte[] { 1, 1 };
                    }
                    else if (databuffer[0] == 1)
                    {
                        myturn = false;
                        checkrecv = new byte[] { 1, 1 };
                    }
                    break;
                case DataType.Damage:
                    StartCoroutine(RefreshHealthInfo());
                    break;
                case DataType.GameEnd:
                    GameOver.gameObject.SetActive(true);

                    if (canplay)
                        GameOver.transform.GetChild(0).gameObject.SetActive(false);
                    else
                        GameOver.transform.GetChild(1).gameObject.SetActive(false);

                    tcp.socket.Send(checkrecv);
                    return;
                    //break;
            }
            tcp.socket.Send(checkrecv);

            recvbuffer = new byte[1024];
            tcp.Receive(recvbuffer, new AsyncCallback(ReceiveCallback));
        }

        if(myturn)
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                Vector2 pos = touch.position;
                Vector3 touchvector = new Vector3(pos.x, pos.y, 0.0f);

                Ray ray = Camera.main.ScreenPointToRay(touchvector);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 30.0f))
                {
                    if (hit.collider.CompareTag("MyCard"))
                    {
                        GameObject cardobj = hit.collider.gameObject;

                        switch (touch.phase)
                        {
                            case TouchPhase.Began:
                                prevpos = cardobj.transform.position;
                                cardobj.transform.Translate(prevpos.x, prevpos.y, 1.0f);
                                break;
                            case TouchPhase.Moved:
                                Vector2 touchdelta = touch.deltaPosition;
                                cardobj.transform.Translate(touchdelta.x * 1.0f, touchdelta.y * 1.0f, 1.0f);
                                break;
                            case TouchPhase.Ended:
                                RaycastHit endedhit;
                                if (Physics.Raycast(cardobj.transform.position, cardobj.transform.forward, out endedhit, 30.0f))
                                {
                                    if (endedhit.collider.CompareTag("Board"))
                                    {
                                        MyCards card = cardobj.GetComponent(typeof(MyCards)) as MyCards;
                                        tcp.Send(new byte[] { 0, card.cardcode }, new AsyncCallback(SendCallback));
                                    }
                                    else
                                    {
                                        cardobj.transform.Translate(prevpos);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    public void StartGame()
    {
        tcp.Receive(recvbuffer, new AsyncCallback(StartCallback));
    }

    public void StartCallback(IAsyncResult res)
    {
        int read = tcp.socket.EndReceive(res);

        byte[] newbuffer = new byte[read];
        Array.Copy(recvbuffer, newbuffer, read);

        mycards = newbuffer;
        MakeMyCards();
        tcp.socket.Send(new byte[] { 1 });

        SetGameInfo();
        
        recvbuffer = new byte[1024];
        tcp.Receive(recvbuffer, new AsyncCallback(ReceiveCallback));
    }

    public void SendCallback(IAsyncResult res)
    {
        tcp.socket.EndSend(res);
    }

    public void ReceiveCallback(IAsyncResult res)
    {
        int read = tcp.socket.EndReceive(res);

        byte protocol = recvbuffer[0];
        byte[] newbuffer = new byte[read - 1];
        Array.Copy(recvbuffer , 1, newbuffer, 0, newbuffer.Length);

        switch(protocol)
        {
            case 0:
                mycards = newbuffer;
                datatype = DataType.Card;
                break;
            case 1:
                databuffer = newbuffer;
                datatype = DataType.Board;
                break;
            case 2:
                databuffer = newbuffer;
                datatype = DataType.Choice;
                break;
            case 3:
                databuffer = newbuffer;
                datatype = DataType.Turn;
                break;
            case 4:
                databuffer = newbuffer;
                datatype = DataType.Damage;
                break;
            case 5:
                databuffer = newbuffer;
                datatype = DataType.GameEnd;
                break;
        }
        
        received = true;
        
    }

    public void SetGameInfo()
    {
        byte[] buffer = new byte[4];
        tcp.socket.Receive(buffer);
        playercode = buffer[0];

        GameObject[] Panels = { P2Panel, P3Panel, P4Panel };
        for(int i = 1; i<4; i++)
        {
            Texture colors = null;
            switch(buffer[i])
            {
                case 1:
                    colors = CardColor_Images[0];
                    break;
                case 2:
                    colors = CardColor_Images[1];
                    break;
                case 3:
                    colors = CardColor_Images[2];
                    break;
                case 4:
                    colors = CardColor_Images[3];
                    break;
            }
            float axis = (i != 4) ? -0.33f : 0.33f;
            players[i - 1] = new Players(buffer[i], Panels[i], 5, colors, axis);
        }

        tcp.socket.Send(new byte[] { 1 });
    }

    IEnumerator MakeOppentCards(Players Player)
    {
        int count = Player.getCount;
        float x = Player.localx;
            
        float gap = (x > 0) ? -0.07f : 0.07f;

        int childcnt = Player.Panel.transform.childCount;

        for (int i = 0; i < childcnt; i++)
            Destroy(Player.Panel.transform.GetChild(i).gameObject);

        float z = -0.0f;
        for (int i = 0; i<count; i++)
        {
            GameObject prefaps = Instantiate(Resources.Load("Card")) as GameObject;
            prefaps.transform.SetParent(Player.Panel.transform);
            prefaps.transform.localPosition = new Vector3(x, 0.0f, z);
            prefaps.GetComponent<Renderer>().material.mainTexture = Player.Color;
            x += gap;
            z -= 0.05f;
        }
        
        yield return null;
    }

    IEnumerator MakeMyCards()
    {
        int childcnt = P1Panel.transform.childCount;

        float axis = 0.33f;
        float gap = 0.07f;

        for (int i = 0; i < childcnt; i++)
            Destroy(P1Panel.transform.GetChild(i).gameObject);

        int count = mycards.Length;

        float z = -0.1f;
        for (int i = 0; i <count; i++)
        {
            GameObject prefaps = Instantiate(Resources.Load("MyCard")) as GameObject;
            MyCards card = prefaps.GetComponent(typeof(MyCards)) as MyCards;
            card.SetCard(mycards[i]);
            prefaps.GetComponent<Renderer>().material.mainTexture = NumberCard_Images[mycards[i]];

            prefaps.transform.SetParent(P1Panel.transform);
            prefaps.transform.localPosition = new Vector3(axis, 0.0f, z);
            axis += gap;
            z -= 0.05f;
        }

        yield return null;
    }

    IEnumerator CardPaintOnBoard()
    {
        byte cardcode = databuffer[0];
        byte cardcount = databuffer[1];
        byte plyrcode = databuffer[2];
        byte currentdmg = databuffer[3];
        byte addeddmg = databuffer[4];
        byte boardclear = databuffer[5];

        float xrand = UnityEngine.Random.Range(-2.5f, 2.5f);
        float yrand = UnityEngine.Random.Range(-2.5f, 2.5f);
        float zrand = UnityEngine.Random.Range(3.0f, 4.0f);

        if(boardclear == 1)
        {
            int childcnt = Board.transform.childCount;
            for (int i = 0; i < childcnt; i++)
                Destroy(Board.transform.GetChild(i).gameObject);
        }

        GameObject prefaps = Instantiate(Resources.Load("Card")) as GameObject;
        prefaps.transform.position = new Vector3(xrand, yrand, zrand);
        prefaps.transform.SetParent(Board.transform);
        prefaps.GetComponent<Renderer>().material.mainTexture = NumberCard_Images[cardcode];

        currentdamage.text = NumberBoard(currentdmg);
        addeddamage.text = NumberBoard(addeddmg);

        foreach (Players item in players)
        {
            if(item.PlayerNumber == plyrcode)
            {
                item.getCount = cardcount;
                MakeOppentCards(item);
            }
        }
        yield return null;
    }

    IEnumerator RefreshHealthInfo()
    {
        byte[] PlyrsNumber = { playercode, players[0].PlayerNumber, players[1].PlayerNumber, players[2].PlayerNumber };

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if ((i + 1) == PlyrsNumber[j])
                {
                    PlayerHPs[j].text = string.Format("체력 : {0}", databuffer[i]);
                }
            }
        }

        if (PlayerHPs[0].text == "체력 : 0")
            canplay = false;

        yield return null;
    }

    private string NumberBoard(byte number)
    {
        string str = "";

        int hundred = number / 100;
        int ten = (hundred % 100) / 10;
        int one = number % 10;

        str = string.Format("{0} {1} {2}", hundred, ten, one);

        return str;
    }

    public void P2QueenButton()
    {
        SendQueenCardInfo(players[0]);
    }

    public void P3QueenButton()
    {
        SendQueenCardInfo(players[1]);
    }

    public void P4QueenButton()
    {
        SendQueenCardInfo(players[2]);
    }

    public void GoLobby()
    {
        SceneManager.LoadScene(1);
    }

    private void SendQueenCardInfo(Players player)
    {
        byte[] newbyte = new byte[] {2, playercode, player.PlayerNumber};
        tcp.Send(newbyte, SendCallback);

        foreach (GameObject bts in QueenButton)
            bts.SetActive(false);
    }
}
