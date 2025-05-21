using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Submit : MonoBehaviour {
    public InputField TextField;
    public GameObject Event;
    public GameObject Error;
    public Text State;
    public TcpSocket connectedsocket;
    
    private Socket clientsocket;
    private IPEndPoint serverEP = null;

	// Use this for initialization
	void Start () {
        Screen.SetResolution(Screen.width,Screen.width*9/16,true);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    
    public void OnClicked()
    {
        try
        {
            clientsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectedsocket.socket = this.clientsocket;

            IPAddress address = IPAddress.Parse(TextField.text);
            serverEP = new IPEndPoint(address,15000);
            //clientsocket.Connect(serverEP);

            IAsyncResult syncrs = clientsocket.BeginConnect(serverEP, null, null);

            if (syncrs.AsyncWaitHandle.WaitOne(10000))
                clientsocket.EndConnect(syncrs);
            else
                throw new Exception("Connection timed out");

            if (clientsocket.Connected)
            {
                connectedsocket.CanReceive = true;
                SceneManager.LoadScene(1);
            }
        }
        catch (System.Exception e)
        {
            if (clientsocket != null)
                clientsocket.Close();
            State.text = e.Message;
            Event.SetActive(false);
            Error.SetActive(true);
        }

    }

}
