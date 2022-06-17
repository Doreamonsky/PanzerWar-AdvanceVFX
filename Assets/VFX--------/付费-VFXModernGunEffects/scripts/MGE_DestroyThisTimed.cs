using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGE_DestroyThisTimed : MonoBehaviour {

	public float destroyTime=5;
	private float time=0;

	
	// Update is called once per frame
	void Update () {
	
		time += Time.deltaTime;
		if (time > destroyTime)
			Destroy (gameObject);

	}
}
