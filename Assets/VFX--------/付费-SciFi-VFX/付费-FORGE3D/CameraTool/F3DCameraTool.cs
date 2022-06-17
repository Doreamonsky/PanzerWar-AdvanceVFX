#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System;
using System.Linq;

namespace Forge3D
{
    [ExecuteInEditMode]
    [Serializable]
    public class F3DCameraTool : MonoBehaviour
    {
        public static F3DCameraTool instance;

        public Camera PrimaryCamera;
        public Camera SecondaryCamera;

        public Vector3 PrimaryCameraOriginPos;
        public Quaternion PrimaryCameraOriginRot;

        public Vector3 SecondaryCameraOriginPos;
        public Quaternion SecondaryCameraOriginRot;

        public bool SetPrimaryCameraPosition;
        public bool SetPrimaryCameraRotation;

        public bool SetSecondaryCameraPosition;
        public bool SetSecondaryCameraRotation;

        public static bool Enabled = false;
        public static bool skipEvents = false;

        public KeyCode ToggleKey;

        public bool GameViewFocus;
        public bool QuietMode;
        public bool EnableGameViewCamera = true;
        public bool AlignSceneCamOnStart = false;
        public bool AlignPrimaryGameCamOnStop = false;
        public bool AlignSecondaryGameCamOnStop = false;

     

        //   #if UNITY_EDITOR
        void Awake()
        {
            instance = this;

           

            // Init CameraTool on scene load
            //    Selection.activeGameObject = FindObjectOfType<F3DCameraTool>().gameObject;

            F3DCameraToolEditor.camTool = this;
        


        }

        void Start()
        {
        
            // Check for multiple instances
            F3DCameraTool[] instCheck = FindObjectsOfType<F3DCameraTool>();

            // Warn for multiple instances
            if (instCheck.Length > 1)
                Debug.LogWarning("[CameraTool] Multiple script instances found! Please use a single instance of this script to ensure a failsafe workflow.");

            // Listen for play mode changes within the editor
            //EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;


        }

        /// <summary>
        /// Fast setup for two cameras with the activation hotkey bound to 'C' key
        /// </summary>
        public void AutoSetup()
        {
            // Try to autofill both cameras
            if (!PrimaryCamera && !SecondaryCamera && Camera.allCameras.Length > 0)
            {
                if (Camera.allCameras != null)
                {
                    var result = Camera.allCameras.OrderByDescending(c => c.depth).ToArray();

                    PrimaryCamera = result[0];

                    SetPrimaryCameraPosition = true;
                    SetPrimaryCameraRotation = true;

                    if (Camera.allCameras.Length > 1)
                    {
                        SecondaryCamera = result[1];

                        SetSecondaryCameraPosition = false;
                        SetSecondaryCameraRotation = true;
                    }
                }
            }

            // Set the hotkey
            ToggleKey = KeyCode.C;
        }

        void HandleOnPlayModeChanged(PlayModeStateChange playModeStateChange)
        {
            // Disable following when stopped playing
            if (!EditorApplication.isPlaying)
                F3DCameraTool.Enabled = false;
        }

        /// <summary>
        /// Camera follow
        /// </summary>
        void OnRenderObject()
        {
            // Check for enabled flag and active scene view
            if (Enabled && UnityEditor.SceneView.lastActiveSceneView != null)
            {
                // If primary camera is set
                if (PrimaryCamera)
                {
                    if (SetPrimaryCameraPosition)
                        PrimaryCamera.transform.position = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position - UnityEditor.SceneView.lastActiveSceneView.camera.transform.forward * 0.1f;
                    if (SetPrimaryCameraRotation)
                        PrimaryCamera.transform.rotation = UnityEditor.SceneView.lastActiveSceneView.camera.transform.rotation;
                }

                // If secondary camera is set
                if (SecondaryCamera)
                {
                    if (SetSecondaryCameraPosition)
                        SecondaryCamera.transform.position = UnityEditor.SceneView.lastActiveSceneView.camera.transform.position - UnityEditor.SceneView.lastActiveSceneView.camera.transform.forward * 0.1f;
                    if (SetSecondaryCameraRotation)
                        SecondaryCamera.transform.rotation = UnityEditor.SceneView.lastActiveSceneView.camera.transform.rotation;
                }
            }
        }

        public void OnBecameActive()
        {
            if (AlignSceneCamOnStart && UnityEditor.SceneView.lastActiveSceneView != null && PrimaryCamera)
                UnityEditor.SceneView.lastActiveSceneView.LookAt(PrimaryCamera.transform.position, PrimaryCamera.transform.rotation, 0, false, true);
        }

        public void OnBecameInactive()
        {
            if (AlignPrimaryGameCamOnStop && PrimaryCamera)
            {
                PrimaryCamera.transform.position = PrimaryCameraOriginPos;
                PrimaryCamera.transform.rotation = PrimaryCameraOriginRot;
            }

            if (AlignSecondaryGameCamOnStop && SecondaryCamera)
            {
                SecondaryCamera.transform.position = SecondaryCameraOriginPos;
                SecondaryCamera.transform.rotation = SecondaryCameraOriginRot;
            }
        }

        /// <summary>
        /// GUI notifications and gameview management
        /// </summary>
        void OnGUI()
        {
         

            // GameView camera is enabled
            if (EnableGameViewCamera)
            {
                // Show notification in gameview
                if (Enabled && !QuietMode)
                {
                    GUI.contentColor = Color.yellow;

                    GUILayout.BeginArea(new Rect(10, 10, 300, 100));
                    if (!PrimaryCamera)
                        GUILayout.Label("[CameraTool] Please set the primary camera first!");
                    else if (UnityEditor.SceneView.lastActiveSceneView == null)
                    {
                        GUILayout.Label("[CameraTool] The Scene editor window is missing. Make sure it's open or visible to return control!");
                    }
                    else if (!SetPrimaryCameraPosition && !SetPrimaryCameraRotation && !SetSecondaryCameraPosition && !SetSecondaryCameraRotation)
                        GUILayout.Label("[CameraTool] Please set the options first!");
                    else if (EditorApplication.isPaused)
                        GUILayout.Label("[CameraTool] The application is paused. Due to restrictions the Game View window is unable to process events. Use the Scene editor window instead...");
                    else
                        GUILayout.Label("[CameraTool]");
                    GUILayout.EndArea();
                }

                // Make sure this instance will receive all the required events
                EditorUtility.SetDirty(this);

                if (PrimaryCamera || SecondaryCamera)
                {
                   /* // Skip sceneview picking
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                        skipEvents = true;
                    else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                        skipEvents = false;*/

                    // Redirect mouse and keyboard events        
                    if (!skipEvents && Enabled && UnityEditor.SceneView.lastActiveSceneView != null)
                    {
                        // Skip repaint and layout events
                        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                        {
                            // Set focus to SceneView window before rewiring any events
                            // This is required to get around Unity bug which makes it freeze when the SceneView is hidden behind various tabs
                            UnityEditor.SceneView.lastActiveSceneView.Focus();

                            //////////////////////////
                            //////////////////////////
                            //////////////////////////////////////////////////////////////////////////////





                            // PASS TROUGH CLICK EVENTS FOR ORBIT MODE!



                            //////////////////////////
                            // Mouse position cheat to make all the area of gameview responsive while manipulating camera
                            if ((Event.current.type == EventType.MouseDown && (Event.current.button == 0 || Event.current.button == 1)))
                                Event.current.mousePosition = new Vector2(100, 100);

                            //if (Event.current.alt)
                            //    Debug.Log("Alt");
                            // Send the event
                            UnityEditor.SceneView.lastActiveSceneView.SendEvent(Event.current);
                            Event.current.Use();

                            // Experimental 
                            // Allows to manipulate the camera when the sceneview is tabbed and invisible
                            if (GameViewFocus)
                            {
                                // Using reflection to get GameView instance and setting focus manually
                                System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
                                System.Type type = assembly.GetType("UnityEditor.GameView");
                                EditorWindow gameview = EditorWindow.GetWindow(type);
                                gameview.Focus();
                            }
                        }
                    }
                }
                
                // GameView support
                // Check for KeyCode.None as it acts as anykey
                if (!Enabled && ToggleKey != KeyCode.None && UnityEditor.SceneView.lastActiveSceneView != null)
                {
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == ToggleKey && !Event.current.shift)
                    {
                        Enabled = true;
                        OnBecameActive();
                        Event.current.Use();
                    }
                }
                else if (Enabled && ToggleKey != KeyCode.None && !PrimaryCamera && !SecondaryCamera)
                {
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == ToggleKey && !Event.current.shift)
                    {
                        Enabled = false;
                        OnBecameInactive();
                        Event.current.Use();
                    }
                }
            }
        }

        /// <summary>
        /// Playmode support
        /// </summary>
        void Update()
        {
          
            // GameView camera is enabled
            if (EnableGameViewCamera)
            {
                // Check for KeyCode.None as it acts as anykey
                if (Application.isPlaying && ToggleKey != KeyCode.None && Input.GetKeyDown(ToggleKey) && (PrimaryCamera || SecondaryCamera))
                {
                    if (!Enabled && UnityEditor.SceneView.lastActiveSceneView == null)
                        Debug.LogWarning("[CameraTool] Stopped. A visible Scene editor window is required to operate!");
                    else
                    {
                        Enabled = true;
                        OnBecameActive();
                    }
                }
            }
        }     
    }
}
#endif