using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCards : MonoBehaviour {
    public byte cardcode;
    
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    
    public void SetCard(byte bytecode)
    {
        this.cardcode = bytecode;
    }

}
