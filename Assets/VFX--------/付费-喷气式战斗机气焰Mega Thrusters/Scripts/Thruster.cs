using UnityEngine;
using System.Collections;

public class Thruster : MonoBehaviour
{
    [System.Serializable]
    public enum ThrusterSounds { noSound, deepSpace, commercialJet, spaceShuttle, combatJet, mediumJet, bigJet}
    [HideInInspector]
    public float globalScale = 1;
    [HideInInspector]
    public float thrusterLength = 10;
    [HideInInspector]
    public ThrusterSounds thrusterSound;

    [HideInInspector]
    [Range(0, 0.1f)]
    public float thrusterVariation = 1f;

    [HideInInspector]
    public LineRenderer thrLine;
    [HideInInspector]
    public AudioSource thrAudio;
    [HideInInspector]
    public GameObject thrSphere;
    [HideInInspector]
    public float sphereModificator = 0.7f;
    [HideInInspector]
    public Color start, end;
    float length;
    float currentScale;
    float variationSpeed = 60;
    


    void Start()
    {
        currentScale = globalScale;
        thrLine = transform.Find("LineRenderer").gameObject.GetComponent<LineRenderer>();
        thrAudio = transform.Find("AudioSource").gameObject.GetComponent<AudioSource>();
        thrSphere = transform.Find("Sphere").gameObject;
        thrLine.SetPosition(1, Vector3.forward * length);
        InvokeRepeating("Variator", 0, 1 / variationSpeed);
        SetSound();
    }

    public void SetSound()
    {
        switch(thrusterSound)
        {
            case ThrusterSounds.noSound:
                thrAudio.clip = null;
                thrAudio.Stop();
                break;
            case ThrusterSounds.deepSpace:
                thrAudio.clip = Resources.Load<AudioClip>("jet00");
                thrAudio.Play();
                break;
            case ThrusterSounds.commercialJet:
                thrAudio.clip = Resources.Load<AudioClip>("jet01");
                thrAudio.Play();
                break;
            case ThrusterSounds.spaceShuttle:
                thrAudio.clip = Resources.Load<AudioClip>("jet02");
                thrAudio.Play();
                break;
            case ThrusterSounds.combatJet:
                thrAudio.clip = Resources.Load<AudioClip>("jet03");
                thrAudio.Play();
                break;
            case ThrusterSounds.mediumJet:
                thrAudio.clip = Resources.Load<AudioClip>("jet04");
                thrAudio.Play();
                break;
            case ThrusterSounds.bigJet:
                thrAudio.clip = Resources.Load<AudioClip>("jet05");
                thrAudio.Play();
                break;
        }

    }

    void Update()
    {
        if(globalScale!=currentScale)
        {
            currentScale = globalScale;
            thrLine.SetWidth(globalScale, globalScale);
            thrSphere.transform.localScale = new Vector3(globalScale * sphereModificator, globalScale * sphereModificator, globalScale * sphereModificator);
        }

        length = thrusterLength * globalScale;
    }

    void Variator()
    {
        float noise = Random.Range(1 - (thrusterVariation/10f), 1);
        thrLine.SetPosition(1, Vector3.forward * length * noise);
    }

}
