﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nuclear_bomb : MonoBehaviour {




	[Range(1.0f,256.0f)]
	public float NukeDuration = 40f;

	private float CurrentDuration;
	public AnimationCurve  SizeCurve;

	[Range(1.0f,1024.0f)]
	public float SizeCurve_multiply;


public float LightRadius = 2048;
public AnimationCurve  LightRadius_curve;
public float LightPower = 64;
public AnimationCurve  LightPower_curve;
private float FinalCurveVaue;




private Vector3 finalShockWaveSize;
public float  sizeSpeed =1.0f;
private float finalShockSizeF;
public Transform ShockWaveTransform;

public Light BlastLight;
public ParticleSystem  blowPart;

public MeshRenderer Mushrom;
public float Emmis_mush;
public float Emmis_steam;
	public AnimationCurve  Mat_SizeCurve;
	public float _mat_SizeCurve_multiply;

	// Use this for initialization
	void Start ()
	{
		finalShockWaveSize = new Vector3 (0f,0f,0f);
		CurrentDuration = 0.0f;
		finalShockSizeF =0.0f;

		
	}
	
	// Update is called once per frame
	void Update () 
	
	{


		finalShockSizeF +=Time.deltaTime*sizeSpeed;
		finalShockWaveSize = new Vector3 (finalShockSizeF,finalShockSizeF,finalShockSizeF);
		ShockWaveTransform.localScale = finalShockWaveSize;

		//if(CurrentDuration < NukeDuration)
		//{
		CurrentDuration += Time.deltaTime;
		//}

		FinalCurveVaue = Mathf.Clamp01(CurrentDuration/NukeDuration);

		BlastLight.intensity =LightPower*LightPower_curve.Evaluate(FinalCurveVaue);
		BlastLight.range = Mathf.Lerp(LightRadius,0.0f,LightRadius_curve.Evaluate(FinalCurveVaue));


if(CurrentDuration > 40.0f)
{
	Destroy(gameObject);
}

		
	}
}
