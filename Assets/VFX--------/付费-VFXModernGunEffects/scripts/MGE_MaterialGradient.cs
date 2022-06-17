using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGE_MaterialGradient : MonoBehaviour {

	public Gradient myGradient;
	public int targetMaterialSlot=0;

	public float speed = 1;

	public bool tintColor=true;
	public bool mainColor=false;
	public bool loop = false;
	public bool resetTimeOnActivation = false;

	private float time=0;
	private Color curColor;






	// Use this for initialization
	void Start () {
		curColor=myGradient.Evaluate(time);
		if (tintColor==true) GetComponent<Renderer>().materials[targetMaterialSlot].SetColor ("_TintColor", curColor);
		if (mainColor==true) GetComponent<Renderer>().materials[targetMaterialSlot].SetColor ("_Color", curColor);

		
	}
	
	// Update is called once per frame
	void Update () {
		time+=Time.deltaTime*speed;

		curColor=myGradient.Evaluate(time);

		if (tintColor==true) GetComponent<Renderer>().materials[targetMaterialSlot].SetColor ("_TintColor", curColor);
		if (mainColor==true) GetComponent<Renderer>().materials[targetMaterialSlot].SetColor ("_Color", curColor);

		if ((loop==true) && (time>=1.0f)) time-=1.0f;
		
	}

	void OnEnable(){
		if (resetTimeOnActivation==true) time=0;

	}
}
