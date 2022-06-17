using UnityEngine;
using System.Collections;

public class tazo_trailFade : MonoBehaviour {
	public float fadeInTime=0.2f;
	public float stayTime=1f;
	public float fadeOutTime=0.5f;
	public TrailRenderer thisTrail;
	private Color mycolor =new  Color(0.5f,0.5f,0.5f,1f);
private float timeElapsed=0f;
private float timeElapsedLast=0f;
private float percent;
void Start (){
		
thisTrail.material.color = mycolor  ;
if(fadeInTime<0.01f) fadeInTime=0.01f; //hack to avoid division with zero
percent=timeElapsed/fadeInTime;

}
void Update (){
timeElapsed+=Time.deltaTime;
if(timeElapsed<=fadeInTime) //fade in
{
percent=timeElapsed/fadeInTime;
			mycolor =new Color(0.5f,0.5f,0.5f,percent);
			thisTrail.material.color = mycolor  ;
}

if((timeElapsed>fadeInTime)&&(timeElapsed<fadeInTime+stayTime)) //set the normal color
{
			thisTrail.material.color = mycolor  ;
}

if(timeElapsed>=fadeInTime+stayTime&&timeElapsed<fadeInTime+stayTime+fadeOutTime) //fade out
{
timeElapsedLast+=Time.deltaTime;
percent=1-(timeElapsedLast/fadeOutTime);
			mycolor =new Color(0.5f,0.5f,0.5f,percent);
			thisTrail.material.color = mycolor  ;
}

}

void OnEnable (){
		thisTrail.material.color = mycolor  ;
timeElapsed=0f;
timeElapsedLast=0f;
percent=timeElapsed/fadeInTime;
}


}