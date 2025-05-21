using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Confirm : MonoBehaviour {
    public GameObject Event;
    public GameObject Error;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    
    public void Error_Confirm()
    {
        Error.SetActive(false);
        Event.SetActive(true);
    }
}
