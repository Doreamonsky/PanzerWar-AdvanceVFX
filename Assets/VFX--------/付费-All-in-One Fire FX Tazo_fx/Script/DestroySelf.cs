using UnityEngine;
using System.Collections;


public class DestroySelf : MonoBehaviour {



	
	public float timeout = 0.5f;

	
	// Update is called once per frame
	void  Start () {
		Destroy (gameObject,timeout);
		}



}
