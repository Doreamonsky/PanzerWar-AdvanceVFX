using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGE_PrefabOnCollision : MonoBehaviour {

	public GameObject createThis;
	private ParticleSystem[] p; 



	// Use this for initialization
	void OnCollisionEnter(Collision coll){
		Instantiate (createThis, transform.position, transform.rotation);

		Destroy(GetComponent<Rigidbody>());
		Destroy(GetComponent<Renderer>());

		p = GetComponentsInChildren<ParticleSystem> ();

		foreach (ParticleSystem PS in p)
			PS.Stop ();



	}


}
