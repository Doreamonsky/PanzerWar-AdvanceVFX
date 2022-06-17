using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Complete
{
    public class TankScript : MonoBehaviour
    {
        Camera cam;

        public float max_Speed = 60f;
        public float max_TurnSpeed = 70f;

        [Range(0f, 3f)]
        public float volume_Idle = 1f, volume_Acceleration = 1f, volume_Movement = 1f, volume_Turbine = 1f, volume_Start = 1f, volume_Stop = 1f, volume_ObstacleHit = 1f;

        public AudioClip c_Idle, c_Acceleration, c_Movement, c_Turbine, c_Start, c_Stop;
        public List<AudioClip> c_ObstacleHit = new List<AudioClip>();

        private AudioSource s_Idle, s_Acceleration, s_Movement, s_Turbine, s_Start, s_Stop, s_ObstacleHit;

        private AudioSource musicSource;

        private AudioSource fadeOutSource;
        private bool fadeOut = false;
        private const float fadeOutTime = 3f;
        private float fadeOutTimer = fadeOutTime;


        public GameObject audioSourcePrefab;

        private string movementAxisName = "Vertical";
        private string turnAxisName = "Horizontal";
        private Rigidbody m_Rigidbody;
        private float movementInputValue = 0f;         // The current value of the movement input.
        private float prevMovementValue = 0f;
        private float turnInputValue = 0f;             // The current value of the turn input.
        private float rpm = 0f;
        private float turbineRPM = 0f;
        private float speed = 0f, normSpeed = 0f;
        private float accelerationFactor;

        private bool camView = true;
        private bool setPassByCam = true;
        private bool acceleration = false;
        private bool accelerationPrev = false;
        private bool deceleration = false;

        private Vector3 movement = Vector3.zero;

        private float timerHitSound;

        public List<Shadow> Movement_buttons = new List<Shadow>();
        public List<Text> UI_text = new List<Text>();

        public Color32 temp;

        private void Awake()
        {
            cam = Camera.main;

            m_Rigidbody = GetComponent<Rigidbody>();
            musicSource = cam.GetComponent<AudioSource>();
        }


        private void Start()
        {
            Invoke("PlayMusic", 2f);

            s_Idle = createAudioSource(c_Idle, true);
            s_Acceleration = createAudioSource(c_Acceleration, true);
            s_Movement = createAudioSource(c_Movement, true);
            s_Turbine = createAudioSource(c_Turbine, true);
            s_Start = createAudioSource(c_Start, false);
            s_Stop = createAudioSource(c_Stop, false);
            s_ObstacleHit = createAudioSource(c_ObstacleHit[0], false);

            timerHitSound = Random.Range(1f, 6f);
        }


        private AudioSource createAudioSource(AudioClip clip, bool isLooped)
        {
            GameObject newSoundObj = Instantiate(audioSourcePrefab);
            newSoundObj.transform.SetParent(this.transform);
            newSoundObj.transform.localPosition = Vector3.zero;
            newSoundObj.name = clip.name + " Sound";
            AudioSource aSource = newSoundObj.GetComponent<AudioSource>();
            aSource.clip = clip;
            if (isLooped)
                aSource.Play();
            else
                aSource.loop = false;
            return aSource;
        }

        private void Update()
        {
            acceleration = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow));
            deceleration = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));

            if (Input.GetKeyDown("space"))
            {
                camView = !camView;

                UI_text[4].enabled = !UI_text[4].enabled;
                setPassByCam = true;
            }

            turnInputValue = Input.GetAxis(turnAxisName);
            movementInputValue = Input.GetAxis(movementAxisName);

            // UI
            Movement_buttons[0].enabled = Movement_buttons[0].transform.GetChild(0).GetComponent<Shadow>().enabled = !(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow));
            Movement_buttons[1].enabled = Movement_buttons[1].transform.GetChild(0).GetComponent<Shadow>().enabled = !(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
            Movement_buttons[2].enabled = Movement_buttons[2].transform.GetChild(0).GetComponent<Shadow>().enabled = !(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow));
            Movement_buttons[3].enabled = Movement_buttons[3].transform.GetChild(0).GetComponent<Shadow>().enabled = !(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));

            foreach (var btn in Movement_buttons)
            {
                btn.GetComponent<Outline>().enabled = !btn.enabled;

                if (btn.enabled)
                {
                    btn.GetComponent<Image>().color = new Color32(255, 255, 255, 102);
                    btn.transform.GetChild(0).GetComponent<Text>().color = new Color32(255, 255, 255, 189);
                }
                else
                {
                    btn.GetComponent<Image>().color = new Color32(255, 255, 255, 190);
                    btn.transform.GetChild(0).GetComponent<Text>().color = new Color32(255, 255, 255, 234);
                }
            }
            
            UI_text[0].text = "  Tank Speed: " + (int)speed + "km/h";
            UI_text[1].text = "  Engine RPM: " + (int)(rpm*100) + "%";
            UI_text[2].text = "  Turbine RPM: " + (int)(turbineRPM*100) + "%";
            UI_text[3].text = "  Distance: " + (int)(Vector3.Distance(cam.transform.position, transform.position) - 4f) + "m" ;

            // FadeOut
            if (fadeOut)
            {
                fadeOutSource.volume *= 0.95f;

                fadeOutTimer -= Time.deltaTime;

                if (fadeOutTimer < 0f && fadeOutSource.volume < 0.01f)
                {
                    fadeOut = false;
                    fadeOutTimer = fadeOutTime;
                    fadeOutSource.Stop();
                    fadeOutSource = null;
                }
            }

            if (Input.GetKey("escape"))
            {
                Application.Quit();
            }
        }


        private void FixedUpdate()
        {
            Move();
            Turn();            

            CalculateVariables();

            Magic();
        }

        private void Magic()
        {
            // Doppler
            if (camView)
            {
                foreach (Transform child in transform)
                {
                    AudioSource aS;
                    if (aS = child.GetComponent<AudioSource>())
                        aS.dopplerLevel = 0.5f;
                }
                if (setPassByCam)
                {
                    Vector3 newCamPosition = (12f + normSpeed * 40f) * transform.forward + new Vector3(0f, 3f, 0f) + new Vector3(0f, 0f, 10f);
                    cam.transform.position = this.transform.position + newCamPosition;
                    setPassByCam = false;
                }
                cam.transform.LookAt(this.transform);

            }
            else
            {
                foreach (Transform child in transform)
                {
                    AudioSource aS;
                    if (aS = child.GetComponent<AudioSource>())
                        aS.dopplerLevel = 0f;
                }
                Vector3 newCamPosition = -15f * transform.forward + new Vector3(0f, 3f, 0f);
                cam.transform.position = this.transform.position + newCamPosition;
                cam.transform.LookAt(this.transform);
                cam.transform.Rotate(Vector3.left, 12f);
            }

            // volume & pitch
            if (s_Idle)
            {
                float gain = 0.13f * rpm + 0.65f;
                s_Idle.volume = gain * volume_Idle;
                s_Idle.pitch = 0.65f * rpm + 0.85f;
            }
            if (s_Acceleration)
            {
                float gain = (0.2f * rpm + 0.2f) * accelerationFactor;
                s_Acceleration.volume = gain * volume_Acceleration;
                s_Acceleration.pitch = 0.65f * rpm + 0.75f;
            }
            if (s_Movement)
            {
                float gain = Mathf.Pow(normSpeed, 0.22f);
                float turnGain = Mathf.Pow(turnInputValue, 0.22f);
                s_Movement.volume = Mathf.Min(1f,(gain + 0.6f * Mathf.Abs(turnInputValue)) * volume_Movement);
                s_Movement.pitch = 1.1f * normSpeed + 0.6f;
            }
            if (s_Turbine)
            {
                float gain = 0.4f + 0.6f * turbineRPM;
                s_Turbine.volume = gain * volume_Turbine;
                s_Turbine.pitch = 0.34f * turbineRPM + 0.66f;
            }
            if (s_Start && fadeOutSource != s_Start)
                s_Start.volume = volume_Start;
            if (s_Stop && fadeOutSource != s_Stop)
                s_Stop.volume = volume_Stop;

            if (s_ObstacleHit)
                s_ObstacleHit.volume = volume_ObstacleHit;

            // Start & Stop sounds
            if (acceleration != accelerationPrev)
            {
                if (acceleration)
                {
                    s_Start.Play();
                    if (s_Stop.isPlaying)
                        FadeOut(s_Stop);
                }
                else
                {
                    s_Stop.Play();
                    if (s_Start.isPlaying)
                        FadeOut(s_Start);
                }
            }
            accelerationPrev = acceleration;

            //Obstacle Hit Sounds
            if (timerHitSound < 0f)
            {
                s_ObstacleHit.clip = c_ObstacleHit[Random.Range(0, c_ObstacleHit.Count - 1)];

                if (normSpeed > 0.23f)
                    s_ObstacleHit.Play();
                timerHitSound = Random.Range(1f, 6f);
            }
        }

        private void FadeOut(AudioSource aSource)
        {
            fadeOutSource = aSource;
            fadeOut = true;
        }

        private void CalculateVariables()
        {
            speed = m_Rigidbody.velocity.magnitude;

            rpm = (speed > 30f) ? 2.333E-03f * speed + 0.86f : -1.033E-03f * speed * speed + 0.062f * speed;

            turbineRPM += (rpm - turbineRPM) * 0.02f;

            normSpeed = speed / max_Speed;

            if (acceleration)
                accelerationFactor = 1f + 1.5f * Mathf.Pow(Mathf.Max(0f, movementInputValue), 0.1f);
            else
                accelerationFactor *= 0.96f;

            timerHitSound -= Time.deltaTime;
        }

        private void Move()
        {
            float movementValue = 0f;

            if (acceleration)
            {
                movementValue = 1f;
            }
            if (deceleration)
            {
                movementValue = -0.2f;
            }

            movementValue -= Mathf.Abs(turnInputValue);

            movementValue = Mathf.Max(0f, prevMovementValue + (movementValue - prevMovementValue) * 0.01f);

            movement = transform.forward * movementValue * movementValue * max_Speed * Time.deltaTime;

            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);

            prevMovementValue = movementValue;
        }


        private void Turn()
        {
            float turn = turnInputValue * max_TurnSpeed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        private void PlayMusic()
        {
            if (musicSource)
                musicSource.Play();
        }
    }
}