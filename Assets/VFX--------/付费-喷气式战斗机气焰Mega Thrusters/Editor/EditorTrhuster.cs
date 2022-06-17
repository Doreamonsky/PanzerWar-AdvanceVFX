#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(Thruster))]
public class EditorTrhuster : Editor
{
    public override void OnInspectorGUI()
    {
        Thruster t = (Thruster)target;
        t.thrLine = t.transform.Find("LineRenderer").gameObject.GetComponent<LineRenderer>();
        t.thrAudio = t.transform.Find("AudioSource").gameObject.GetComponent<AudioSource>();
        t.thrSphere = t.transform.Find("Sphere").gameObject;

        DrawHeader();
        Texture2D myText = null;
        myText = Resources.Load("EditorData/MainImage") as Texture2D;

        float maxHeight = Screen.width / 2 > 512?512: Screen.width / 2;

        GUILayout.Label(myText, GUILayout.MaxHeight(maxHeight-10)); 
        EditorGUILayout.LabelField("LOAD A PRESET OR CUSTOMIZE THE THRUSTER");
        Color temp = GUI.backgroundColor;
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.clear;
        myText = Resources.Load("EditorData/preset00") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 1);
        }
        myText = Resources.Load("EditorData/preset01") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 2);
        }
        myText = Resources.Load("EditorData/preset02") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 3);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.clear;
        myText = Resources.Load("EditorData/preset03") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 4);
        }
        myText = Resources.Load("EditorData/preset04") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 5);
        }
        myText = Resources.Load("EditorData/preset05") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 6);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.clear;
        myText = Resources.Load("EditorData/preset06") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 7);
        }
        myText = Resources.Load("EditorData/preset07") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 8);
        }
        myText = Resources.Load("EditorData/preset08") as Texture2D;
        if (GUILayout.Button(myText, GUILayout.Width(128), GUILayout.Height(64)))
        {
            loadPreset(t, 9);
        }
        EditorGUILayout.EndHorizontal();
        
        GUI.backgroundColor = temp;
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUILayout.HelpBox("Use global scale to set size of all elements", MessageType.None, true);

        float sc = t.globalScale;
        t.globalScale = EditorGUILayout.Slider("Global scale:",t.globalScale, 0.1f, 100);
        if (sc != t.globalScale)
        {
            t.thrLine.SetWidth(t.globalScale, t.globalScale);
            t.thrSphere.transform.localScale = new Vector3(t.globalScale * t.sphereModificator, t.globalScale * t.sphereModificator, t.globalScale * t.sphereModificator);
        }

        EditorGUILayout.LabelField("");
        EditorGUILayout.HelpBox("Choose thruster sound or none if wont any sound at all", MessageType.None, true);
        Thruster.ThrusterSounds ta = t.thrusterSound;
        t.thrusterSound = (Thruster.ThrusterSounds)EditorGUILayout.EnumPopup("Jet sound:", t.thrusterSound);
        if (EditorApplication.isPlaying && ta!=t.thrusterSound)
            t.SetSound();
        
        EditorGUILayout.LabelField("");
        EditorGUILayout.HelpBox("Thruster length and thruter variation controls the size and flickering of the thruster", MessageType.None, true);
        
        t.thrusterLength = EditorGUILayout.FloatField("Thruster length:",t.thrusterLength);
        t.thrLine.SetPosition(1, Vector3.forward * t.thrusterLength*t.globalScale);

        t.thrusterVariation = EditorGUILayout.Slider("Thruster variation:", t.thrusterVariation, 0, 5f);

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.HelpBox("Set manually the colors of the thruster instead of the presets", MessageType.None, true);

        Color sta = t.start; 
        Color edb = t.end;
        t.start = EditorGUILayout.ColorField("Start color", t.start);
        t.end = EditorGUILayout.ColorField("End color", t.end);
        if (sta != t.start || edb != t.end)
        {
            t.thrLine.SetColors(t.start, t.end);
            t.thrSphere.GetComponent<Renderer>().sharedMaterial.SetColor("_TintColor", t.start);
        }
        EditorGUILayout.Space();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(t);
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        }

        //EditorUtility.SetDirty(t);
    }

    void loadPreset(Thruster t, int skin)
    {
        switch(skin)
        {
            case 1:
                t.start = hexToColor("298CADFF");
                t.end = hexToColor("1300FFFF");
                break;
            case 2:
                t.start = hexToColor("0041FFFF");
                t.end = hexToColor("FF00CEFF");
                break;
            case 3:
                t.start = hexToColor("FF00F8FF");
                t.end = hexToColor("DBFF00FF");
                break;
            case 4:
                t.start = hexToColor("A1AACEFF");
                t.end = hexToColor("000CFFFF");
                break;
            case 5:
                t.start = hexToColor("A4CEA1FF");
                t.end = hexToColor("1EFF00FF");
                break;
            case 6:
                t.start = hexToColor("AFB273FF");
                t.end = hexToColor("FF2500FF");
                break;
            case 7:
                t.start = hexToColor("ADBD07FF");
                t.end = hexToColor("FF0005FF");
                break;
            case 8:
                t.start = hexToColor("1E00FFFF");
                t.end = hexToColor("891686FF");
                break;
            case 9:
                t.start = hexToColor("FF4400FF");
                t.end = hexToColor("FF4400FF");
                break;
        }
        t.thrLine.SetColors(t.start, t.end);
        t.thrSphere.GetComponent<Renderer>().sharedMaterial.SetColor("_TintColor", t.start);
    }

    public Color hexToColor(string hex)
    {
        hex = hex.Replace("0x", "");//in case the string is formatted 0xFFFFFF
        hex = hex.Replace("#", "");//in case the string is formatted #FFFFFF
        byte a = 255;//assume fully visible unless specified in hex
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        //Only use alpha if the string has enough characters
        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return new Color32(r, g, b, a);
    }
}
#endif