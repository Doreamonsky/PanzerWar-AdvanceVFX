#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System;

namespace Forge3D
{
    [InitializeOnLoad]
    [CustomEditor(typeof(F3DCameraTool))]
    [Serializable]
    public class F3DCameraToolEditor : Editor
    {
        public static F3DCameraTool camTool;
        public static F3DCameraToolEditor camToolEditor;

        bool expFoldout;

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;

            camTool = (F3DCameraTool)target;

            if (!camTool)
                camTool = F3DCameraTool.instance;

            camToolEditor = this;
        }

        static F3DCameraToolEditor()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }
     
        [MenuItem("FORGE3D/CameraTool/Auto setup", false, 0)]
        static void CameraToolSetup()
        {
            Selection.activeGameObject = (Camera.allCameras.OrderByDescending(c => c.depth).ToArray())[0].gameObject;
            Selection.activeGameObject.AddComponent<F3DCameraTool>();
            Selection.activeGameObject.GetComponent<F3DCameraTool>().AutoSetup();
        }

        [MenuItem("FORGE3D/CameraTool/Auto setup", true)]
        static bool ValidateCameraToolSetup()
        {
            if (Camera.allCameras == null || FindObjectOfType<F3DCameraTool>() != null) return false;
            else return true;
        }

        [MenuItem("FORGE3D/CameraTool/Setup default", false, 1)]
        static void AddCameraTool()
        {
            Selection.activeGameObject = (Camera.allCameras.OrderByDescending(c => c.depth).ToArray())[0].gameObject;
            Selection.activeGameObject.AddComponent<F3DCameraTool>();
        }

        [MenuItem("FORGE3D/CameraTool/Setup default", true)]
        static bool ValidateAddCameraTool()
        {
            if (Camera.allCameras == null || FindObjectOfType<F3DCameraTool>() != null) return false;
            else return true;
        }

        [MenuItem("FORGE3D/CameraTool/Select active camera [Shift + C]", false, 2)]
        static void SelectCameraTool()
        {
            if (FindObjectOfType<F3DCameraTool>() != null)
                Selection.activeGameObject = FindObjectOfType<F3DCameraTool>().gameObject;
        }

        [MenuItem("FORGE3D/CameraTool/Select active camera [Shift + C]", true)]
        static bool ValidateSelectCameraTool()
        {
            if (FindObjectOfType<F3DCameraTool>() != null) return true;
            else return false;
        }

        public override void OnInspectorGUI()
        {     
            EditorGUILayout.BeginVertical();

            GUIStyle smallFont = new GUIStyle();
            smallFont.fontSize = 9;
            smallFont.wordWrap = true;

            smallFont.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            GUIStyle headerFont = new GUIStyle();
            headerFont.fontSize = 11;
            headerFont.fontStyle = FontStyle.Bold;
            headerFont.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            GUIStyle subHeaderFont = new GUIStyle();
            subHeaderFont.fontSize = 10;
            subHeaderFont.fontStyle = FontStyle.Bold;
            subHeaderFont.margin = new RectOffset(1, 0, 0, 0);
            subHeaderFont.padding = new RectOffset(1, 0, 3, 0);
            subHeaderFont.normal.textColor = new Color(0.70f, 0.70f, 0.70f);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Camera selection:", headerFont);

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            camTool.PrimaryCamera = (Camera)EditorGUILayout.ObjectField("Primary camera:", camTool.PrimaryCamera, typeof(Camera), true);
            EditorGUILayout.EndVertical();

            if (camTool.PrimaryCamera)
            {
                EditorGUILayout.LabelField("Follow options:", subHeaderFont);

                camTool.SetPrimaryCameraPosition = EditorGUILayout.Toggle("Position", camTool.SetPrimaryCameraPosition);
                camTool.SetPrimaryCameraRotation = EditorGUILayout.Toggle("Rotation", camTool.SetPrimaryCameraRotation);

                EditorGUILayout.LabelField("Camera settings:", subHeaderFont);

                camTool.AlignPrimaryGameCamOnStop = EditorGUILayout.Toggle("Self align", camTool.AlignPrimaryGameCamOnStop);
                camTool.AlignSceneCamOnStart = EditorGUILayout.Toggle("Match Scene to Game", camTool.AlignSceneCamOnStart);

                EditorGUILayout.LabelField("Stored origin:", subHeaderFont);

                camTool.PrimaryCameraOriginPos = EditorGUILayout.Vector3Field("Position", camTool.PrimaryCameraOriginPos);
                camTool.PrimaryCameraOriginRot = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", camTool.PrimaryCameraOriginRot.eulerAngles));

                EditorGUILayout.Space();

                if (GUILayout.Button("Store origin"))
                {
                    camTool.PrimaryCameraOriginPos = camTool.PrimaryCamera.transform.position;
                    camTool.PrimaryCameraOriginRot = camTool.PrimaryCamera.transform.rotation;
                }

                if (GUILayout.Button("Align to origin"))
                {
                    F3DCameraTool.Enabled = false;

                    camTool.PrimaryCamera.transform.position = camTool.PrimaryCameraOriginPos;
                    camTool.PrimaryCamera.transform.rotation = camTool.PrimaryCameraOriginRot;
                }

            }
            EditorGUILayout.EndVertical();

            //EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginVertical("Box");
            camTool.SecondaryCamera = (Camera)EditorGUILayout.ObjectField("Secondary camera:", camTool.SecondaryCamera, typeof(Camera), true);
            EditorGUILayout.EndVertical();

            if (camTool.SecondaryCamera)
            {
                EditorGUILayout.LabelField("Follow options:", subHeaderFont);

                camTool.SetSecondaryCameraPosition = EditorGUILayout.Toggle("Position", camTool.SetSecondaryCameraPosition);
                camTool.SetSecondaryCameraRotation = EditorGUILayout.Toggle("Rotation", camTool.SetSecondaryCameraRotation);

                EditorGUILayout.LabelField("Camera settings:", subHeaderFont);

                camTool.AlignSecondaryGameCamOnStop = EditorGUILayout.Toggle("Self align", camTool.AlignSecondaryGameCamOnStop);

                EditorGUILayout.LabelField("Stored origin:", subHeaderFont);

                camTool.SecondaryCameraOriginPos = EditorGUILayout.Vector3Field("Position", camTool.SecondaryCameraOriginPos);
                camTool.SecondaryCameraOriginRot = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", camTool.SecondaryCameraOriginRot.eulerAngles));

                EditorGUILayout.Space();

                if (GUILayout.Button("Store origin"))
                {
                    camTool.SecondaryCameraOriginPos = camTool.SecondaryCamera.transform.position;
                    camTool.SecondaryCameraOriginRot = camTool.SecondaryCamera.transform.rotation;
                }

                if (GUILayout.Button("Align to origin"))
                {
                    F3DCameraTool.Enabled = false;

                    camTool.SecondaryCamera.transform.position = camTool.SecondaryCameraOriginPos;
                    camTool.SecondaryCamera.transform.rotation = camTool.SecondaryCameraOriginRot;
                }

            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            if (F3DCameraTool.Enabled && camTool.PrimaryCamera == null && camTool.SecondaryCamera == null)
                EditorGUILayout.HelpBox("No cameras found. Assign at least one camera uppon activatiion.", MessageType.Warning);

            EditorGUILayout.LabelField("General settings:", headerFont);

            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginVertical("Box");
            camTool.ToggleKey = (KeyCode)EditorGUILayout.EnumPopup("Toggle key", camTool.ToggleKey);
            F3DCameraTool.Enabled = EditorGUILayout.Toggle("Enabled", F3DCameraTool.Enabled);
            EditorGUILayout.EndVertical();

            if (camTool.ToggleKey == KeyCode.None)
                EditorGUILayout.HelpBox("No key assigned", MessageType.Warning);

            camTool.EnableGameViewCamera = EditorGUILayout.Toggle("Enable in Game view", camTool.EnableGameViewCamera);

            camTool.QuietMode = EditorGUILayout.Toggle("Quiet mode", camTool.QuietMode);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            expFoldout = EditorGUILayout.Foldout(expFoldout, "Experimental features");
            if (expFoldout)
            {
                EditorGUILayout.HelpBox("Setting this attribute will return focus to the Game window. Use this to prevent the Scene window capturing focus when F3DCameraTool is active. This scenario is possible when the Scene and Game windows are both docked next to each other so one becomes invisible. Please note that enabling this behaviour may produce unexpected results and should be used at your own risk!", MessageType.Warning);
                camTool.GameViewFocus = EditorGUILayout.Toggle("Game window focus", camTool.GameViewFocus);
            }

            EditorGUILayout.EndVertical();

            // Keep persistant trough play
            EditorUtility.SetDirty(camTool);
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            // Hotkey select active camera 
            if(Event.current.shift && Event.current.keyCode == KeyCode.C && Event.current.type == EventType.KeyDown)               
            {
                SelectCameraTool();
                Event.current.Use();
                return;
            }

            // Display GUI notifications
            if (camTool && camTool.enabled && !camTool.QuietMode)
            {

                if (F3DCameraTool.Enabled)
                {
                    Handles.BeginGUI();

                    GUI.contentColor = Color.yellow;
                    GUILayout.BeginArea(new Rect(10, 10, 300, 100));

                    if (!camTool.PrimaryCamera)
                        GUILayout.Label("[CameraTool] Please set the primary camera first!");
                    else if (!camTool.SetPrimaryCameraPosition && !camTool.SetPrimaryCameraRotation && !camTool.SetSecondaryCameraPosition && !camTool.SetSecondaryCameraRotation)
                        GUILayout.Label("[CameraTool] Please set the options first!");
                    else
                        GUILayout.Label("[CameraTool]");

                    GUILayout.EndArea();
                    Handles.EndGUI();
                }
            }

            // Check for KeyCode.None as it acts as anykey
            if (camTool && camTool.ToggleKey != KeyCode.None && !Event.current.shift)
            {
                if (Event.current.keyCode == camTool.ToggleKey && Event.current.type == EventType.KeyDown)
                {
                    F3DCameraTool.Enabled = !F3DCameraTool.Enabled;

                    if (F3DCameraTool.Enabled)
                        camTool.OnBecameActive();
                    else
                        camTool.OnBecameInactive();

                    if (camToolEditor != null)
                        camToolEditor.Repaint();

                    Event.current.Use();

                    // Repaint gameview to update GUI text in case the inspector is not visible
                    Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
                    Type type = assembly.GetType("UnityEditor.GameView");
                    EditorWindow gameview = EditorWindow.GetWindow(type);
                    if (gameview)
                        gameview.Focus();
                }
            }
        }
    }
}
#endif