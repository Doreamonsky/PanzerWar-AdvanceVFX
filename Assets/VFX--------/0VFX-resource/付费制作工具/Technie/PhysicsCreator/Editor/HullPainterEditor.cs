
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

/*
	TODO:
		Make overlay behave properly when HullPainter component removed

		Make sure painting has proper undo/redo support

		Move everything into Technie namespace

	Katie test bugs:
		
		Highlight behaves badly with scales (on self or on parent)

		Hull painter still leaking overlay objects / raycast objects

		HullPainter component drops all asset/collider refs when turned into a prefab

		Focus issue prevents painting on first paint after opening window from component

		Reconnect obj to painting data after deletion
	
	Create overlay object with mesh filter + mesh renderer
	Generate mesh with picked triangles, with vertex colours and material to match

	Need to rebuild overlay mesh on selection changed

	FIXME: Use PrefabUtility.GetPrefabType to avoid generating mesh highlights for prefabs in asset dir?

	FIXME: Components lose the references when turned into assets


	FIXME: Make sure undo/redo works when creating child colliders
			we make new child GameObjects as well as HullPainterChild and Collider objects - these should all be recorded in the undo system

	FIXME: Check if undo is handled correctly when we modify a prefab instance, or within the prefab editor

*/

namespace Technie.PhysicsCreator
{
	[CustomEditor(typeof(HullPainter))]
	public class HullPainterEditor : Editor
	{
		public Texture technieIcon;

		public override void OnInspectorGUI()
		{
			if (technieIcon == null)
			{
				string installPath = HullPainterWindow.FindInstallPath();
				technieIcon = AssetDatabase.LoadAssetAtPath<Texture>(installPath + "Icons/TechnieIcon.png");
			}

			if (HullPainterWindow.IsOpen())
			{
				HullPainterWindow window = HullPainterWindow.instance;

				window.OnInspectorGUI();
			}

			HullPainter selectedPainter = SelectionUtil.FindSelectedHullPainter ();
			if (selectedPainter != null)
			{
				if (selectedPainter.paintingData != null
				    && selectedPainter.hullData != null)
				{
					if (GUILayout.Button(new GUIContent("Open Hull Painter", technieIcon)))
					{
						EditorWindow.GetWindow(typeof(HullPainterWindow));
					}
				}
				else
				{
					MeshFilter srcMeshFilter = selectedPainter.gameObject.GetComponent<MeshFilter>();
					Mesh srcMesh = srcMeshFilter != null ? srcMeshFilter.sharedMesh : null;
					if (srcMesh != null)
					{
						CommonUi.DrawGenerateOrReconnectGui(selectedPainter.gameObject, srcMesh);
					}
					else
					{
						GUILayout.Label("No mesh on current object!");
					}
				}
			}
		}


		// NB: If Gizmos are disabled then OnSceneGUI is not called
		// From 2019.1 onward the HullPainterWindow uses OnBeforeSceneGUI so this might be redundant
		// (but still needed for 2018 etc.)
		public void OnSceneGUI ()
		{
			if (HullPainterWindow.IsOpen())
			{
				if (Event.current.commandName == "UndoRedoPerformed")
				{
					HullPainterWindow window = HullPainterWindow.instance;
					window.Repaint();
				}
			}
		}


	}

} // namespace Techie.PhysicsCreator

