using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class JoinRoom : MonoBehaviour {
    public TcpSocket tcp;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Join()
    {
        string str = transform.name + "$Join";

        byte[] buffer = Encoding.UTF8.GetBytes(str);
        tcp.socket.Send(buffer);

        //byte[] recv = new byte[1];
        //tcp.socket.Receive(recv);

        //if(recv[0] == 1)
        //{
        //    script1.loopFlags = false;

        //    ListCanvas.SetActive(false);
        //    RoomCanvas.SetActive(true);

        //    script2.StartLoop();
        //}

    }
}
