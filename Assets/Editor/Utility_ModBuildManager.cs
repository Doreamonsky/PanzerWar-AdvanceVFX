using ShanghaiWindy.Core;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace ShanghaiWindy.Editor
{
    public class Utility_ModBuildManager : EditorWindow
    {
        private struct BuildMap
        {
            public VehicleInfo vehicleInfo;
            public VehicleData vehicleData;
            public ModPackageBuildPiplineData piplineData;
        }

        private List<BuildMap> buildMaps = new List<BuildMap>();

        private Vector2 _scrollPosition;
        private string searchText;

        private List<bool> selectionList = new List<bool>();

        [MenuItem("Mod/Mod Build Manager")]
        static void Init()
        {
            var win = EditorWindow.GetWindow(typeof(Utility_ModBuildManager));
            win.titleContent.text = "Mod Build Manager";
        }

        private void OnEnable()
        {
            var vehicleDataList = new List<VehicleData>();
            var piplineDataList = new List<ModPackageBuildPiplineData>();

            AssetDatabase.FindAssets("t:VehicleData").ToList().ForEach(guid =>
            {
                vehicleDataList.Add(AssetDatabase.LoadAssetAtPath<VehicleData>(AssetDatabase.GUIDToAssetPath(guid)));
            });

            AssetDatabase.FindAssets("t:ModPackageBuildPiplineData").ToList().ForEach(guid =>
            {
                piplineDataList.Add(AssetDatabase.LoadAssetAtPath<ModPackageBuildPiplineData>(AssetDatabase.GUIDToAssetPath(guid)));
            });

            foreach (var guid in AssetDatabase.FindAssets("t:VehicleInfo"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var vehicleInfo = AssetDatabase.LoadAssetAtPath<VehicleInfo>(path);
                var vehicleData = vehicleDataList.Find(x => x.vehicleTextData.AssetName == vehicleInfo.vehicleName);
                var piplineData = piplineDataList.Find(x => x.linkedObjects.Contains(vehicleInfo));

                if (vehicleInfo != null && vehicleData != null && piplineData != null)
                {
                    buildMaps.Add(new BuildMap()
                    {
                        piplineData = piplineData,
                        vehicleData = vehicleData,
                        vehicleInfo = vehicleInfo
                    });

                    selectionList.Add(false);
                }
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MinWidth(1000));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mod Build Manager");
            EditorGUILayout.LabelField("Seach:");
            searchText = GUILayout.TextField(searchText);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selection:");

            if (GUILayout.Button("Select Module Ready Vehicles"))
            {
                for (var i = 0; i < buildMaps.Count; i++)
                {
                    if (buildMaps[i].vehicleInfo.isModuleReady)
                    {
                        selectionList[i] = true;
                    }
                    else
                    {
                        selectionList[i] = false;
                    }
                }
            }

            if (GUILayout.Button("All"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    selectionList[i] = true;
                }
            }

            if (GUILayout.Button("None"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    selectionList[i] = false;
                }
            }

            if (GUILayout.Button("Invert"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    selectionList[i] = !selectionList[i];
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Repack Selected Vehicle Data"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    if (selectionList[i])
                    {
                        VehicleDataEditor.PackAsset(buildMaps[i].vehicleData);
                    }
                }

                EditorUtility.DisplayDialog("Build Compeleted", "All the selected vehicle data have been built", "OK");
            }

            if (GUILayout.Button("Repack Selected PiplineData"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    if (selectionList[i])
                    {
                        ModPackageBuildPiplineDataEditor.BuildPipline(buildMaps[i].piplineData);
                    }
                }

                EditorUtility.DisplayDialog("Build Compeleted", "All the selected piplinedata have been built", "OK");
            }

            if (GUILayout.Button("Repack Selected Package"))
            {
                for (var i = 0; i < selectionList.Count; i++)
                {
                    if (selectionList[i])
                    {
                        ModPackageDataEditor.BuildPipline(buildMaps[i].piplineData.linkedModPackage);
                    }
                }

                EditorUtility.DisplayDialog("Build Compeleted", "All the selected packages have been built", "OK");
            }

            if (GUILayout.Button("Copy Selected Package File to AssembleFolder"))
            {
                var assembleFolder = new DirectoryInfo("Build/AssembleFolder/");

                if (!assembleFolder.Exists)
                {
                    assembleFolder.Create();
                }

                for (var i = 0; i < selectionList.Count; i++)
                {
                    if (selectionList[i])
                    {
                        var modPackData = buildMaps[i].piplineData.linkedModPackage;
                        var modPackDir = $"Build/Mod-Package/{EditorUserBuildSettings.activeBuildTarget}/{modPackData.name}/";
                        var format = modPackData.isAuthorizeOwnerShip ? "umodpack" : "modpack";
                        var zipFileName = $"{modPackDir}/{EditorUserBuildSettings.activeBuildTarget}_{modPackData.name}.{format}";
                        File.Copy(zipFileName, $"{assembleFolder.FullName}/{new FileInfo(zipFileName).Name}", true);
                    }
                }

                EditorUtility.RevealInFinder(assembleFolder.FullName);
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < buildMaps.Count; i++)
            {
                BuildMap buildMap = buildMaps[i];
                if (!string.IsNullOrEmpty(searchText))
                {
                    if (!buildMap.vehicleInfo.vehicleName.Contains(searchText))
                    {
                        continue;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Vehicle Name :{buildMap.vehicleInfo.vehicleName}");

                selectionList[i] = EditorGUILayout.Toggle(selectionList[i]);

                if (GUILayout.Button("Re-Pack VehicleData", GUILayout.Width(150)))
                {
                    VehicleDataEditor.PackAsset(buildMap.vehicleData);
                }

                if (GUILayout.Button("Build PiplineData", GUILayout.Width(120)))
                {
                    ModPackageBuildPiplineDataEditor.BuildPipline(buildMap.piplineData, true);
                }

                if (GUILayout.Button("Package", GUILayout.Width(80)))
                {
                    ModPackageDataEditor.BuildPipline(buildMap.piplineData.linkedModPackage, true);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
