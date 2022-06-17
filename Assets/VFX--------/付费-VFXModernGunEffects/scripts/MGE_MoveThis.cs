using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGE_MoveThis : MonoBehaviour {

	public float x_speed;
	public float x_rnd;   	//rnd means deviation. An xspeed of 3 and an rnd of 1 will make xspeed 2-4.
							// random is set when the script stars, and does not change after that
	public float y_speed;
	public float y_rnd;

	public float z_speed;
	public float z_rnd;

	private float x_actual;  //these will store the actual values, after randomization
	private float y_actual;
	private float z_actual;

	public bool localMovement=true;  // local movement or world coordinates?


	// Use this for initialization
	void Start () {

		x_actual = Random.Range (x_speed-x_rnd, x_speed+x_rnd);
		y_actual = Random.Range (y_speed-y_rnd, y_speed+y_rnd);
		z_actual = Random.Range (z_speed-z_rnd, z_speed+z_rnd);
		
	}
	
	// Update is called once per frame
	void Update () {
		if (localMovement==true)
			transform.Translate (x_actual*Time.deltaTime, y_actual*Time.deltaTime, z_actual*Time.deltaTime);
		else
			transform.Translate (x_actual*Time.deltaTime, y_actual*Time.deltaTime, z_actual*Time.deltaTime, Space.World);
	}
}
