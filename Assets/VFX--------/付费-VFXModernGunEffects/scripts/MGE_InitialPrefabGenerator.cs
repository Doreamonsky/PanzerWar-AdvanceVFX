using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script creates random gameObjects or prefabs. It can add some random rotation and position. It is not a 
/// "continuous" generation, it generates only once, at Start
/// </summary>

public class MGE_InitialPrefabGenerator : MonoBehaviour {
	
	public GameObject[] createThese;

	private GameObject justCreated;  //this will store the freshly created objects for easier readability

	public int createThisMany;

	public bool parentUnderCreator = false;


	public float x_rnd=0;
	public float y_rnd=0;
	public float z_rnd=0;

	public float x_rot_rnd=0;
	public float y_rot_rnd=0;
	public float z_rot_rnd=0;


	// Use this for initialization
	void Start () {

		for (int i = 0; i < createThisMany; i++) {

			justCreated = Instantiate (createThese[Random.Range(0, createThese.Length)], transform.position, transform.rotation);

			justCreated.transform.Translate(new Vector3 (Random.Range (-x_rnd, x_rnd), Random.Range (-y_rnd, y_rnd), Random.Range (-z_rnd, z_rnd)));

			justCreated.transform.Rotate(new Vector3 (Random.Range (-x_rot_rnd, x_rot_rnd), Random.Range (-y_rot_rnd, y_rot_rnd), Random.Range (-z_rot_rnd, z_rot_rnd)));

			if (parentUnderCreator == true)
				justCreated.transform.parent = transform;

		}

		
	}

	void OnDrawGizmos(){

		Gizmos.matrix = transform.localToWorldMatrix;

		float gizmoXSize;
		gizmoXSize = 0.1f + (x_rnd*2);

		float gizmoYSize;
		gizmoYSize = 0.1f + (y_rnd*2);

		float gizmoZSize;
		gizmoZSize = 0.1f + (z_rnd*2);


		Gizmos.DrawWireCube (Vector3.zero, new Vector3 (gizmoXSize,gizmoYSize, gizmoZSize));
		Gizmos.DrawWireCube (new Vector3(0f, 0.25f+y_rnd, 0f), new Vector3 (0.1f, 0.5f, 0.1f));

	}

}
