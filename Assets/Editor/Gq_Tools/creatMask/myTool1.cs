using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class myTool1 : EditorWindow
{
    public Texture2D metallic,occlusion,detailmask,smoothness, roughness;
    public Vector4 colorScale=new Vector4(1,1,1,1);
     Material StepMat;
    int take=0;
    bool isUseRoughnes;
    [MenuItem("myTools/CreatMask")]
    public static void ShowWin()
    {
        EditorWindow.CreateInstance<myTool1>().Show();
    }
    //为了显示图片上的标签
    private static Texture2D TextureField(string name, Texture2D texture)
    {
        GUILayout.BeginVertical();
        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.fixedWidth = 70;
        GUILayout.Label(name, style);
        var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
        GUILayout.EndVertical();
        return result;
    }
    private void OnGUI()
    {
        GUILayout.Space(10);
        isUseRoughnes = EditorGUILayout.Toggle("isUseRoughness?", isUseRoughnes);
        GUILayout.BeginHorizontal("box");
        metallic= TextureField("Metallic", metallic);
        occlusion= TextureField("Occlusion", occlusion);
        detailmask= TextureField("Detailmask", detailmask);
        if (!isUseRoughnes)
        {
            smoothness = TextureField("Smoothness", smoothness);
        }
        else //如果使用了Roughness贴图，那么换一个图片标签
        {
            smoothness = TextureField("Roughness", smoothness);
        }
        // detailmask = (Texture)EditorGUILayout.ObjectField(new GUIContent("detailmask:"), detailmask, typeof(Texture), true, GUILayout.MinWidth(1f));
        //smoothness = (Texture)EditorGUILayout.ObjectField(new GUIContent("smoothness:"), smoothness, typeof(Texture), true, GUILayout.MinWidth(1f));

        //occlusion = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("occlusion:"), occlusion, typeof(Texture), true, GUILayout.MinWidth(1f));
        // detailmask = (Texture)EditorGUILayout.ObjectField(new GUIContent("detailmask:"), detailmask, typeof(Texture), true, GUILayout.MinWidth(1f));
        // smoothness = (Texture)EditorGUILayout.ObjectField(new GUIContent("smoothness:"), smoothness, typeof(Texture), true, GUILayout.MinWidth(1f));
        GUILayout.EndHorizontal();

        // StepMat = (Material)EditorGUILayout.ObjectField(new GUIContent("Used_Material:"), StepMat, typeof(Material), true, GUILayout.MinWidth(100f));
        //// colorScale=(Vector4)EditorGUILayout.Vector4Field("r*X--g*Y--b*Z--a*W", colorScale);
        Material StepMat = (Material)Resources.Load("creatMaskMat", typeof(Material));
        GUILayout.Space(10);
        if (GUILayout.Button("CreatMaskTex"))//UI上画一个按钮
        {
            Do();
        }

        void Do()
        {
            //一个空的黑色rt,如果用户没有拖入图片，那么应该为shader传入默认的黑色信息
            RenderTexture nullrt = new RenderTexture(8,8, 16, RenderTextureFormat.ARGB32);
            nullrt.Create();

            //mask保存路径
            string path = "";
            //Shader.SetGlobalVector("_ColorScale", colorScale);
            if (!isUseRoughnes) Shader.SetGlobalInt("isUseRoughness", 0);
             else Shader.SetGlobalInt("isUseRoughness", 1);//如果使用了roughness

            Vector2 rtSize = new Vector2(1024,1024);
            if (metallic == null)
            {
                Shader.SetGlobalTexture("_MetallicTex", nullrt);
            }
            else
            {
                Shader.SetGlobalTexture("_MetallicTex", metallic);
                rtSize.x = metallic.width; rtSize.y = metallic.height;
                path = AssetDatabase.GetAssetPath(metallic);
                path = path.Substring(0, path.Length - 4);
            }
            if (occlusion== null)
            {
                Shader.SetGlobalTexture("_OcclusionTex", nullrt);
            }
            else
            {
                Shader.SetGlobalTexture("_OcclusionTex", occlusion);
                rtSize.x = occlusion.width; rtSize.y = occlusion.height;
                path = AssetDatabase.GetAssetPath(occlusion);
                path = path.Substring(0, path.Length - 4);
            }
            if (detailmask == null)
            {
                Shader.SetGlobalTexture("_DetailMaskTex", nullrt);
            }
            else {
                Shader.SetGlobalTexture("_DetailMaskTex", detailmask);
                rtSize.x = detailmask.width; rtSize.y = detailmask.height;
                path = AssetDatabase.GetAssetPath(detailmask);
                path = path.Substring(0, path.Length - 4);
            }
            if (smoothness == null)
            {
                Shader.SetGlobalTexture("_SmoothnessTex", nullrt);
            }
            else
            {
                Shader.SetGlobalTexture("_SmoothnessTex", smoothness);
                rtSize.x = smoothness.width; rtSize.y = smoothness.height;
                path = AssetDatabase.GetAssetPath(smoothness);
                path = path.Substring(0, path.Length - 4);
            }
            RenderTexture rt = new RenderTexture((int)rtSize.x, (int)rtSize.y, 16, RenderTextureFormat.ARGB32);
            rt.Create();
            Graphics.Blit(nullrt, rt, StepMat, 0);
           
            //MonoBehaviour.print("do0" + path);
            SaveRenderTextureToPNG(rt, path, Take());
        }
        //保存
        void SaveRenderTextureToPNG(RenderTexture rt, string contents,string addStr)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
            png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            byte[] bytes = png.EncodeToPNG();
            if (!Directory.Exists(contents))
                Directory.CreateDirectory(contents);
            FileStream file = File.Open(contents + "Mask" + addStr+ ".png", FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(bytes);
            file.Close();
            Texture2D.DestroyImmediate(png);
            png = null;
            RenderTexture.active = prev;
            //MonoBehaviour.print("do1");
            AssetDatabase.Refresh();
        }
        //防止重名覆盖
        string Take()
        {
            take++;
            string outStr = take.ToString();
            return outStr;
        }
    }
}