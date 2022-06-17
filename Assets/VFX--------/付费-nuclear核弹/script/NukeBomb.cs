using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NukeBomb : MonoBehaviour {

public GameObject Prefab;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetButtonDown ("Fire1"))
		{
			Instantiate(Prefab,transform.position,transform.rotation);
		}
		
	}
}
