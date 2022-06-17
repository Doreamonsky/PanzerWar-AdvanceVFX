using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGE_InitialForce : MonoBehaviour {

	/// <summary>
	/// Adds an initial force and torque to a rigidbody object. Needs rigidbody to work.
	/// </summary>

	public bool local = true;

	public float x_str = 1f;
	public float x_rnd = 0.5f;
	private float x_act;

	public float y_str;
	public float y_rnd;
	private float y_act;

	public float z_str;
	public float z_rnd;
	private float z_act;

	public float x_torq;
	public float x_torq_rnd;
	private float x_tq_act;

	public float y_torq;
	public float y_torq_rnd;
	private float y_tq_act;

	public float z_torq;
	public float z_torq_rnd;
	private float z_tq_act;

	private Rigidbody myRB;


	// Use this for initialization
	void Start () {

		myRB = GetComponent<Rigidbody> ();

		x_act = x_str + Random.Range (-x_rnd, x_rnd);
		y_act = y_str + Random.Range (-y_rnd, y_rnd);
		z_act = z_str + Random.Range (-z_rnd, z_rnd);

		x_tq_act = x_torq + Random.Range (-x_torq_rnd, x_torq_rnd);
		y_tq_act = y_torq + Random.Range (-y_torq_rnd, y_torq_rnd);
		z_tq_act = z_torq + Random.Range (-z_torq_rnd, z_torq_rnd);

		if (local == false) {
			myRB.AddForce (new Vector3 (x_act, y_act, z_act), ForceMode.Impulse); 
			myRB.AddTorque (new Vector3 (x_tq_act, y_tq_act, z_tq_act), ForceMode.Impulse);
		} else {
			myRB.AddRelativeForce (new Vector3 (x_act, y_act, z_act), ForceMode.Impulse);	
			myRB.AddRelativeTorque (new Vector3 (x_tq_act, y_tq_act, z_tq_act), ForceMode.Impulse);
		}




	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
