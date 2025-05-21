using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class TcpSocket : MonoBehaviour
{
    public Socket socket;
    public bool CanReceive;
    public byte[] tcpbuffer;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Send(byte[] buffer, AsyncCallback callback)
    {
        socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, callback, this);
    }

    public void Send(byte[] buffer, AsyncCallback callback, object obj)
    {
        socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, callback, obj);
    }

    public void Receive(byte[] buffer, AsyncCallback callback)
    {
        this.tcpbuffer = buffer;
        socket.BeginReceive(tcpbuffer, 0, tcpbuffer.Length, SocketFlags.None, callback, buffer); 
    }

    public void CloseSocket()
    {
        if (socket != null)
        {
            socket.Close();
        }
    }

}
