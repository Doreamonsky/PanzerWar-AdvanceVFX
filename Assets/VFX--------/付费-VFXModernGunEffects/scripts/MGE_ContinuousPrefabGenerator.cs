using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script creates random gameObjects or prefabs. It can add some random rotation and position. 
/// It generates prefabs continuously. A known bug is that if the frame rate is very low (time between frames
/// is larger than time between prefab generation) then it will generate prefabs at each frame instead of more 
/// (so it will generate less prefabs than intended). 
/// I don't think it happens very often.
/// </summary>

public class MGE_ContinuousPrefabGenerator : MonoBehaviour {
	
	public GameObject[] createThese;  		// a list of objects to create
	public float createTimeIntervals;  		// how much time to wait between creations
	public float stopAfter=-1; 				// stop after X seconds, anything <=0 means it goes on forever



	private float time=0;   				//needed for generation
	private float totalTime = 0;  			//needed to turn off generation if neccessary
	private bool stop=false;
	private GameObject justCreated;  		//this will store the freshly created objects for easier readability

	public bool parentUnderCreator = false;


	public float x_rnd=0;
	public float y_rnd=0;
	public float z_rnd=0;

	public float x_rot_rnd=0;
	public float y_rot_rnd=0;
	public float z_rot_rnd=0;

	void Start(){

	}



	void Update(){

		time += Time.deltaTime;
		totalTime += Time.deltaTime;  

		if (totalTime > stopAfter && stopAfter > 0) {
			stop = true;
		}

		if (time > createTimeIntervals && stop==false) {
			CreatePrefab ();
			time -= createTimeIntervals;
		}



	}


	void CreatePrefab(){
		
		justCreated = Instantiate (createThese[Random.Range(0, createThese.Length)], transform.position, transform.rotation);

		justCreated.transform.Translate(new Vector3 (Random.Range (-x_rnd, x_rnd), Random.Range (-y_rnd, y_rnd), Random.Range (-z_rnd, z_rnd)));

		justCreated.transform.Rotate(new Vector3 (Random.Range (-x_rot_rnd, x_rot_rnd), Random.Range (-y_rot_rnd, y_rot_rnd), Random.Range (-z_rot_rnd, z_rot_rnd)));

		if (parentUnderCreator == true)
			justCreated.transform.parent = transform;
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
